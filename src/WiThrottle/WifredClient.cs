using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.LocoTable;
using Shared.Models;
using WiThrottle.Models.WiFred;

namespace WiThrottle;

public class WifredClient
{
    private readonly ILogger _logger;
    public string Id { get; private set; }
    public string Name { get; private set; }
    public DateTime? ConnectedAt { get; private set; }
    public DateTime LastMessage { get; private set; }
    public int BatteryVoltage { get; private set; }

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

    public async Task StartProcessingAsync(CancellationToken stoppingToken)
    {
        await UpdateBatteryVoltageAsync();
        _stoppingToken = stoppingToken;
        while (!stoppingToken.IsCancellationRequested && IsConnected)
        {
            WiThrottleCommand command = WiThrottleMessageProcessor.ParseCommand(await _customTcpClient!.ReadNextMessageAsync(stoppingToken));
            _logger.LogInformation("[{uid}] Message received: {command}", Id, command);

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (command.Type)
            {
                case CommandType.Quit:
                    _logger.LogInformation("[{uid}] {Name} says bye.", Id, Name);
                    Disconnect();
                    break;
                case CommandType.MultiThrottle:
                    await MultiThrottleAsync($"M{command.Message}");
                    break;
                case CommandType.HeartBeat:
                case CommandType.Unknown:
                    break;
            }

            LastMessage = DateTime.Now;
        }
    }

    public void UpdateConnection(CustomTcpClient client)
    {
        if (IsConnected) Disconnect();

        _myLocos.Clear();
        
        _customTcpClient = client;
        ConnectedAt = DateTime.Now;
    }

    private async Task UpdateBatteryVoltageAsync()
    {
        var url = new Uri($"http://{GetIpAddress()}/api/getConfigXML");
    
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
        
            var xmlContent = await httpClient.GetStringAsync(url, _stoppingToken);
            
            if (xmlContent.StartsWith("<?XML", StringComparison.OrdinalIgnoreCase))
            {
                xmlContent = "<?xml" + xmlContent.Substring(5);
            }
        
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(WiFredConfig));
            using var stringReader = new StringReader(xmlContent);
        
            var config = (WiFredConfig?)serializer.Deserialize(stringReader);
        
