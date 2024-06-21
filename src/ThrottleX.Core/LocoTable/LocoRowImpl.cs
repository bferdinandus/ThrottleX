using Shared.Models;

namespace ThrottleX.Core.LocoTable
{
    public class LocoRowImpl : ILoconet2Row, IDisposable
    {
        private readonly AutoResetEvent _requesting = new(false);

        public LocoRowImpl(IAddress address)
        {
            Address = address;
            LocoRowState = ELocoRowState.Requesting;
            RequestedSpeed.SetStop();
            RequestedDirection = Direction.Forward;
        }

        /// <summary>
        /// After constructon of this object and after putting this object into the loco table,
        /// this method shall be called from the wiThrottle thread that handles one wiFRED.
        /// This returns when a reply from the first command station was processed or a timeout
        /// occures.
        /// </summary>
        /// <returns>true if command station delivered a slot that we can use, 
        /// false if we can't use it or a timeout occured</returns>
        public bool WaitForCommandStation()
        {
            if (_requesting.WaitOne(TimeSpan.FromSeconds(5)))
                return LocoRowState == ELocoRowState.Operational;

            lock (this)
            {
                LocoRowState = ELocoRowState.Inactive; // memorize the failure that we decide here and now
                return false;
            }
        }

        public IAddress Address { get; }

        public ELocoRowState LocoRowState { get; private set; }

        public Speed RequestedSpeed { get; } = new Speed();

        public Direction RequestedDirection { get; private set; }

        public void DeliverCommandStationState(Speed speed, Direction dir, (int index, FunctionButton state)[] functions)
        {
            lock (this)
            {
                switch (LocoRowState)
                {
                    case ELocoRowState.Requesting:
                        RequestedSpeed.WiThrottle = speed.WiThrottle;
                        RequestedDirection = dir;
                        //todo: functions
                        LocoRowState = ELocoRowState.Operational;
                        _requesting.Set();
                        break;

                    case ELocoRowState.Operational:
                        //todo: decide what to do here, another command station delivered faster
                        break;

                    case ELocoRowState.Inactive:
                        //todo: maybe only complain via logging that state is unexpected
                        break;
                }
            }
        }

        public void FetchingFromCommandStationFailed()
        {
            lock (this)
            {
                switch (LocoRowState)
                {
                    case ELocoRowState.Requesting:
                        LocoRowState = ELocoRowState.Inactive;
                        _requesting.Set();
                        break;

                    case ELocoRowState.Operational:
                        //todo: decide what to do here, another command station delivered before we could fail
                        break;

                    case ELocoRowState.Inactive:
                        //todo: maybe only complain via logging that state is unexpected
                        break;
                }
            }
        }

        void IDisposable.Dispose()
        {
            _requesting.Dispose();
        }
    }
}
