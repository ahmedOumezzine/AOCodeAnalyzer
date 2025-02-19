using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class UnusedVariableAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
            AnalyzeVariables(root);
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
            Console.WriteLine("✅ Aucune variable non utilisée détectée.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des variables non utilisées ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
            Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
            Console.WriteLine($"Suggestion: {result.Suggestion}\n");
        }
    }

    public IEnumerable<NamingConventionResult> GetResults() => _results;

    private void AnalyzeVariables(SyntaxNode root)
    {
        foreach (var variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            var variableName = variable.Identifier.Text;

            // Vérifier si la variable est référencée ailleurs dans le code
            if (!IsVariableUsed(root, variableName))
            {
                AddViolation(
                    ruleId: "UV001",
                    message: $"La variable '{variableName}' est déclarée mais non utilisée.",
                    location: variable.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: variableName,
                    expectedPattern: "Variable utilisée ou supprimée",
                    suggestion: $"Supprimez cette variable si elle n'est pas nécessaire."
                );
            }
        }
    }

    private bool IsVariableUsed(SyntaxNode root, string variableName)
    {
        // Rechercher des références à la variable dans tout l'arbre syntaxique
        return root.DescendantNodes()
                   .OfType<IdentifierNameSyntax>()
                   .Any(identifier => identifier.Identifier.Text == variableName);
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