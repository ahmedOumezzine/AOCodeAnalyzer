 
using System.Text.RegularExpressions; 
using AOCodeAnalyzer.TestGenerator.Core.Models; 

namespace AOCodeAnalyzer.TestGenerator.TypeScriptAnalyzer.Services
{
    public class TSCodeParserService
    {
        public List<TestMethodSuggestion> ParseCode(string tsContent)
        {
            var testSuggestions = new List<TestMethodSuggestion>();

            // Extraire la classe et ses méthodes publiques
            var className = ExtractClassName(tsContent);
            var methods = ExtractPublicMethods(tsContent);

            if (string.IsNullOrEmpty(className))
            {
                Console.WriteLine("Aucune classe trouvée dans le fichier TypeScript.");
                return testSuggestions;
            }

            foreach (var method in methods)
            {
                var complexity = AnalyzeMethodComplexity(method.Body);

                var testDetails = GenerateTestDetails(method.Name, method.ReturnType, method.Parameters, complexity);

                testSuggestions.Add(new TestMethodSuggestion
                {
                    MethodName = method.Name,
                    Parameters = method.Parameters,
                    Complexity = complexity,
                    TestDetails = testDetails
                });
            }

            return testSuggestions;
        }

        private string ExtractClassName(string tsContent)
        {
            var classMatch = Regex.Match(tsContent, @"export\s+class\s+(\w+)");
            return classMatch.Success ? classMatch.Groups[1].Value : null;
        }

        private List<MethodDetails> ExtractPublicMethods(string tsContent)
        {
            var methods = new List<MethodDetails>();

            var methodMatches = Regex.Matches(tsContent, @"public\s+(\w+)\s*\(([^)]*)\)\s*:\s*(\w+)?\s*\{([^}]*)\}");
            foreach (Match match in methodMatches)
            {
                var methodName = match.Groups[1].Value;
                var parameters = match.Groups[2].Value.Split(',').Select(p => p.Trim()).ToList();
                var returnType = match.Groups[3].Value;
                var body = match.Groups[4].Value;

                var complexity = AnalyzeMethodComplexity(body);

                methods.Add(new MethodDetails
                {
                    Name = methodName,
                    Parameters = parameters,
                    ReturnType = returnType,
                    Body = body,
                    Complexity = complexity
                });
            }

            return methods;
        }

        private MethodComplexity AnalyzeMethodComplexity(string methodBody)
        {
            var complexity = new MethodComplexity();

            // Compter les conditions (if, else, switch)
            complexity.IfCount += Regex.Matches(methodBody, @"if\s*\(|else\s*{|switch\s*\(").Count;

            // Compter les boucles (for, while, do-while)
            complexity.LoopCount += Regex.Matches(methodBody, @"for\s*\(|while\s*\(|do\s*\{").Count;

            // Compter les opérateurs logiques (&&, ||)
            complexity.Conditions.AddRange(Regex.Matches(methodBody, @"\|\||&&").Cast<Match>().Select(m => m.Value));

            return complexity;
        }

        private List<TestMethodDetails> GenerateTestDetails(string methodName, string returnType, List<string> parameters, MethodComplexity complexity)
        {
            var testDetails = new List<TestMethodDetails>();

            foreach (var condition in complexity.Conditions)
            {
                testDetails.Add(new TestMethodDetails
                {
                    TestName = $"should handle {condition} for {methodName}",
                    Conditions = condition,
                    ExpectedReturnType = returnType
                });
            }

            return testDetails;
        }
    } 
}