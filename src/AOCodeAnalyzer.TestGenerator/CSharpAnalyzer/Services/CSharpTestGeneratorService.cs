using AOCodeAnalyzer.TestGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.TestGenerator.CSharpAnalyzer.Services
{
    public class CSharpTestGeneratorService
    {
        public string GenerateTests(List<TestMethodSuggestion> testSuggestions)
        {
            var result = new StringBuilder();

            // Charger les templates depuis les ressources intégrées
            string classTemplate = LoadTemplateFromResource("CSharpAnalyzer", "UnitTestTemplate.txt");
            string methodTemplate = LoadTemplateFromResource("CSharpAnalyzer", "TestMethodTemplate.txt");

            // Regrouper les suggestions par nom de classe
            var groupedSuggestions = testSuggestions.GroupBy(s => s.ClassName);

            foreach (var group in groupedSuggestions)
            {
                string className = group.Key; // Nom de la classe analysée
                var suggestions = group.ToList();

                // Générer le contenu des méthodes de test pour cette classe
                string testMethodsContent = GenerateTestMethodsContent(className, suggestions, methodTemplate);

                // Remplacer les placeholders dans le template de classe
                string classContent = classTemplate
                    .Replace("{{ClassName}}", className)
                    .Replace("{{ConstructorParameters}}", string.Join(", ", suggestions.SelectMany(s => s.Parameters).Distinct().Select(p => p.Split(' ')[0])))
                    .Replace("{{TestMethodContent}}", testMethodsContent);

                // Ajouter la classe de test au résultat final
                result.AppendLine(classContent);
            }

            return result.ToString();
        }
        private string GenerateTestMethodsContent(string className, List<TestMethodSuggestion> suggestions, string methodTemplate)
        {
            var sb = new StringBuilder();

            foreach (var suggestion in suggestions)
            {
                foreach (var testDetail in suggestion.TestDetails)
                {  // Créer des mocks pour les paramètres complexes
                    var mockDeclarations = new StringBuilder();
                    var mockParameters = new StringBuilder();

                    foreach (var parameter in suggestion.Parameters)
                    {
                        var paramName = parameter.Split(' ')[0];
                        var paramType = parameter.Split(' ')[1].Replace("(", "").Replace(")", "");
                        mockDeclarations.AppendLine($"var mock{paramName} = new Mock<{paramType}>(); \n");
                        mockParameters.Append($"mock{paramName}, ");
 
                    }

                    // Supprimer la virgule finale
                    if (mockParameters.Length > 0)
                        mockParameters.Length -= 2;
                    // Construire le code "Arrange"
                    string ArrangeCode = "// ConditionPath" + string.Join(" >> ", testDetail.ConditionPath);
                    ArrangeCode += "\n " + mockDeclarations;
                    // Construire le code "Act"
                    string actCode = suggestion.Parameters.Any()
                        ? $"var result = _service.{suggestion.MethodName}({mockParameters});"
                        : $"var result = _service.{suggestion.MethodName}();";

                    // Construire le code "Assert"
                    string assertCode = testDetail.ExpectedReturnType == "Exception"
                        ? $"Assert.ThrowsException(() => _service.{suggestion.MethodName}());"
                        : $"Assert.AreEqual({FormatExpectedValue(testDetail.ExpectedReturnType)}, result);";

                    // Remplacer les placeholders dans le template de méthode
                    string methodContent = methodTemplate
                        .Replace("{{TestMethodName}}", FormatTestName(suggestion.MethodName, testDetail))
                        .Replace("{{ArrangeCode}}", ArrangeCode)
                        .Replace("{{ActCode}}", actCode)
                        .Replace("{{AssertCode}}", assertCode);

                    sb.AppendLine(methodContent);
                }
            }

            return sb.ToString();
        }
        private string FormatTestName(string methodName, TestMethodDetails testDetail)
        {
        
            return testDetail.TestName.Replace("__", "_").Trim('_');
        }
        private string FormatExpectedValue(string expectedValue)
        {
            // Si la valeur attendue est une chaîne, ajoutez des guillemets
            if (!string.IsNullOrEmpty(expectedValue) && !expectedValue.Contains("Exception"))
            {
                return $"\"{expectedValue}\"";
            }
            return expectedValue;
        }
        private string LoadTemplateFromResource(string dossier, string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"{assembly.GetName().Name}.{dossier}.Templates.{resourceName}";

            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Template '{resourceName}' not found in embedded resources.");
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
   
    }
}