using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnhandledAsyncExceptionAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
            AnalyzeAsyncMethods(root);
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
            Console.WriteLine("✅ Aucune exception non gérée détectée dans les méthodes asynchrones.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des exceptions non gérées ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
            Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
            Console.WriteLine($"Suggestion: {result.Suggestion}\n");
        }
    }

    public IEnumerable<NamingConventionResult> GetResults() => _results;

    private void AnalyzeAsyncMethods(SyntaxNode root)
    {
        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            // Vérifier si la méthode est asynchrone
            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                continue;

            // Vérifier si la méthode contient un bloc try-catch
            var hasTryCatch = method.DescendantNodes().OfType<TryStatementSyntax>().Any();

            if (!hasTryCatch)
            {
                AddViolation(
                    ruleId: "AE001",
                    message: $"La méthode asynchrone '{method.Identifier.Text}' ne gère pas les exceptions.",
                    location: method.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: method.Identifier.Text,
                    expectedPattern: "Bloc try-catch",
                    suggestion: $"Ajoutez un bloc try-catch pour gérer les exceptions potentielles."
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