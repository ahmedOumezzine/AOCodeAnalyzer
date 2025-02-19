using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class CyclomaticComplexityAnalyzer : ICodeAnalyzer<NamingConventionResult>
{
    private readonly List<NamingConventionResult> _results = new();
    private const int MaxCyclomaticComplexity = 10; // Seuil maximal de complexité cyclomatique

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
            Console.WriteLine("✅ Aucune méthode avec une complexité cyclomatique élevée détectée.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des méthodes complexes ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
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
            var complexity = CalculateCyclomaticComplexity(method);

            if (complexity > MaxCyclomaticComplexity)
            {
                AddViolation(
                    ruleId: "CC001",
                    message: $"La méthode '{methodName}' a une complexité cyclomatique élevée ({complexity}).",
                    location: method.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: methodName,
                    expectedPattern: $"Moins de {MaxCyclomaticComplexity} points de complexité",
                    suggestion: $"Découpez cette méthode en sous-méthodes plus petites."
                );
            }
        }
    }

    private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        // Calculer la complexité cyclomatique en comptant les points de décision
        var decisionPoints = method.DescendantNodes()
            .Where(node => IsDecisionPoint(node))
            .Count();

        // Ajouter 1 pour le chemin par défaut
        return decisionPoints + 1;
    }

    private bool IsDecisionPoint(SyntaxNode node)
    {
        // Identifier les points de décision qui augmentent la complexité
        return node.IsKind(SyntaxKind.IfStatement) ||
               node.IsKind(SyntaxKind.ForStatement) ||
               node.IsKind(SyntaxKind.ForEachStatement) ||
               node.IsKind(SyntaxKind.WhileStatement) ||
               node.IsKind(SyntaxKind.DoStatement) ||
               node.IsKind(SyntaxKind.SwitchStatement) ||
               node.IsKind(SyntaxKind.ConditionalExpression);
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