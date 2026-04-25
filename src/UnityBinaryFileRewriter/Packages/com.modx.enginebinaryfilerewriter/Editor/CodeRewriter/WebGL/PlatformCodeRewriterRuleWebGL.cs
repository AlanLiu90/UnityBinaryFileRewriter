using System;
using System.Collections.Generic;
using System.Text;

namespace EngineBinaryFileRewriter
{
    public sealed class PlatformCodeRewriterRuleWebGL : PlatformCodeRewriter
    {
        public override bool IsValid => Symbols != null && Symbols.Length > 0;

        public bool Development;
        public bool ThreadsSupport;
        public bool OptimizeForSize;
        public SymbolWebGL[] Symbols;

        public override bool Match(Dictionary<string, object> parameters)
        {
            if (!parameters.TryGetValue(nameof(Development), out object developmentObj) || !(developmentObj is bool development))
                return false;

            if (development != Development)
                return false;

            if (!parameters.TryGetValue(nameof(ThreadsSupport), out object threadsSupportObj) || !(threadsSupportObj is bool threadsSupport))
                return false;

            if (threadsSupport != ThreadsSupport)
                return false;

            if (!parameters.TryGetValue(nameof(OptimizeForSize), out object optimizeForSizeObj) || !(optimizeForSizeObj is bool optimizeForSize))
                return false;

            if (optimizeForSize != OptimizeForSize)
                return false;

            return true;
        }

        public override void Validate(List<string> errors)
        {
            foreach (var symbol in Symbols)
            {
                if (string.IsNullOrEmpty(symbol.FileName))
                    errors.Add("Symbol's FileName is empty");

                if (symbol.Instructions == null || symbol.Instructions.Length == 0)
                {
                    errors.Add("Symbol's Instructions is empty");
                    continue;
                }

                foreach (var inst in symbol.Instructions)
                {
                    if (string.IsNullOrEmpty(inst.OriginalMachineCode))
                        errors.Add($"Instruction's OriginalMachineCode is empty (Symbol: {symbol.Name})");
                    else if (!Utility.ValidateMachineCode(inst.OriginalMachineCode, Architecture.WASM))
                        errors.Add($"Instruction's OriginalMachineCode is invalid (Symbol: {symbol.Name})");

                    if (string.IsNullOrEmpty(inst.NewMachineCode))
                        errors.Add($"Instruction's NewMachineCode is empty (Symbol: {symbol.Name})");
                    else if (!Utility.ValidateMachineCode(inst.NewMachineCode, Architecture.WASM))
                        errors.Add($"Instruction's NewMachineCode is invalid (Symbol: {symbol.Name})");
                }
            }
        }

        public override string GetTargetString()
        {
            var sb = new StringBuilder();

            sb.Append(GetText(Development));

            if (ThreadsSupport)
                sb.Append("+ThreadsSupport");

            if (!Development && OptimizeForSize)
                sb.Append("+OptimizeForSize");

            return sb.ToString();
        }
    }

    [Serializable]
    public sealed class SymbolWebGL
    {
        public string FileName;
        public string Name;
        public Instruction[] Instructions;
    }
}
