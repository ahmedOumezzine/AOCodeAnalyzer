using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;

namespace AOCodeAnalyzer.Analyzers
{
    public class NamingConventionResult : AnalysisResult
    {
        public string InvalidName { get; init; }
        public string ExpectedPattern { get; init; }

        public NamingConventionResult(
            string ruleId,
            string message,
            Location location,
            SeverityLevel severity,
            string invalidName,
            string expectedPattern,
            string suggestion = null)
            : base(ruleId, message, location, severity, suggestion)
        {
            InvalidName = invalidName;
            ExpectedPattern = expectedPattern;
        }
    }

}