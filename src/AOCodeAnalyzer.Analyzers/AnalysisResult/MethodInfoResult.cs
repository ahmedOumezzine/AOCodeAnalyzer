using AOCodeAnalyzer.Analyzers.Entity;
using AOCodeAnalyzer.Core;

namespace AOCodeAnalyzer.Analyzers
{
    public class MethodInfoResult : AnalysisResult
    {
        public string Name { get; init; }
        public string ReturnType { get; init; }
        public List<ParameterInfo> Parameters { get; init; }
        public List<string> AccessModifiers { get; init; }
        public List<string> Attributes { get; init; }
        public CodeLocation Location { get; init; }

        public MethodInfoResult()
            : base(
                ruleId: "MA001",
                message: "Analyse de la méthode",
                location: null,
                severity: SeverityLevel.Info,
                suggestion: null
            )
        {
        }
    }
}
