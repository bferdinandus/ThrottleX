using Microsoft.Extensions.Logging;
using Shared;
using Shared.LocoTable;
using Shared.Models;
using WiThrottle.Enums;
using WiThrottle.Models;

namespace WiThrottle;

public class WifredClient
{
    private volatile bool _isAcceptingCommands = false;
    private readonly ILogger _logger;
    public string Id { get; private set; }
    public string Name { get; private set; }
    public DateTime? ConnectedAt { get; private set; }
    public DateTime LastMessage { get; private set; }

    public bool IsConnected => _customTcpClient?.IsConnected ?? false;
    public string GetIpAddress() => _customTcpClient?.GetIpAddress() ?? string.Empty;
    public string GetLocoAdresses() => string.Join(", ", _myLocos.Keys.Select(k => k.Address.ToString()));

    private CustomTcpClient? _customTcpClient;
    private readonly Dictionary<IAddress, IThrottle2Row> _myLocos = new();
    private readonly IThrottle2Table _locoTable;
    private CancellationToken _stoppingToken;

    public WifredClient(string id, string name, ILogger<WifredClient> logger, IThrottle2Table locoTable)
    {
        _logger = logger;
        _locoTable = locoTable;

        Id = id;
        Name = name;
    }

    public async Task StartProcessingAsync(CancellationToken cancellationToken)
    {
        _stoppingToken = cancellationToken;
        _isAcceptingCommands = true;
        while (!cancellationToken.IsCancellationRequested && IsConnected && _isAcceptingCommands)
        {
            string command = await _customTcpClient!.ReadNextMessageAsync(cancellationToken);
            if (!_isAcceptingCommands)
            {
                break;
            }
            
            WiThrottleMessage message = MessageProcessor.ParseCommand(command);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (message.Type)
            {
                case WtCommand.Quit:
                    _logger.LogInformation("{Name} says bye.", Name);
                    await Task.Delay(1000, cancellationToken);
                    Disconnect();
                    break;
                case WtCommand.MultiThrottle:
                    await MultiThrottleAsync(MessageProcessor.ParseMultiThrottleCommand(message.Command));
                    break;
                case WtCommand.HeartBeat:
                // TODO : act when heartbeat disappears...
                case WtCommand.Unknown:
                    break;
            }

            LastMessage = DateTime.UtcNow;
        }
    }

    public void UpdateConnection(CustomTcpClient client)
    {
        if (IsConnected) Disconnect();

        _customTcpClient = client;
        ConnectedAt = DateTime.UtcNow;
    }

    public void Disconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        _isAcceptingCommands = false;

        // TODO: make sure all loco's are stopped and released
        // maybe? the wifred should do that actually. but what happens if the wifred just disappears...?

