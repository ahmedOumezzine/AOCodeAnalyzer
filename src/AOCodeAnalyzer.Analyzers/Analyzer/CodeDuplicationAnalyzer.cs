using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers.Enum;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AOCodeAnalyzer.Analyzers.Analyzer
{
    public class CodeDuplicationAnalyzer : ICodeAnalyzer<NamingConventionResult>
    {
        private readonly List<NamingConventionResult> _results = new();
        private const int MinDuplicateLength = 10; // Longueur minimale pour considérer une duplication

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
                AnalyzeDuplicateMethods(root);
                AnalyzeDuplicateCodeBlocks(root);
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
                Console.WriteLine("✅ Aucune duplication de code détectée.");
                return;
            }

            Console.WriteLine($"🚨 Rapports de duplication de code ({_results.Count} problèmes) :\n");
            foreach (var result in _results)
            {
                Console.WriteLine($"[{result.Severity}] {result.Message}");
                Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
                Console.WriteLine($"Suggestion: {result.Suggestion}\n");
            }
        }

        public IEnumerable<NamingConventionResult> GetResults() => _results;

        private void AnalyzeDuplicateMethods(SyntaxNode root)
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var methodBodies = new Dictionary<string, List<MethodDeclarationSyntax>>();

            foreach (var method in methods)
            {
                var body = method.Body?.ToString() ?? "";
                if (body.Length >= MinDuplicateLength)
                {
                    if (!methodBodies.ContainsKey(body))
                        methodBodies[body] = new List<MethodDeclarationSyntax>();
                    methodBodies[body].Add(method);
                }
            }

            foreach (var group in methodBodies.Where(g => g.Value.Count > 1))
            {
                var methodNames = group.Value.Select(m => m.Identifier.Text).ToList();
                var locations = group.Value.Select(m => m.GetLocation()).ToList();
                AddIssue(
                    IssueType.CodeStyle,
                    $"Méthodes dupliquées détectées : {string.Join(", ", methodNames)}",
                    (locations.First()),
                    GetRefactoringSuggestions(methodNames)
                );
            }
        }

        private void AnalyzeDuplicateCodeBlocks(SyntaxNode root)
        {
            var blocks = root.DescendantNodes()
                .OfType<BlockSyntax>()
                .Where(b => b.Statements.Count > 0)
                .ToList();

            var blockContents = new Dictionary<string, List<BlockSyntax>>();

            foreach (var block in blocks)
            {
                var content = block.ToString();
                if (content.Length >= MinDuplicateLength)
                {
                    if (!blockContents.ContainsKey(content))
                        blockContents[content] = new List<BlockSyntax>();
                    blockContents[content].Add(block);
                }
            }

            foreach (var group in blockContents.Where(g => g.Value.Count > 1))
            {
                var locations = group.Value.Select(b => b.GetLocation()).ToList();
                AddIssue(
                    IssueType.CodeStyle,
                    $"Bloc de code dupliqué détecté ({group.Value.Count} occurrences).",
                    (locations.First()),
                    "Extrayez ce bloc dans une méthode distincte pour éviter la duplication."
                );
            }
        }

        private void AddIssue(IssueType issueType, string message, Location location, string suggestion)
        {
            _results.Add(new NamingConventionResult(
                ruleId: "CD001",
                message: message,
                location: location,
                severity: SeverityLevel.Warning,
                invalidName: null,
                expectedPattern: null,
                suggestion: suggestion
            ));
        }

        private static string GetRefactoringSuggestions(List<string> methodNames)
        {
            return $"Suggestions pour les méthodes dupliquées ({string.Join(", ", methodNames)}) :\n" +
                   "• Extrayez le code commun dans une méthode distincte.\n" +
                   "• Utilisez des paramètres pour gérer les différences mineures.\n" +
                   "• Envisagez d'utiliser un design pattern comme Template Method si applicable.";
        }

    }
}