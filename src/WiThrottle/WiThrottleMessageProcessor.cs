using Microsoft.Extensions.Logging;

namespace WiThrottle;

public static class WiThrottleMessageProcessor
{
    public static WiThrottleCommand ParseCommand(string message)
    {
        WiThrottleCommand response = new() { Type = CommandType.Unknown, Message = message };
        if (string.IsNullOrWhiteSpace(message))
        {
            return response;
        }

        // switch to the wiThrottle command determined by the first character
        switch (message[0])
        {
            case 'N': // Device Name
                response = new WiThrottleCommand { Type = CommandType.Name, Message = message[1..] };

                break;
            case 'H': // Hardware
                char subCommand = message[1];

                response = subCommand switch
                {
                    'U' => // Identifier
                        new WiThrottleCommand { Type = CommandType.Uid, Message = message[2..] },
                    _ => response
                };

                break;
            case 'M':
                response = new WiThrottleCommand { Type = CommandType.MultiThrottle, Message = message[1..] };
                break;
            case 'Q':
                response = new WiThrottleCommand { Type = CommandType.Quit };
                break;
            case '*':
                response = new WiThrottleCommand { Type = CommandType.HeartBeat };
                break;
        }

        return response;
    }

    public static WiThrottleCommand ParseThrottleCommand(string message)
    {
        WiThrottleCommand response = new() { Type = CommandType.Unknown, Message = message };
        if (string.IsNullOrWhiteSpace(message))
        {
            return response;
        }
        // todo: parse the multi throttle command
        return response;
    }
}
