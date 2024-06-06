namespace ThrottleX.Core.Loconet
{
    public class LoconetOptions
    {
        public List<LoconetClientOption> Clients { get; set; } = new();

        public class LoconetClientOption
        {
            public string Host { get; set; } = string.Empty;
            public ushort Port { get; set; }
        }
    }
}
