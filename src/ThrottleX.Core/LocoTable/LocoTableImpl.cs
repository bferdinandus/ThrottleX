namespace ThrottleX.Core.LocoTable
{
    public class LocoTableImpl : ILoconet2Table
    {
        private Dictionary<int, LocoRowImpl> _table = new();

        public ILoconet2Row this[int index] => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();
    }
}
