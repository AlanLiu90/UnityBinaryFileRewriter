using System.Collections.Generic;

namespace EngineBinaryFileRewriter
{
    public class PlatformCodeRewriterGeneral : PlatformCodeRewriter
    {
        public override bool IsValid => Symbols != null && Symbols.Length > 0;

        public bool Development;
        public Architecture Architecture;
        public Symbol[] Symbols;

        public override bool Match(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue(nameof(Development), out object developmentObj) || !(developmentObj is bool development))
                return false;

            if (development != Development)
                return false;

            if (!parameters.TryGetValue(nameof(Architecture), out object architectureObj) || !(architectureObj is Architecture architecture))
                return false;

            if (architecture != Architecture)
                return false;

            return true;
        }

        public override void Validate(List<string> errors)
        {
            foreach (var symbol in Symbols)
            {
                if (string.IsNullOrEmpty(symbol.Pattern))
                    errors.Add("Symbol's Name is empty");

                if (symbol.Instructions == null || symbol.Instructions.Length == 0)
                {
                    errors.Add("Symbol's Instructions is empty");
                    continue;
                }

                foreach (var inst in symbol.Instructions)
                {
                    if (string.IsNullOrEmpty(inst.OriginalMachineCode))
                        errors.Add($"Instruction's OriginalMachineCode is empty (Symbol: {symbol.Pattern})");
                    else if (!Utility.ValidateMachineCode(inst.OriginalMachineCode, Architecture))
                        errors.Add($"Instruction's OriginalMachineCode is invalid (Symbol: {symbol.Pattern})");

                    if (string.IsNullOrEmpty(inst.NewMachineCode))
                        errors.Add($"Instruction's NewMachineCode is empty (Symbol: {symbol.Pattern})");
                    else if (!Utility.ValidateMachineCode(inst.NewMachineCode, Architecture))
                        errors.Add($"Instruction's NewMachineCode is invalid (Symbol: {symbol.Pattern})");
                }
            }
        }

        public override string GetTargetString()
        {
            return $"{Architecture}+{GetText(Development)}";
        }
    }
}
