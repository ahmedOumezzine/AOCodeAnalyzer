using AOCodeAnalyzer.TestGenerator.Core.Models;
using AOCodeAnalyzer.TestGenerator.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AOCodeAnalyzer.TestGenerator.TypeScriptAnalyzer.Services
{
    public class TSCodeParserService : ICodeParser
    {
        public List<TestMethodSuggestion> ParseCode(string tsContent)
        {
            var testSuggestions = new List<TestMethodSuggestion>();

            // Extraire le nom de la classe
            string className = ExtractClassName(tsContent);
            if (string.IsNullOrEmpty(className))
            {
                Console.WriteLine("Aucune classe trouvée dans le fichier TypeScript.");
                return testSuggestions;
            }

            // Extraire les paramètres du constructeur
            var constructorParameters = ExtractConstructorParameters(tsContent);

            // Extraire les méthodes publiques
            var publicMethods = ExtractPublicMethods(tsContent);

            foreach (var method in publicMethods)
            {
                // Analyser la complexité de la méthode
                var complexity = AnalyzeMethodComplexity(method.Body);

                // Générer des détails de tests
                var testDetails = GenerateTestDetails(method.Name, method.ReturnType, method.Parameters, complexity);

                // Ajouter les suggestions à la liste
                testSuggestions.Add(new TestMethodSuggestion
                {
                    ClassName = className,
                    MethodName = method.Name,
                    Parameters = method.Parameters,
                    ConstructorParameters = constructorParameters,
                    Complexity = complexity,
                    TestDetails = testDetails
                });
            }

            return testSuggestions;
        }
        private string ExtractClassName(string tsContent)
        {
            var classMatch = Regex.Match(tsContent, @"export\s+class\s+(\w+)");
            return classMatch.Success ? classMatch.Groups[1].Value : null;
        }

        private List<string> ExtractConstructorParameters(string tsContent)
        {
            var constructorParams = new List<string>();

            // Rechercher le constructeur de la classe
            var constructorMatch = Regex.Match(tsContent, @"constructor\s*\(([^)]*)\)");
            if (constructorMatch.Success)
            {
                var paramsString = constructorMatch.Groups[1].Value;
                constructorParams = paramsString
                    .Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
            }

            return constructorParams;
        }

        private List<MethodDetails> ExtractPublicMethods(string tsContent)
        {
            var methods = new List<MethodDetails>();

            // Expression régulière pour capturer les méthodes publiques
            var methodMatches = Regex.Matches(
                tsContent,
                @"public\s+(\w+)\s*\(([^)]*)\)\s*:\s*(\w+)?\s*\{([^}]*)\}",
                RegexOptions.Singleline
            );

            foreach (Match match in methodMatches)
            {
                var methodName = match.Groups[1].Value.Trim();
                var parameters = match.Groups[2].Value
                    .Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                var returnType = match.Groups[3].Value.Trim();
                var body = match.Groups[4].Value.Trim()+ "}";

                methods.Add(new MethodDetails
                {
                    Name = methodName,
                    Parameters = parameters,
                    ReturnType = returnType,
                    Body = body
                });
            }

            return methods;
        }
        private MethodComplexity AnalyzeMethodComplexity(string methodBody)
        {
            // Initialiser l'objet de complexité
            var complexity = new MethodComplexity();

            if (string.IsNullOrEmpty(methodBody)) return complexity;
            AnalyzeStatement(methodBody, complexity);
 

            return complexity;
        }
 

        private void AnalyzeStatement(string statement, MethodComplexity complexity)
        {
            // Utiliser des expressions régulières pour analyser les différentes structures
            if (Regex.IsMatch(statement, @"if\s*\("))
            {
                complexity.IfCount++;
                var condition = ExtractConditionFromIf(statement);
                complexity.Conditions.Add(condition);

                // Analyser le corps de l'instruction if
                var body = ExtractBodyFromIf(statement);
                AnalyzeStatement(body, complexity);

                // Analyser la clause else si elle existe
                if (Regex.IsMatch(statement, @"else"))
                {
                    var elseBody = ExtractElseBody(statement);
                    AnalyzeStatement(elseBody, complexity);
                }
            }
            else if (Regex.IsMatch(statement, @"switch\s*\("))
            {
                complexity.SwitchCount++;
                var cases = ExtractCasesFromSwitch(statement);
                foreach (var caseValue in cases.Keys)
                {
                    complexity.CaseValues.Add(caseValue);
                    foreach (var returnValue in cases[caseValue])
                    {
                        complexity.ReturnValues.Add(returnValue);
                    }
                }
            }
            else if (Regex.IsMatch(statement, @"return\s+"))
            {
                var returnValue = ExtractReturnValue(statement);
                complexity.ReturnValues.Add(returnValue);
            }
            else if (Regex.IsMatch(statement, @"throw\s+"))
            {
                complexity.ThrowCount++;
                var exceptionType = ExtractExceptionType(statement);
                complexity.ExceptionTypes.Add(exceptionType);
            }
            else if (Regex.IsMatch(statement, @"\w+\.\w+\s*\("))
            {
                var apiCall = ExtractApiCall(statement);
                complexity.ApiCalls.Add(apiCall);
            }
            else if (Regex.IsMatch(statement, @"for\s*\(|while\s*\(|do\s*\{"))
            {
                complexity.LoopCount++;
                var loopBody = ExtractLoopBody(statement);
                AnalyzeStatement(loopBody, complexity);
            }
            else if (Regex.IsMatch(statement, @"try\s*\{"))
            {
                complexity.ExceptionCount++;
                var tryBlock = ExtractTryBlock(statement);
                AnalyzeStatement(tryBlock, complexity);

                var catchClauses = ExtractCatchClauses(statement);
                foreach (var catchClause in catchClauses)
                {
                    complexity.ExceptionTypes.Add(catchClause);
                    AnalyzeStatement(catchClause, complexity);
                }

                var finallyBlock = ExtractFinallyBlock(statement);
                if (!string.IsNullOrEmpty(finallyBlock))
                {
                    AnalyzeStatement(finallyBlock, complexity);
                }
            }
        }
        private string ExtractConditionFromIf(string statement)
        {
            var match = Regex.Match(statement, @"if\s*\((.+?)\)");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private string ExtractBodyFromIf(string statement)
        {
            var match = Regex.Match(statement, @"if\s*\(.*?\)\s*\{(.+?)\}", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private string ExtractElseBody(string statement)
        {
            var match = Regex.Match(statement, @"else\s*\{(.+?)\}", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private Dictionary<string, List<string>> ExtractCasesFromSwitch(string statement)
        {
            var cases = new Dictionary<string, List<string>>();
            var matches = Regex.Matches(statement, @"case\s+(.+?)\s*:\s*(.+?)(?=break;|default:|case)", RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var caseValue = match.Groups[1].Value.Trim();
                var returnValue = match.Groups[2].Value.Trim();
                if (!cases.ContainsKey(caseValue))
                {
                    cases[caseValue] = new List<string>();
                }
                cases[caseValue].Add(returnValue);
            }

            return cases;
        }

        private string ExtractFinallyBlock(string statement)
        {
            var match = Regex.Match(statement, @"finally\s*\{(.+?)\}", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private string ExtractTryBlock(string statement)
        {
            var match = Regex.Match(statement, @"try\s*\{(.+?)\}", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private string ExtractLoopBody(string statement)
        {
            var match = Regex.Match(statement, @"(for|while)\s*\((.+?)\)\s*\{(.+?)\}|do\s*\{(.+?)\}\s*while\s*\((.+?)\)", RegexOptions.Singleline);
            if (match.Success)
            {
                // For 'for' or 'while' loops
                if (!string.IsNullOrEmpty(match.Groups[3].Value))
                {
                    return match.Groups[3].Value.Trim();
                }
                // For 'do-while' loops
                else if (!string.IsNullOrEmpty(match.Groups[4].Value))
                {
                    return match.Groups[4].Value.Trim();
                }
            }
            return string.Empty;
        }

        private List<string> ExtractCatchClauses(string statement)
        {
            var catchClauses = new List<string>();
            var matches = Regex.Matches(statement, @"catch\s*\((.+?)\)\s*\{(.+?)\}", RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    string exceptionType = match.Groups[1].Value.Trim();
                    string catchBody = match.Groups[2].Value.Trim();
                    catchClauses.Add($"catch ({exceptionType}) {{ {catchBody} }}");
                }
            }

            return catchClauses;
        }



        private string ExtractReturnValue(string statement)
        {
            var match = Regex.Match(statement, @"return\s+(.+?);");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private string ExtractExceptionType(string statement)
        {
            var match = Regex.Match(statement, @"throw\s+new\s+(\w+)");
            return match.Success ? match.Groups[1].Value.Trim() : "Error";
        }

        private string ExtractApiCall(string statement)
        {
            var match = Regex.Match(statement, @"(\w+\.\w+)\s*\(");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }


        private List<TestMethodDetails> GenerateTestDetails(string methodName, string returnType, List<string> parameters, MethodComplexity complexity)
        {
            var testDetails = new List<TestMethodDetails>();

            // Générer des tests pour chaque condition détectée
            foreach (var condition in complexity.Conditions)
            {
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, condition, "True", FormatExpectedResult(complexity.ReturnValues.FirstOrDefault() ?? returnType)),
                    Conditions = $"Condition: {condition} == true",
                    ExpectedReturnType = complexity.ReturnValues.FirstOrDefault() ?? returnType,
                    ConditionPath = new List<string> { $"{condition} == true" }
                });

                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, condition, "False", FormatExpectedResult(complexity.ReturnValues.Skip(1).FirstOrDefault() ?? returnType)),
                    Conditions = $"Condition: {condition} == false",
                    ExpectedReturnType = complexity.ReturnValues.Skip(1).FirstOrDefault() ?? returnType,
                    ConditionPath = new List<string> { $"{condition} == false" }
                });
            }

            // Ajouter un cas de test par défaut si aucune condition n'est détectée
            if (!complexity.Conditions.Any())
            {
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, "", "", FormatExpectedResult(complexity.ReturnValues.FirstOrDefault() ?? returnType)),
                    Conditions = "Default case",
                    ExpectedReturnType = complexity.ReturnValues.FirstOrDefault() ?? returnType,
                    ConditionPath = new List<string> { "Default" }
                });
            }

            return testDetails;
        }

        private static string GenerateTestName(string methodName, string conditionPart, string resultCondition, string expectedPart)
        {
            // Formater la condition pour qu'elle soit valide dans un nom de test
            string formattedCondition = FormatConditionForTestName(conditionPart);

            // Construire le nom du test en fonction du contexte
            string action = !string.IsNullOrEmpty(expectedPart) ? $"should Return '{expectedPart.ToLower()}'" : "should execute";
            string condition = !string.IsNullOrEmpty(formattedCondition) ? $" when '{formattedCondition}'" : "";
            string result = !string.IsNullOrEmpty(resultCondition) ? $" is  '{resultCondition.ToLower()}'" : "";

            // Retourner le nom du test formaté
            return $"{action} {condition} {result}"
                .Replace("__", "_") // Éviter les doubles underscores
                .Trim('_')          // Supprimer les underscores inutiles au début ou à la fin
                .Replace("  ", " ") // Nettoyer les espaces supplémentaires
                .Replace("\"", ""); // Supprimer les guillemets
        }

        private static string FormatConditionForTestName(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return string.Empty;

            // Remplacer les opérateurs par des mots lisibles
            condition = condition.Replace(">", " greater than");
            condition = condition.Replace("<", " less than");
            condition = condition.Replace(">=", " greater than or equal to");
            condition = condition.Replace("<=", " less than or equal to");
            condition = condition.Replace("==", " equals");
            condition = condition.Replace("!=", " not equal to");
            condition = condition.Replace("&&", " and ");
            condition = condition.Replace("||", " or ");

            // Supprimer les caractères non valides
            condition = condition.Replace("\"", "");
            condition = condition.Replace("'", "");

            // Remplacer les espaces multiples par un seul espace
            condition = System.Text.RegularExpressions.Regex.Replace(condition, @"\s+", " ").Trim();

            return condition;
        }

        private string FormatExpectedResult(string expectedValue)
        {
            if (!string.IsNullOrEmpty(expectedValue) && !expectedValue.Contains("Exception"))
            {
                return FormatConditionForTestName(expectedValue);
            }
            return expectedValue;
        }
    }

 
}