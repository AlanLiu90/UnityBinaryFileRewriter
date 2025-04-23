using System;
using System.Globalization;
using System.IO;
using UnityEngine.Assertions;

namespace EngineBinaryFileRewriter
{
    [Serializable]
    public sealed class Instruction
    {
        public string OriginalInstructionDescription;
        public string OriginalMachineCode;
        public int Index = -1;
        public string NewInstructionDescription;
        public string NewMachineCode;

        public void Apply(FileStream fs)
        {
            Assert.IsTrue(OriginalMachineCode.Length % 2 == 0);
            Assert.IsTrue(OriginalMachineCode.Length == NewMachineCode.Length);

            long position = fs.Position;

            var buffer = new byte[OriginalMachineCode.Length / 2];
            fs.Read(buffer, 0, buffer.Length);

            int count = 0;
            for (int i = 0; i < NewMachineCode.Length / 2; i++)
            {
                byte code = byte.Parse(NewMachineCode.Substring(i * 2, 2), NumberStyles.HexNumber);
                if (buffer[i] == code)
                    count++;
            }

            if (count == NewMachineCode.Length / 2)
                return;

            for (int i = 0; i < OriginalMachineCode.Length / 2; i++)
            {
                byte code = byte.Parse(OriginalMachineCode.Substring(i * 2, 2), NumberStyles.HexNumber);

                if (buffer[i] != code)
                    throw new Exception($"Machine code isn't matched.(Expected: 0x{code:X}, Actual: 0x{buffer[i]:X})");
            }

            for (int i = 0; i < NewMachineCode.Length / 2; i++)
            {
                byte code = byte.Parse(NewMachineCode.Substring(i * 2, 2), NumberStyles.HexNumber);
                buffer[i] = code;
            }

            fs.Seek(position, SeekOrigin.Begin);
            fs.Write(buffer, 0, buffer.Length);
        }
    }
}
