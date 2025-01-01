using Shared.Models;

namespace Shared.LocoTable
{
    public interface IThrottle2Row : ICommon2Row
    {
        /// <summary>
        /// If set than there is exactly one wiThrottle client that has this loco row
        /// in its set of controlled addresses.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Throttle wants to start controlling this loco
        /// </summary>
        void Activate();

        /// <summary>
        /// Throttle wants to stop controlling this loco
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Throttle wants to start talking to this loco
        /// </summary>
        /// <param name="cancel">stop waiting for LocoNet</param>
        /// <returns>true if all command stations gave us control over the respective slot, </returns>
        Task<(OccupySlotResult, OccupySlotReply?)> WaitForSlotsAsync(CancellationToken cancel);

        /// <summary>
        /// Throttle sets a function
        /// </summary>
        /// <param name="number">0=F0, 1=F1, ...</param>
        /// <param name="state"></param>
        void SetFunction(int number, FunctionButton state);

        /// <summary>
        /// Throttle set the direction
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        void SetDirection(Direction dir);

        /// <summary>
        /// Throttle sets the speed
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        void SetSpeed(int v);

        /// <summary>
        /// Throttle requests emergency stop
        /// </summary>
        /// <returns></returns>
        void SetEmergencyStop();
    }
}