        ConnectedAt = null;
        _customTcpClient?.Dispose();
        _customTcpClient = null!;
    }

    #region MultiThrottleService

    private async Task MultiThrottleAsync(MultiThrottleMessage mtMessage)
    {
        switch (mtMessage.Command)
        {
            case MtCommand.Add: await MtAddAsync(mtMessage); break;
            case MtCommand.Remove: await MtRemoveAsync(mtMessage); break;
            case MtCommand.Action: await MtActionAsync(mtMessage); break;
            default: _logger.LogError("{S}: Received unknown command: {C}", Name, mtMessage.Command); break;
        }
    }

    private async Task MtActionAsync(MultiThrottleMessage mtMessage)
    {
        var throttleCommand = (ThrottleCommand)mtMessage.ThrottleCommandMessage[0];
        var parameter = mtMessage.ThrottleCommandMessage[1..];

        Action<IThrottle2Row>? action = throttleCommand switch
        {
            ThrottleCommand.SetVelocity => row => row.SetSpeed(int.Parse(parameter)),
            ThrottleCommand.SetDirection => row => row.SetDirection(parameter.ParseBinary() ? Direction.Forward : Direction.Reverse),
            ThrottleCommand.EmergencyStop => row => row.SetEmergencyStop(),
            ThrottleCommand.FunctionKey => row =>
            {
                var (number, state) = parameter.ParseFunction();
                row.SetFunctionKey(number, state);
            },
            ThrottleCommand.ForceFunction => row =>
            {
                var (number, state) = parameter.ParseFunction();
                row.ForceFunction(number, state);
            },
            ThrottleCommand.MomentaryFunction => row =>
            {
                var (number, state) = parameter.ParseFunction();
                row.SetMomentaryFunction(number, state);
            },
            _ => null
        };

        if (action == null)
            _logger.LogWarning("Command '{Cmd}'={ThrottleCommand} not implemented, ignoring...", throttleCommand, throttleCommand);
        else
            ForSelectedRows(mtMessage.Address, action);
    }

    private async Task MtRemoveAsync(MultiThrottleMessage mtMessage)
    {
        if (mtMessage.Address != null && !_myLocos.ContainsKey(mtMessage.Address))
        {
            _logger.LogWarning("Throttle wants so remove an address that we don't have under control, ignoring this!");
            return;
        }

        lock (_locoTable)
        {
            ForSelectedRows(mtMessage.Address, row =>
            {
                _myLocos.Remove(row.Address);
                row.Deactivate();
            });
        }
    }

    private async Task MtAddAsync(MultiThrottleMessage mtMessage)
    {
        if (mtMessage.Address == null)
            throw Panic(LogLevel.Error, "MT-Add with wildcard is not legal");

        if (_myLocos.ContainsKey(mtMessage.Address))
            throw Panic(LogLevel.Error, "MT-Add tries to add a loco _again_");

        var locoRow = _locoTable.GetRowForAddress(mtMessage.Address);
        if (locoRow.IsActive)
        {
            _logger.LogError("got loco row for address {LocoRowAddress} that is already active, refusing to steal!", locoRow.Address);
            await _customTcpClient?.SendMessageAsync($"M{mtMessage.ThrottleId}S{mtMessage.Address.EncodeWtAddress()}{Constants.Separator}")!;
        }

        _logger.LogInformation("{Name}: got loco row for address {LocoRowAddress}, activating now", Name, locoRow.Address);

        lock (_locoTable) // lock scope is entire table in order to synchronize with cleanup thread
        {
            _myLocos.Add(mtMessage.Address, locoRow);
            locoRow.Activate();
        }

        var (csResult, slotData) = await locoRow.WaitForSlotsAsync(_stoppingToken);
        switch (csResult)
        {
            case OccupySlotResult.Success:
                _logger.Log(LogLevel.Information, "{Name}: command station success", Name);
                await _customTcpClient?.SendMessageAsync($"M{mtMessage.ThrottleId}+{mtMessage.Address.EncodeWtAddress()}{Constants.Separator}")!;
                var prefix = $"M{mtMessage.ThrottleId}A{mtMessage.Address.EncodeWtAddress()}{Constants.Separator}";
                var slot = slotData!.Value; // not null in this case
                await _customTcpClient?.SendMessageAsync($"{prefix}V{slot.SlotSpeed.WiThrottle}")!;
                await _customTcpClient?.SendMessageAsync($"{prefix}R{(int)slot.SlotDirection}")!;
                //TODO forward functions
                return; // finished for now

            case OccupySlotResult.Occupied:
                _logger.LogError("{Name}: Loconet sais the address {Address} is already active, refusing to steal, deactivating!", Name, mtMessage.Address);
                await _customTcpClient?.SendMessageAsync($"M{mtMessage.ThrottleId}S{mtMessage.Address.EncodeWtAddress()}{Constants.Separator}")!;
                break; // deactivate below

            default: // failure
                _logger.LogWarning("{Name}: command station failure for address {Address}, deactivating", Name, mtMessage.Address);
                //TODO: what to answer for failure???
                break; // deactivate below
        }

        lock (_locoTable)
        {
            locoRow.Deactivate();
            _myLocos.Remove(mtMessage.Address);
        }
    }

    private void ForSelectedRows(IAddress? address, Action<IThrottle2Row> action)
    {
        if (address == null) // wildcard -> all my locos
        {
            foreach (var row in _myLocos.Values)
                action(row);
        }
        else // selected single loco
        {
            if (_myLocos.ContainsKey(address))
                action(_myLocos[address]);
            else
                _logger.LogWarning("{Name}: loco {Address} is not currently under control!?", Name, address);
        }
    }

    private IEnumerable<IThrottle2Row> SelectedRows(IAddress? address)
    {
        if (address == null)
        {
            return _myLocos.Values;
        }

        return [_myLocos[address]];
    }

    private InvalidOperationException Panic(LogLevel level, string msg)
    {
        _logger.Log(level, msg);

        return new InvalidOperationException($"{Name}: {msg}");
    }

    #endregion
}
