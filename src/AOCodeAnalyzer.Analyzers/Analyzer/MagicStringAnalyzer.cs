using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MagicStringAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
            AnalyzeMagicStrings(root);
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
            Console.WriteLine("✅ Aucune chaîne magique détectée.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des chaînes magiques ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
            Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
            Console.WriteLine($"Suggestion: {result.Suggestion}\n");
        }
    }

    public IEnumerable<NamingConventionResult> GetResults() => _results;

    private void AnalyzeMagicStrings(SyntaxNode root)
    {
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var stringValue = literal.Token.ValueText;

                // Ignorer les chaînes vides ou triviales
                if (string.IsNullOrWhiteSpace(stringValue) || IsExcluded(stringValue))
                    continue;

                AddViolation(
                    ruleId: "MS001",
                    message: $"Chaîne magique détectée : \"{stringValue}\".",
                    location: literal.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: stringValue,
                    expectedPattern: "Constante ou énumération",
                    suggestion: $"Remplacez cette chaîne par une constante ou une énumération."
                );
            }
        }
    }

    private bool IsExcluded(string value)
    {
        // Liste des chaînes autorisées (par exemple, des chaînes triviales comme "true", "false", etc.)
        var excludedStrings = new HashSet<string>
        {
            "true", "false", "null", "yes", "no", "on", "off"
        };
        return excludedStrings.Contains(value.ToLower());
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

}