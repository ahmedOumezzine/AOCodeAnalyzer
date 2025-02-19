using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AOCodeAnalyzer.Refactoring
{
    public class IfElseToTernaryRefactoring
    {
        public static void SuggestTernaryRefactoring(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            foreach (var ifStatement in root.DescendantNodes().OfType<IfStatementSyntax>())
            {
                if (ifStatement.Else != null &&
                    ifStatement.Statement is ReturnStatementSyntax return1 &&
                    ifStatement.Else.Statement is ReturnStatementSyntax return2)
                {
                    Console.WriteLine($"🎯 Suggestion : Convertir `if` en opérateur ternaire à la ligne {ifStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
                }
            }
        }
    }
}
