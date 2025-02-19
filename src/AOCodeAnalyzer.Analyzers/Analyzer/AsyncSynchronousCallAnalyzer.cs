using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class AsyncSynchronousCallAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
            AnalyzeAsyncCalls(root);
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
            Console.WriteLine("✅ Aucun appel synchrone dans le code asynchrone détecté.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des appels synchrones dans le code asynchrone ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
            Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
            Console.WriteLine($"Suggestion: {result.Suggestion}\n");
        }
    }

    public IEnumerable<NamingConventionResult> GetResults() => _results;

    private void AnalyzeAsyncCalls(SyntaxNode root)
    {
        foreach (var memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            // Vérifier si l'appel est `.Result` ou `.Wait()`
            if (memberAccess.Name.ToString() == "Result" || memberAccess.Name.ToString() == "Wait")
            {
                AddViolation(
                    ruleId: "AS001",
                    message: $"Appel synchrone détecté : '{memberAccess}'.",
                    location: memberAccess.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: memberAccess.Name.ToString(),
                    expectedPattern: "Utilisation de 'await'",
                    suggestion: $"Remplacez cet appel par une utilisation de 'await' pour éviter les blocages."
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