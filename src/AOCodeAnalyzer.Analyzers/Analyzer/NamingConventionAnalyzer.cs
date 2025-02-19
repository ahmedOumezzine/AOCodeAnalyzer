using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace AOCodeAnalyzer.Analyzers.Analyzer
{
    public class NamingConventionAnalyzer : ICodeAnalyzer<NamingConventionResult>
    {
        private readonly List<NamingConventionResult> _results = new();

        public void Analyze(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Le code à analyser ne peut pas être nul ou vide.");
            }

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                AnalyzeMethods(root);
                AnalyzeClasses(root);
                AnalyzeParameters(root);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'analyse du code : {ex.Message}");
            }
        }

        public void DisplayReport()
        {
            if (!_results.Any())
            {
                Console.WriteLine("✅ Aucune violation de convention de nommage détectée.");
                return;
            }

            Console.WriteLine($"🚨 Rapports de conventions de nommage ({_results.Count} problèmes) :\n");
            foreach (var result in _results)
            {
                Console.WriteLine($"[{result.Severity}] {result.Message}");
                Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
                Console.WriteLine($"Suggestion: {result.Suggestion}\n");
            }
        }

        public IEnumerable<NamingConventionResult> GetResults() => _results;

        private void AnalyzeMethods(SyntaxNode root)
        {
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodName = method.Identifier.Text;
                if (!IsPascalCase(methodName))
                {
                    AddViolation(
                        ruleId: "NC001",
                        message: $"La méthode '{methodName}' n'est pas en PascalCase.",
                        location: method.Identifier.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: methodName,
                        expectedPattern: "PascalCase",
                        suggestion: $"Renommez la méthode en '{ToPascalCase(methodName)}'."
                    );
                }
            }
        }

        private void AnalyzeClasses(SyntaxNode root)
        {
            foreach (var classNode in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = classNode.Identifier.Text;
                if (!IsPascalCase(className))
                {
                    AddViolation(
                        ruleId: "NC002",
                        message: $"La classe '{className}' n'est pas en PascalCase.",
                        location: classNode.Identifier.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: className,
                        expectedPattern: "PascalCase",
                        suggestion: $"Renommez la classe en '{ToPascalCase(className)}'."
                    );
                }
            }
        }

        private void AnalyzeParameters(SyntaxNode root)
        {
            foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
            {
                var parameterName = parameter.Identifier.Text;
                if (!IsCamelCase(parameterName))
                {
                    AddViolation(
                        ruleId: "NC003",
                        message: $"Le paramètre '{parameterName}' n'est pas en camelCase.",
                        location: parameter.Identifier.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: parameterName,
                        expectedPattern: "camelCase",
                        suggestion: $"Renommez le paramètre en '{ToCamelCase(parameterName)}'."
                    );
                }
            }
        }

        private void AddViolation(string ruleId, string message, Location location, SeverityLevel severity, string invalidName, string expectedPattern, string suggestion)
        {
            var result = new NamingConventionResult(
                ruleId,
                message,
                location,
                severity,
                invalidName,
                expectedPattern,
                suggestion
            );
            _results.Add(result);
        }

        private static bool IsPascalCase(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsUpper(name[0]);
        }
        private static bool IsCamelCase(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsLower(name[0]);
        }
        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }
        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLower(input[0]) + input.Substring(1);
        }
    }
}



