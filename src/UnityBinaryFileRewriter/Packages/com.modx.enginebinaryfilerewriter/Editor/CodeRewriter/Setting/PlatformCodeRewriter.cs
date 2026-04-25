using System.Collections.Generic;

namespace EngineBinaryFileRewriter
{
    public abstract class PlatformCodeRewriter
    {
        public abstract bool IsValid { get; }
        public abstract bool Match(Dictionary<string, object> parameters);
        public abstract void Validate(List<string> errors);
        public abstract string GetTargetString();

        protected static string GetText(bool development)
        {
            return development ? "Development" : "Release";
        }
    }
}
