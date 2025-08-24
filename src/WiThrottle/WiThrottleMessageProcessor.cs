using Microsoft.Extensions.Logging;

namespace WiThrottle;

public static class WiThrottleMessageProcessor
{
    public static WiThrottleMessage HandleMessage(string? message)
    {
        WiThrottleMessage response = new() { Type = CommandType.Unknown, Message = message ?? string.Empty };
        if (string.IsNullOrWhiteSpace(message))
        {
            return response;
        }

        // switch to the wiThrottle command determined by the first character
        switch (message[0])
        {
            case 'N': // Device Name
                response = new WiThrottleMessage { Type = CommandType.Name, Message = message[1..] };

                break;
            case 'H': // Hardware
                char subCommand = message[1];

                response = subCommand switch
                {
                    'U' => // Identifier
                        new WiThrottleMessage { Type = CommandType.Uid, Message = message[2..] },
                    _ => response
                };

                break;
            case 'M':
                //await MultiThrottleAsync(message);
                break;
            case 'Q':
                response = new WiThrottleMessage { Type = CommandType.Quit };
                break;
            case '*':
                response = new WiThrottleMessage { Type = CommandType.KeepAlive };
                break;
        }

        return response;
    }
}
