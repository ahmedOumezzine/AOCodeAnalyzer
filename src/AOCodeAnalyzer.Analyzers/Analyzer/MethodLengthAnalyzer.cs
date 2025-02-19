using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MethodLengthAnalyzer : ICodeAnalyzer<NamingConventionResult>
{
    private readonly List<NamingConventionResult> _results = new();
    private const int MaxMethodLength = 30; // Nombre maximal de lignes par méthode

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
            Console.WriteLine("✅ Aucune méthode trop longue détectée.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des méthodes trop longues ({_results.Count} problèmes) :\n");
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
            var lineCount = CountLines(method);

            if (lineCount > MaxMethodLength)
            {
                AddViolation(
                    ruleId: "ML001",
                    message: $"La méthode '{methodName}' est trop longue ({lineCount} lignes).",
                    location: method.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: methodName,
                    expectedPattern: $"Moins de {MaxMethodLength} lignes",
                    suggestion: $"Découpez cette méthode en sous-méthodes plus petites."
                );
            }
        }
    }

    private int CountLines(MethodDeclarationSyntax method)
    {
        // Compte le nombre de lignes dans la méthode
        var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line;
        var endLine = method.GetLocation().GetLineSpan().EndLinePosition.Line;
        return endLine - startLine + 1;
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