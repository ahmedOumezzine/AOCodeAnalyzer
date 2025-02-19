using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.Analyzers.Analyzer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AOCodeAnalyzer.Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class AmbiguousNameAnalyzer : ICodeAnalyzer<NamingConventionResult>
    {
        private readonly List<NamingConventionResult> _results = new();
        private static readonly HashSet<string> AmbiguousNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "data", "info", "value", "result", "temp", "item", "obj", "var"
    };

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
                AnalyzeNames(root);
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
                Console.WriteLine("✅ Aucun nom ambigu ou trop court détecté.");
                return;
            }

            Console.WriteLine($"🚨 Rapports de détection des noms ambigus ou trop courts ({_results.Count} problèmes) :\n");
            foreach (var result in _results)
            {
                Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
                Console.WriteLine($"{result.Message}");
                Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
                Console.WriteLine($"Suggestion: {result.Suggestion}\n");
            }
        }

        public IEnumerable<NamingConventionResult> GetResults() => _results;

        private void AnalyzeNames(SyntaxNode root)
        {
            // Analyser les variables locales
            foreach (var variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                var variableName = variable.Identifier.Text;

                if (IsAmbiguousOrTooShort(variableName))
                {
                    AddViolation(
                        ruleId: "AN001",
                        message: $"Le nom de la variable '{variableName}' est ambigu ou trop court.",
                        location: variable.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: variableName,
                        expectedPattern: "Nom descriptif",
                        suggestion: $"Renommez cette variable avec un nom plus descriptif."
                    );
                }
            }

            // Analyser les noms de méthodes
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodName = method.Identifier.Text;

                if (IsAmbiguousOrTooShort(methodName))
                {
                    AddViolation(
                        ruleId: "AN002",
                        message: $"Le nom de la méthode '{methodName}' est ambigu ou trop court.",
                        location: method.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: methodName,
                        expectedPattern: "Nom descriptif",
                        suggestion: $"Renommez cette méthode avec un nom plus descriptif."
                    );
                }
            }

            // Analyser les paramètres de méthode
            foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
            {
                var parameterName = parameter.Identifier.Text;

                if (IsAmbiguousOrTooShort(parameterName))
                {
                    AddViolation(
                        ruleId: "AN003",
                        message: $"Le nom du paramètre '{parameterName}' est ambigu ou trop court.",
                        location: parameter.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: parameterName,
                        expectedPattern: "Nom descriptif",
                        suggestion: $"Renommez ce paramètre avec un nom plus descriptif."
                    );
                }
            }
        }

        private bool IsAmbiguousOrTooShort(string name)
        {
            // Vérifier si le nom est trop court (moins de 3 caractères)
            if (name.Length < 3)
                return true;

            // Vérifier si le nom est ambigu
            return AmbiguousNames.Contains(name.ToLower());
        }

        private void AddViolation(
            string ruleId,
            string message,
            Location location,
            SeverityLevel severity,
            string invalidName,
            string expectedPattern,
            string suggestion)
        {
            _results.Add(new NamingConventionResult(
                ruleId: ruleId,
                message: message,
                location: (location),
                severity: severity,
                invalidName: invalidName,
                expectedPattern: expectedPattern,
                suggestion: suggestion
            ));
        }

        private static CodeLocation ToCodeLocation(Location location)
        {
            if (location == null || !location.IsInSource)
            {
                return new CodeLocation(-1, -1, -1, -1);
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
