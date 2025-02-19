using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class ObsoleteCommentAnalyzer : ICodeAnalyzer<NamingConventionResult>
{
    private readonly List<NamingConventionResult> _results = new();
    private static readonly HashSet<string> ObsoleteKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "TODO",
        "FIXME",
        "HACK",
        "BUG"
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
            AnalyzeComments(root);
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
            Console.WriteLine("✅ Aucun commentaire obsolète ou incorrect détecté.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des commentaires obsolètes ou incorrects ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
            Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
            Console.WriteLine($"Suggestion: {result.Suggestion}\n");
        }
    }

    public IEnumerable<NamingConventionResult> GetResults() => _results;

    private void AnalyzeComments(SyntaxNode root)
    {
        foreach (var trivia in root.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                var commentText = trivia.ToString().TrimStart('/', '*', ' ', '\r', '\n');

                // Ignorer les commentaires vides
                if (string.IsNullOrWhiteSpace(commentText))
                    continue;

                // Vérifier si le commentaire contient des mots-clés obsolètes
                if (ObsoleteKeywords.Any(keyword => commentText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    AddViolation(
                        ruleId: "OC001",
                        message: $"Commentaire obsolète détecté : '{commentText}'.",
                        location: trivia.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: commentText,
                        expectedPattern: "Commentaire mis à jour ou supprimé",
                        suggestion: $"Mettez à jour ou supprimez ce commentaire."
                    );
                }

                // Vérifier si le commentaire semble incorrect (par exemple, trop court ou sans contenu utile)
                if (commentText.Length < 5 && !char.IsLetterOrDigit(commentText.FirstOrDefault()))
                {
                    AddViolation(
                        ruleId: "OC002",
                        message: $"Commentaire incorrect détecté : '{commentText}'.",
                        location: trivia.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: commentText,
                        expectedPattern: "Commentaire clair et utile",
                        suggestion: $"Ajoutez plus de détails ou supprimez ce commentaire."
                    );
                }
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