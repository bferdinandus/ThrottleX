namespace WiThrottle.Enums;

public enum ThrottleCommand
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
