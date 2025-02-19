using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class RedundantMethodNameAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
            Console.WriteLine("✅ Aucun nom de méthode redondant ou incohérent détecté.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des noms de méthodes redondants ou incohérents ({_results.Count} problèmes) :\n");
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
        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var className = classDeclaration.Identifier.Text;

            foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                var methodName = method.Identifier.Text;

                if (IsRedundantOrInconsistent(className, methodName))
                {
                    AddViolation(
                        ruleId: "RM001",
                        message: $"Le nom de la méthode '{methodName}' est redondant ou incohérent par rapport à la classe '{className}'.",
                        location: method.GetLocation(),
                        severity: SeverityLevel.Warning,
                        invalidName: methodName,
                        expectedPattern: "Nom cohérent et non redondant",
                        suggestion: $"Renommez cette méthode pour éviter la redondance ou clarifier son rôle."
                    );
                }
            }
        }
    }

    private bool IsRedundantOrInconsistent(string className, string methodName)
    {
        // Vérifier si le nom de la méthode contient le nom de la classe
        if (methodName.StartsWith(className, StringComparison.OrdinalIgnoreCase))
            return true;

        // Vérifier si le nom de la méthode est trop générique ou ambigu
        var genericNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Process", "Handle", "Execute", "Run", "DoWork"
        };

        return genericNames.Contains(methodName);
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