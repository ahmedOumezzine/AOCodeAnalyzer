using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AOCodeAnalyzer.TestGenerator.TestGenerators
{
    public class CSharpTestGenerator
    {
        public static string GenerateTests(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            StringBuilder testCode = new StringBuilder();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                // Début du test
                testCode.AppendLine($"// Test pour la méthode {method.Identifier}");
                testCode.AppendLine($"[Fact]"); // xUnit test attribute
                testCode.AppendLine($"public void {method.Identifier}Test()");
                testCode.AppendLine($"{{");

                // Ajouter des tests de base pour les paramètres
                foreach (var parameter in method.ParameterList.Parameters)
                {
                    testCode.AppendLine($"    // Test pour le paramètre {parameter.Identifier}");
                    testCode.AppendLine($"    var param = {parameter.Identifier};");
                    testCode.AppendLine($"    Assert.NotNull(param); // Validation simple");
                }

                // Vérification de la méthode
                testCode.AppendLine($"    var result = {method.Identifier}();");
                testCode.AppendLine($"    Assert.NotNull(result);");
                testCode.AppendLine($"}}");
            }

            return testCode.ToString();
        }
    }
}
