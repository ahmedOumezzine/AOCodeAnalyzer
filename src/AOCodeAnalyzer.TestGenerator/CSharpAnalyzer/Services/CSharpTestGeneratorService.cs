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
            string classTemplate = LoadTemplateFromResource("UnitTestTemplate.txt");
            string methodTemplate = LoadTemplateFromResource("TestMethodTemplate.txt");

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
                    .Replace("{{ConstructorParameters}}", string.Join(", ", suggestions.SelectMany(s => s.Parameters).Distinct().Select(p => $"/* Provide value for {p.Split(' ')[0]} */")))
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
                {
                    // Construire le code "Arrange"
                    string ArrangeCode = string.Join(" >> ", testDetail.ConditionPath);
                    // Construire le code "Act"
                    string actCode = suggestion.Parameters.Any()
                        ? $"var result = _service.{suggestion.MethodName}({string.Join(", ", suggestion.Parameters.Select(p => $"/* Provide value for {p.Split(' ')[0]} */"))});"
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
        private string LoadTemplateFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"{assembly.GetName().Name}.CSharpAnalyzer.Templates.{resourceName}";

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