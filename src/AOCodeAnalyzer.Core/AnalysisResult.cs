using Microsoft.CodeAnalysis;

namespace AOCodeAnalyzer.Core
{
    public abstract class AnalysisResult : IAnalysisResult
    {
        public string RuleId { get; init; }
        public string Message { get; init; }
        public CodeLocation Location { get; init; }
        public SeverityLevel Severity { get; init; }
        public string Suggestion { get; init; }

        protected AnalysisResult(
            string ruleId,
            string message,
            Location location,
            SeverityLevel severity,
            string suggestion = null)
        {
            RuleId = ruleId;
            Message = message;
            Location = ToCodeLocation(location);
            Severity = severity;
            Suggestion = suggestion;
        }


        private static CodeLocation ToCodeLocation(Location location)
        {
            if (location == null || !location.IsInSource)
            {
                return new CodeLocation(-1, -1, -1, -1); // Valeurs par défaut pour indiquer une position inconnue
            }

            var lineSpan = location.GetLineSpan();
            return new CodeLocation(
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.EndLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                lineSpan.EndLinePosition.Character + 1
            );
        }
    }

}