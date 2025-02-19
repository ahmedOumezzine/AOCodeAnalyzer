using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AOCodeAnalyzer.Refactoring
{
    public class LoopRefactoring
    {
        public static void SuggestForToForeachRefactoring(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            foreach (var forStatement in root.DescendantNodes().OfType<ForStatementSyntax>())
            {
                Console.WriteLine($"🔄 Suggestion : Convertir la boucle `for` en `foreach` dans la ligne {forStatement.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            }
        }
    }
}
