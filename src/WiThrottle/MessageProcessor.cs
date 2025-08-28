using Shared;
using WiThrottle.Enums;
using WiThrottle.Models;

namespace WiThrottle;

public static class MessageProcessor
{
    public static WiThrottleMessage ParseCommand(string message)
    {
        WiThrottleMessage response = new() { Type = WtCommand.Unknown, Command = message };
        if (string.IsNullOrWhiteSpace(message))
        {
            return response;
        }

        // switch to the wiThrottle command determined by the first character
        switch ((WtCommand)message[0])
        {
            case WtCommand.Name: // Device Name
                response = new WiThrottleMessage { Type = WtCommand.Name, Command = message[1..] };

                break;
            case WtCommand.Hardware: // Hardware
                response = (WtCommand)message[1] switch
                {
                    WtCommand.Uid => // Identifier
                        new WiThrottleMessage { Type = WtCommand.Uid, Command = message[2..] },
                    _ => response
                };

                break;
            case WtCommand.MultiThrottle:
                response = new WiThrottleMessage { Type = WtCommand.MultiThrottle, Command = message[1..] };
                break;
            case WtCommand.Quit:
                response = new WiThrottleMessage { Type = WtCommand.Quit };
                break;
            case WtCommand.HeartBeat:
                response = new WiThrottleMessage { Type = WtCommand.HeartBeat };
                break;
        }

        return response;
    }

    public static MultiThrottleMessage ParseMultiThrottleCommand(string message)
    {
        MultiThrottleMessage response = new();
        if (string.IsNullOrWhiteSpace(message))
        {
            return response;
        }

        response.ThrottleId = message[0];
        response.Command = (MtCommand)message[1];

        string[] commandParts = message[2..].Split(Constants.Separator);

        if (commandParts[0].TryParseWtAddress(out var address))
        {
            response.Address = address;
        }

        response.ThrottleCommandMessage = commandParts[1]; // TODO: figure out if we can parse the command message here

        return response;
    }
}
