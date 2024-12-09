using Microsoft.Extensions.Logging;
using Shared.Models;
using Shared;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Shared.LocoTable;
using System.Net;
using System.Runtime.CompilerServices;

namespace WiThrottle;

public class TcpClientConnection
{
    public TcpClient Client { get; }
    public string ClientId { get; } = Guid.NewGuid().ToString();
    public DateTime ConnectionTime { get; } = DateTime.Now;
    public string Name { get; internal set; }

    private readonly ILogger _logger;
    private readonly CancellationToken _stoppingToken;
    private readonly Stream _stream;
    private readonly IThrottle2Table _locoTable;
    private readonly Dictionary<IAddress, IThrottle2Row> _myLocos = new();

    public TcpClientConnection(ILogger logger, IThrottle2Table locoTable, TcpClient client, CancellationToken stoppingToken)
    {
        _logger = logger;
        _locoTable = locoTable;
        Client = client;
        _stoppingToken = stoppingToken;

        _stream = Client.GetStream();
        Name = ClientId; // will be overwritten when client tells us its name
    }

    internal async Task HandleClientAsync()
    {
        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new();

        // Send welcome message to the client
        await SendMessageAsync("VN2.0");

        int bytesRead;
        while ((bytesRead = await ReadAsync(buffer)) != 0)
        {
            // Convert the received bytes into a string and append to the message buffer
            string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            messageBuffer.Append(receivedText);

//            _logger.LogDebug("Received text from {client}: {receivedText}", Name, receivedText);

            // Extract and process complete messages
            List<string> messages = ExtractCompleteMessages(ref messageBuffer);
            foreach (string message in messages)
            {
                _logger.LogInformation("Processing message from {client}: `{message}`", Name, message);
                await HandleIncomingMessageAsync(message);
            }
        }
    }

    private static List<string> ExtractCompleteMessages(ref StringBuilder messageBuffer)
    {
        string bufferContent = messageBuffer.ToString();
        List<string> messages = [];

        // when the buffer content ends with a newline the last message is considered complete
        // if not complete it should be added back to the messageBuffer and wait for more characters
        bool lastMessageComplete = bufferContent[^1] == '\r' || bufferContent[^1] == '\n';

        // Split the buffer content by newline characters
        string[] splitMessages = bufferContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Add all complete messages to the list
        messages.AddRange(lastMessageComplete ? splitMessages : splitMessages[..^1]);

        // Clear the messageBuffer and append the last (incomplete) part back to it
        messageBuffer.Clear();
        if (!lastMessageComplete)
        {
            messageBuffer.Append(splitMessages[^1]);
        }

        return messages;
    }

    private async Task HandleIncomingMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        char command = message[0];

        switch (command)
        {
            case 'N': // Device Name
                string deviceName = message[1..];
                _logger.LogInformation("Received Name: {deviceName}", deviceName);
                Name = deviceName;

                await SendMessageAsync("*60");

                break;
            case 'H': // Hardware
                char subCommand = message[1];

                switch (subCommand)
                {
                    case 'U': // Identifier
                        string deviceIdentifier = message[2..];
                        _logger.LogInformation("Received Uid: {deviceIdentifier}", deviceIdentifier);
                        break;
                    default:
                        _logger.LogWarning("Unknown sub command: {command} in {message}", command, message);
                        break;
                }

                break;
            case 'M':
                await MultiThrottleAsync(message);
                break;
            case '*':
                break;
            default:
                _logger.LogWarning("Unknown command: {command} in {message}", command, message);
                break;
        }
    }

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
            case '+': await MtAddAsync   (mtIdentifier, address); break;
            case '-': await MtRemoveAsync(mtIdentifier, address); break;
            case 'A': await MtActionAsync(mtIdentifier, address, commandParts[1]); break;
            default:  _logger.LogError($"{Name}: Received unknown command: {first[0]}"); break;
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

        bool ParseBinary() => par switch
        {
            "0" => false,
            "1" => true,
            _ => throw new ArgumentException(par + " must be 0 or 1", nameof(par))
        };

        Action<IThrottle2Row>? action = ((ThrottleCommand)cmd) switch
        {
            ThrottleCommand.SetVelocity   =>  row => row.SetSpeed(int.Parse(par)),
            ThrottleCommand.SetDirection  =>  row => row.SetDirection(ParseBinary() ? Direction.Forward : Direction.Reverse),
            ThrottleCommand.EmergencyStop =>  row => row.SetEmergencyStop(),
            ThrottleCommand.Quit          =>  row => _logger.LogInformation($"{Name} sais bye."),
            _ => null
        };

        if (action == null)
            _logger.LogWarning($"Command '{cmd}'={(ThrottleCommand)cmd} not implemented, ignoring...");
        else 
            ForSelectedRows(address, action);
    }

    private async Task MtRemoveAsync(char mtIdentifier, IAddress? address)
    {
        if (address!=null && !_myLocos.ContainsKey(address))
        {
            _logger.LogWarning($"Throttle wants so remove an address that we don't have under control, ignoring this!");
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
            _logger.LogError($"got loco row for address {locoRow.Address} that is already active, refusing to steal!");
            await SendMessageAsync($"M{mtIdentifier}S{address.EncodeWtAddress()}{Constants.Separator}");
        }

        _logger.LogInformation($"{Name}: got loco row for address {locoRow.Address}, activating now");

        lock (_locoTable) // lock scope is entire table in order to synchronize with cleanup thread
        {
            _myLocos.Add(address, locoRow);
            locoRow.Activate();
        }

        var (csResult, slotData) = await locoRow.WaitForSlotsAsync(_stoppingToken);
        switch (csResult)
        {
            case OccupySlotResult.Success:
                _logger.Log(LogLevel.Information, $"{Name}: command station success");
                await SendMessageAsync($"M{mtIdentifier}+{address.EncodeWtAddress()}{Constants.Separator}");
                var prefix = $"M{mtIdentifier}A{address.EncodeWtAddress()}{Constants.Separator}";
                var slot = slotData!.Value; // not null in this case
                await SendMessageAsync($"{prefix}V{slot.SlotSpeed}");
                await SendMessageAsync($"{prefix}R{(int)slot.SlotDirection}");
                return; // finished for now

            case OccupySlotResult.Occupied:
                _logger.LogError($"{Name}: Loconet sais the address {address} is already active, refusing to steal, deactivating!");
                await SendMessageAsync($"M{mtIdentifier}S{address.EncodeWtAddress()}{Constants.Separator}");
                break; // deactivate below

            default: // failure
                _logger.LogWarning($"{Name}: command station failure for address {address}, deactivating");
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
                _logger.LogWarning($"{Name}: loco {address} is not currently under control!?");
        }
    }

    private IEnumerable<IThrottle2Row> SelectedRows(IAddress? address)
    {
        if (address == null)
            return _myLocos.Values;
        else
            return [ _myLocos[address] ];
    }

    private Exception Panic(LogLevel level, string msg)
    {
        _logger.Log(level, msg);
        return new InvalidOperationException($"{Name}: {msg}");
    }

    private async Task SendMessageAsync(string message)
    {
        byte[] messageToSend = Encoding.UTF8.GetBytes(message + Environment.NewLine);
        await _stream.WriteAsync(messageToSend, _stoppingToken);
        _logger.LogInformation("Sent to {Name}: '{message}'", Name, message);
    }

    private async Task<int> ReadAsync(byte[] buffer)
    {
        return await _stream.ReadAsync(buffer, _stoppingToken);
    }
}
