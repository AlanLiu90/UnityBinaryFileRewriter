using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

namespace EngineBinaryFileRewriter
{
    internal abstract class ToolSetBase
    {
        public bool TryGetInstruction(string text, string machineCode, int index, out string line, out int address)
        {
            var pattern = GetInstructionPattern(machineCode);
            var matches = Regex.Matches(text, pattern, RegexOptions.Multiline);
            if (matches.Count == 0)
            {
                line = null;
                address = 0;
                return false;
            }

            Match match;
            if (matches.Count > 1)
            {
                if (index < 0 || index >= matches.Count)
                    throw new Exception($"More than one instruction found, but index is invalid: instructions={matches.Count}, index={index}");

                match = matches[index];
            }
            else
            {
                match = matches[0];
            }

            Assert.IsTrue(match.Groups.Count == 2);

            line = match.Groups[0].Value;
            address = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
            return true;
        }

        protected abstract string GetInstructionPattern(string machineCode);
    }
}
