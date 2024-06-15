using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Loconet
{
    public abstract class LoconetMessage
    {
        public readonly byte[] Bytes;

        public class TwoByte : LoconetMessage
        {
            /// <summary>
            /// New instance for two byte message
            /// </summary>
            /// <param name="opcode"></param>
            public TwoByte(byte opcode)
            : base(new byte[] { opcode, 0 })
            {
            }
        }

        public class FourByte : LoconetMessage
        {
            /// <summary>
            /// New instance for four byte message
            /// </summary>
            /// <param name="opcode"></param>
            /// <param name="param1"></param>
            /// <param name="param2"></param>
            public FourByte(byte opcode, byte param1, byte param2)
            : base(new byte[] { opcode, param1, param2, 0 })
            {
            }
        }

        public class Unknown : LoconetMessage
        {
            public Unknown(byte[] bytes)
            : base(bytes)
            { }
        }

        /// <summary>
        /// Derived messages may initialize for a custom message
        /// </summary>
        /// <param name="bytes"></param>
        protected LoconetMessage(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            
            if (bytes.Length < 2)
                throw new ArgumentException($"too small message of {bytes.Length} bytes", nameof(bytes));

            Bytes = bytes;
        }

        /// <summary>
        /// Hex dump with a space between the byes as used for LoconetOverTcp
        /// </summary>
        public string Hex => BitConverter.ToString(Bytes).Replace('-', ' ');

        /// <summary>
        /// Set check byte at end of message to correct value
        /// This will be called by LoconetClient.BlockingSend() - user does not need to take care
        /// </summary>
        public void SetCheckByte()
        {
            Bytes[Bytes.Length - 1] = CalcCheck();
        }

        private byte CalcCheck()
        {
            byte work = 0xff;

            for (int i = 0; i < Bytes.Length-1; i++)
                work ^= Bytes[i];

            return work;
        }

        public override string ToString()
        {
            return Hex;
        }
    }
}
