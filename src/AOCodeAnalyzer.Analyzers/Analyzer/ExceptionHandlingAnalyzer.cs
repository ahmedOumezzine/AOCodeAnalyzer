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
    public class ExceptionHandlingAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
                Console.WriteLine("✅ Aucune violation de gestion des exceptions détectée.");
                return;
            }

            Console.WriteLine($"🚨 Rapports de gestion des exceptions ({_results.Count} problèmes) :\n");
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
                var tryCatchBlocks = method.Body?.DescendantNodes().OfType<TryStatementSyntax>().ToList();

                if (tryCatchBlocks == null || !tryCatchBlocks.Any())
                {
                    AddIssue(
                        IssueType.BestPractice,
                        $"La méthode '{methodName}' ne contient pas de gestion d'exceptions.",
                        method.GetLocation(),
                        GetRefactoringSuggestions(methodName)
                    );
                }
                else
                {
                    AnalyzeTryCatchBlocks(tryCatchBlocks, methodName);
                }
            }
        }

        private void AnalyzeTryCatchBlocks(List<TryStatementSyntax> tryCatchBlocks, string methodName)
        {
            foreach (var tryCatchBlock in tryCatchBlocks)
            {
                var catches = tryCatchBlock.Catches;

                foreach (var catchClause in catches)
                {
                    if (catchClause.Block.Statements.Count == 0)
                    {
                        AddIssue(
                            IssueType.BestPractice,
                            $"Le bloc catch dans la méthode '{methodName}' est vide.",
                            catchClause.GetLocation(),
                            "Ajoutez une gestion d'erreur appropriée ou loggez l'exception."
                        );
                    }

                    if (catchClause.Declaration == null || catchClause.Declaration.Type.ToString() == "Exception")
                    {
                        AddIssue(
                            IssueType.BestPractice,
                            $"Le bloc catch dans la méthode '{methodName}' utilise une exception générique.",
                            catchClause.GetLocation(),
                            "Utilisez des exceptions spécifiques pour une meilleure gestion des erreurs."
                        );
                    }
                }

                if (tryCatchBlock.Finally == null)
                {
                    AddIssue(
                        IssueType.BestPractice,
                        $"Le bloc try dans la méthode '{methodName}' n'a pas de bloc finally.",
                        tryCatchBlock.GetLocation(),
                        "Ajoutez un bloc finally pour libérer les ressources si nécessaire."
                    );
                }
            }
        }

        private void AddIssue(IssueType issueType, string message, Location location, string suggestion)
        {
            _results.Add(new NamingConventionResult(
                ruleId: "EH001",
                message: message,
                location: location,
                severity: SeverityLevel.Warning,
                invalidName: null,
                expectedPattern: null,
                suggestion: suggestion
            ));
        }

        private static string GetRefactoringSuggestions(string methodName)
        {
            return $"Suggestions pour '{methodName}':\n" +
                   "• Ajoutez un bloc try-catch pour gérer les exceptions potentielles.\n" +
                   "• Utilisez des exceptions spécifiques plutôt que 'Exception'.\n" +
                   "• Loggez les exceptions pour un débogage plus facile.\n" +
                   "• Envisagez d'utiliser un bloc finally pour libérer les ressources.";
        }

    }

}