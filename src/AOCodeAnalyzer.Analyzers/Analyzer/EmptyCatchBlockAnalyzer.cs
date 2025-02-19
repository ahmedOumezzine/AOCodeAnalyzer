using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class EmptyCatchBlockAnalyzer : ICodeAnalyzer<NamingConventionResult>
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
            AnalyzeCatchBlocks(root);
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
            Console.WriteLine("✅ Aucun bloc catch vide ou mal géré détecté.");
            return;
        }

        Console.WriteLine($"🚨 Rapports de détection des blocs catch vides ou mal gérés ({_results.Count} problèmes) :\n");
        foreach (var result in _results)
        {
            Console.WriteLine($"[{result.Severity}] Rule ID: {result.RuleId}");
            Console.WriteLine($"{result.Message}");
            Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
            Console.WriteLine($"Suggestion: {result.Suggestion}\n");
        }
    }

    public IEnumerable<NamingConventionResult> GetResults() => _results;

    private void AnalyzeCatchBlocks(SyntaxNode root)
    {
        foreach (var catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            // Vérifier si le bloc catch est vide ou contient uniquement des instructions triviales
            if (IsEmptyOrTrivial(catchClause.Block))
            {
                AddViolation(
                    ruleId: "EC001",
                    message: $"Bloc catch vide ou mal géré détecté.",
                    location: catchClause.GetLocation(),
                    severity: SeverityLevel.Warning,
                    invalidName: "catch",
                    expectedPattern: "Gestion explicite de l'exception",
                    suggestion: $"Ajoutez une logique de gestion d'exception, comme la journalisation ou la relance."
                );
            }
        }
    }

    private bool IsEmptyOrTrivial(BlockSyntax block)
    {
        // Un bloc est considéré comme vide s'il ne contient aucune instruction
        if (block.Statements.Count == 0)
            return true;

        // Un bloc est considéré comme trivial s'il contient uniquement des instructions vides ou des commentaires
        return block.Statements.All(statement =>
            statement.IsKind(SyntaxKind.EmptyStatement) ||
            statement.IsKind(SyntaxKind.ExpressionStatement) && IsTrivialExpression((ExpressionStatementSyntax)statement));
    }

    private bool IsTrivialExpression(ExpressionStatementSyntax expressionStatement)
    {
        // Vérifier si l'expression est triviale (par exemple, un commentaire ou une opération sans effet)
        return expressionStatement.Expression.ToString().Trim().Length == 0;
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