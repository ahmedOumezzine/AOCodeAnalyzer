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

    public class UninitializedFieldAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
                AnalyzeFields(root);
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
                Console.WriteLine("✅ Aucun champ privé non initialisé détecté.");
                return;
            }

            Console.WriteLine($"🚨 Rapports de détection des champs privés non initialisés ({_results.Count} problèmes) :\n");
            foreach (var result in _results)
            {
                Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
                Console.WriteLine($"{result.Message}");
                Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
                Console.WriteLine($"Suggestion: {result.Suggestion}\n");
            }
        }

        public IEnumerable<NamingConventionResult> GetResults() => _results;

        private void AnalyzeFields(SyntaxNode root)
        {
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                // Vérifier si le champ est privé
                if (!field.Modifiers.Any(SyntaxKind.PrivateKeyword))
                    continue;

                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldName = variable.Identifier.Text;

                    // Ignorer les champs avec une initialisation explicite
                    if (variable.Initializer != null)
                        continue;

                    AddViolation(
                        ruleId: "UF001",
                        message: $"Le champ privé '{fieldName}' n'est pas initialisé.",
                        location: variable.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: fieldName,
                        expectedPattern: "Initialisé explicitement ou via un constructeur",
                        suggestion: $"Initialisez ce champ explicitement ou dans le constructeur."
                    );
                }
            }
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
