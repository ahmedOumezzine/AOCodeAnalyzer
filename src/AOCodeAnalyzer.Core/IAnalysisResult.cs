namespace AOCodeAnalyzer.Core
{
    public interface IAnalysisResult
    {
        string RuleId { get; }
        string Message { get; }
        CodeLocation Location { get; }
        SeverityLevel Severity { get; }
        string Suggestion { get; }
    }
    public record CodeLocation(int StartLine, int EndLine, int StartColumn, int EndColumn);

}