            if (config != null)
            {
                BatteryVoltage = config.BatteryVoltage.Value;
                _logger.LogInformation("[{uid}] Battery voltage updated: {BatteryVoltage}mV", Id, BatteryVoltage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{uid}] Failed to update battery voltage from {Url}", Id, url);
        }
    }

    public void Disconnect()
    {
        if (!IsConnected) return;

        ConnectedAt = null;
        _customTcpClient?.Dispose();
        _customTcpClient = null!;
    }

    #region MultiThrottle

    private async Task MultiThrottleAsync(string message)
    {
        char mtIdentifier = message[1]; //TODO: find out what to do with this identifier
        string mtCommand = message[2..];

        string[] commandParts = mtCommand.Split(Constants.Separator);
        string first = commandParts[0];

        var addressPart = first[1..];
        IAddress? address = null; // null means wildcard
        if (!"*".Equals(addressPart))
            address = addressPart.ParseWtAddress();

        switch (first[0])
        {
            case '+': await MtAddAsync(mtIdentifier, address); break;
            case '-': await MtRemoveAsync(mtIdentifier, address); break;
            case 'A': await MtActionAsync(mtIdentifier, address, commandParts[1]); break;
            default: _logger.LogError("[{uid}] {S}: Received unknown command: {C}", Id, Name, first[0]); break;
        }
    }

    private enum ThrottleCommand
    {
        Consist = 'C',
        ConsistLeadFromRoosterEntry = 'c',
        Dispatch = 'd',
        SetAddressFromRoosterEntry = 'E',
        FunctionKey = 'F',
        ForceFunction = 'f',
        Idle = 'I',
        SetLongAddress = 'L',
        MomentaryFunction = 'm',
        AskForCurrentSettings = 'q',
        Quit = 'Q',
        SetDirection = 'R',
        Release = 'r',
        SetShortAddress = 'S',
        SetSpeedSetMode = 's',
        SetVelocity = 'V',
        EmergencyStop = 'X'
    }

    private async Task MtActionAsync(char mtIdentifier, IAddress? address, string second)
    {
        var cmd = second[0];
        var par = second[1..];

        bool ParseBinary(string parameter) => parameter switch
        {
            "0" => false,
            "1" => true,
            _ => throw new ArgumentException(parameter + " must be 0 or 1", nameof(parameter))
        };

        (int number, bool state) ParseFunction()
        {
            var number = int.Parse(par[1..]);
            var state = ParseBinary(par[..1]);
            return (number, state);
        }

        Action<IThrottle2Row>? action = ((ThrottleCommand)cmd) switch
        {
            ThrottleCommand.SetVelocity => row => row.SetSpeed(int.Parse(par)),
            ThrottleCommand.SetDirection => row => row.SetDirection(ParseBinary(par) ? Direction.Forward : Direction.Reverse),
            ThrottleCommand.EmergencyStop => row => row.SetEmergencyStop(),
            ThrottleCommand.FunctionKey => row =>
            {
                var (number, state) = ParseFunction();
                row.SetFunctionKey(number, state);
            },
            ThrottleCommand.ForceFunction => row =>
            {
                var (number, state) = ParseFunction();
                row.ForceFunction(number, state);
            },
            ThrottleCommand.MomentaryFunction => row =>
            {
                var (number, state) = ParseFunction();
                row.SetMomentaryFunction(number, state);
            },
            _ => null
        };

        if (action == null)
            _logger.LogWarning("[{uid}] Command '{Cmd}'={ThrottleCommand} not implemented, ignoring...", Id, cmd, (ThrottleCommand)cmd);
        else
            ForSelectedRows(address, action);
    }

    private async Task MtRemoveAsync(char mtIdentifier, IAddress? address)
    {
        if (address != null && !_myLocos.ContainsKey(address))
        {
            _logger.LogWarning("[{uid}] Throttle wants so remove an address that we don't have under control, ignoring this!", Id);
            return;
        }

        lock (_locoTable)
        {
            ForSelectedRows(address, row =>
            {
                _myLocos.Remove(row.Address);
                row.Deactivate();
            });
        }
    }

    private async Task MtAddAsync(char mtIdentifier, IAddress? address)
    {
        if (address == null)
            throw Panic(LogLevel.Error, "MT-Add with wildcard is not legal");

        if (_myLocos.ContainsKey(address))
            throw Panic(LogLevel.Error, "MT-Add tries to add a loco _again_");

        var locoRow = _locoTable.GetRowForAddress(address);
        if (locoRow.IsActive)
        {
            _logger.LogError("[{uid}] got loco row for address {LocoRowAddress} that is already active, refusing to steal!", Id, locoRow.Address);
            await _customTcpClient?.SendMessageAsync($"M{mtIdentifier}S{address.EncodeWtAddress()}{Constants.Separator}")!;
        }

        _logger.LogInformation("[{uid}] {Name}: got loco row for address {LocoRowAddress}, activating now", Id, Name, locoRow.Address);

        lock (_locoTable) // lock scope is entire table in order to synchronize with cleanup thread
        {
            _myLocos.Add(address, locoRow);
            locoRow.Activate();
        }

        var (csResult, slotData) = await locoRow.WaitForSlotsAsync(_stoppingToken);
        switch (csResult)
        {
            case OccupySlotResult.Success:
                _logger.Log(LogLevel.Information, "[{uid}] {Name}: command station success", Id, Name);
                await _customTcpClient?.SendMessageAsync($"M{mtIdentifier}+{address.EncodeWtAddress()}{Constants.Separator}")!;
                var prefix = $"M{mtIdentifier}A{address.EncodeWtAddress()}{Constants.Separator}";
                var slot = slotData!.Value; // not null in this case
                await _customTcpClient?.SendMessageAsync($"{prefix}V{slot.SlotSpeed.WiThrottle}")!;
                await _customTcpClient?.SendMessageAsync($"{prefix}R{(int)slot.SlotDirection}")!;
                //TODO forward functions
                return; // finished for now

            case OccupySlotResult.Occupied:
                _logger.LogError("[{uid}] {Name}: Loconet sais the address {Address} is already active, refusing to steal, deactivating!", Id, Name, address);
                await _customTcpClient?.SendMessageAsync($"M{mtIdentifier}S{address.EncodeWtAddress()}{Constants.Separator}")!;
                break; // deactivate below

            default: // failure
                _logger.LogWarning("[{uid}] {Name}: command station failure for address {Address}, deactivating", Id, Name, address);
                //TODO: what to answer for failure???
                break; // deactivate below
        }

        lock (_locoTable)
        {
            locoRow.Deactivate();
            _myLocos.Remove(address);
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
                _logger.LogWarning("[{uid}] {Name}: loco {Address} is not currently under control!?", Id, Name, address);
        }
    }

    private IEnumerable<IThrottle2Row> SelectedRows(IAddress? address)
    {
        if (address == null)
            return _myLocos.Values;
        else
            return [_myLocos[address]];
    }

    private Exception Panic(LogLevel level, string msg)
    {
        _logger.Log(level, "[{uid}] {msg}", Id, msg);
        return new InvalidOperationException($"{Name}: {msg}");
    }

    #endregion
}
