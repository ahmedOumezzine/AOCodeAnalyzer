using AOCodeAnalyzer.TestGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AOCodeAnalyzer.TestGenerator.TypeScriptAnalyzer.Services
{
    public class TSTestGeneratorService
    {
        public string GenerateTests(List<TestMethodSuggestion> testSuggestions)
        {
            var result = new StringBuilder();

            // Charger les templates depuis les ressources intégrées
            string specTemplate = LoadTemplateFromResource("SpecTemplate.txt");
            string testTemplate = LoadTemplateFromResource("TestMethodTemplate.txt");

            // Regrouper les suggestions par nom de classe
            var groupedSuggestions = testSuggestions.GroupBy(s => s.ClassName);

            foreach (var group in groupedSuggestions)
            {
                string className = group.Key; // Nom de la classe analysée
                var suggestions = group.ToList();

                // Générer les mocks pour les paramètres du constructeur
                var mockDeclarations = GenerateMockDeclarations(suggestions.First().ConstructorParameters);

                // Générer le contenu des tests pour cette classe
                string testMethodsContent = GenerateTestMethodsContent(className, suggestions, testTemplate);

                // Remplacer les placeholders dans le template de spécification
                string specContent = specTemplate
                    .Replace("{{ClassName}}", className)
                    .Replace("{{ConstructorParameters}}", string.Join(", ", suggestions.First().ConstructorParameters.Select(p => $"mock{p.Split(':')[0].Trim()}")))
                    .Replace("{{MockDeclarations}}", mockDeclarations)
                    .Replace("{{TestMethodContent}}", testMethodsContent);

                // Ajouter le contenu de la spécification au résultat final
                result.AppendLine(specContent);
            }

            return result.ToString();
        }

        private string GenerateMockDeclarations(List<string> constructorParameters)
        {
            var sb = new StringBuilder();

            foreach (var param in constructorParameters)
            {
                var paramName = param.Split(':')[0].Trim();
                var paramType = param.Split(':')[1].Trim();

                // Créer un mock pour chaque paramètre du constructeur
                sb.AppendLine($"const mock{paramName} = paramType;");
            }

            return sb.ToString();
        }

        private string GenerateTestMethodsContent(string className, List<TestMethodSuggestion> suggestions, string testTemplate)
        {
            var sb = new StringBuilder();

            foreach (var suggestion in suggestions)
            {
                foreach (var testDetail in suggestion.TestDetails)
                {
                    // Construire le code "Arrange"
                    string arrangeCode = string.Join("\n    ", testDetail.ConditionPath.Select(condition => $"// {condition}"));

                    // Construire le code "Act"
                    string actCode = suggestion.Parameters.Any()
                        ? $"const result = component.{suggestion.MethodName}({string.Join(", ", suggestion.Parameters.Select(p =>  p.Split(':')[0].Trim()  ))});"
                        : $"const result = component.{suggestion.MethodName}();";

                    // Construire le code "Assert"
                    string assertCode = testDetail.ExpectedReturnType == "Exception"
                        ? $"expect(() => component.{suggestion.MethodName}()).toThrowError();"
                        : $"expect(result).toEqual({FormatExpectedValue(testDetail.ExpectedReturnType)});";

                    // Remplacer les placeholders dans le template de test
                    string methodContent = testTemplate
                        .Replace("{{TestMethodName}}", testDetail.TestName)
                        .Replace("{{ArrangeCode}}", arrangeCode)
                        .Replace("{{ActCode}}", actCode)
                        .Replace("{{AssertCode}}", assertCode);

                    sb.AppendLine(methodContent);
                }
            }

            return sb.ToString();
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
            var fullResourceName = $"{assembly.GetName().Name}.TypeScriptAnalyzer.Templates.{resourceName}";

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