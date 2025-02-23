


using AOCodeAnalyzer.TestGenerator.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace AOCodeAnalyzer.TestGenerator.Core.Services
{
    public class CodeParserService : ICodeParser
    {
        public   List<TestMethodSuggestion> ParseCode(string code)
        {
            // Parse le code source en arbre syntaxique
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var testSuggestions = new List<TestMethodSuggestion>();

            // Trouver toutes les classes publiques
            var publicClasses = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(cls => cls.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)));

            foreach (var cls in publicClasses)
            {
                string className = cls.Identifier.Text;

                // Extraire les paramètres du constructeur
                var constructorParameters = GetConstructorParameters(cls);

                // Trouver toutes les méthodes publiques dans la classe
                var publicMethods = cls.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)));

                foreach (var method in publicMethods)
                {
                    var parameters = method.ParameterList.Parameters.Select(param =>
                        $"{param.Identifier.Text} ({param.Type})").ToList();

                    var complexity = AnalyzeMethodComplexity(method.Body);

                    var testDetails = GenerateTestDetails(method.Identifier.Text, method.ReturnType.ToString(), parameters, complexity);

                    testSuggestions.Add(new TestMethodSuggestion
                    {
                        ClassName = className, // Stocker le nom de la classe
                        MethodName = method.Identifier.Text,
                        Parameters = parameters,
                        Complexity = complexity,
                        TestDetails = testDetails,
                        ConstructorParameters = constructorParameters // Stocker les paramètres du constructeur
                    });
                }
            }

            return testSuggestions;
        }
 


    private MethodComplexity AnalyzeMethodComplexity(BlockSyntax body)
        {
            var complexity = new MethodComplexity();

            if (body == null) return complexity;

            foreach (var statement in body.Statements)
            {
                AnalyzeStatement(statement, complexity);
            }

            return complexity;
        }
        private static List<string> GetConstructorParameters(ClassDeclarationSyntax classDeclaration)
        {
            var constructor = classDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault();

            if (constructor == null)
            {
                return new List<string>(); // Aucun constructeur trouvé
            }

            return constructor.ParameterList.Parameters
                .Select(param => $"{param.Identifier.Text} ({param.Type})")
                .ToList();
        }
        private void AnalyzeStatement(StatementSyntax statement, MethodComplexity complexity)
        {
            switch (statement)
            {
                case IfStatementSyntax ifStatement:
                    complexity.IfCount++;
                    complexity.Conditions.Add(ifStatement.Condition.ToString());
                    AnalyzeStatement(ifStatement.Statement, complexity);
                    if (ifStatement.Else != null)
                    {
                        AnalyzeStatement(ifStatement.Else.Statement, complexity);
                    }
                    break;

                case SwitchStatementSyntax switchStatement:
                    complexity.SwitchCount++;
                    foreach (var section in switchStatement.Sections)
                    {
                        foreach (var label in section.Labels.OfType<CaseSwitchLabelSyntax>())
                        {
                            complexity.CaseValues.Add(label.Value.ToString());
                        }
                        foreach (var stmt in section.Statements.OfType<ReturnStatementSyntax>())
                        {
                            complexity.ReturnValues.Add(stmt.Expression?.ToString());
                        }
                    }
                    break;

                case ReturnStatementSyntax returnStatement:
                    complexity.ReturnValues.Add(returnStatement.Expression?.ToString());
                    break;

                case ThrowStatementSyntax throwStatement:
                    complexity.ThrowCount++;
                    complexity.ExceptionTypes.Add((throwStatement.Expression as ObjectCreationExpressionSyntax)?.Type.ToString());
                    break;

                case ExpressionStatementSyntax expressionStatement:
                    if (expressionStatement.Expression is InvocationExpressionSyntax invocationExpression)
                    {
                        complexity.ApiCalls.Add(invocationExpression.Expression.ToString());
                    }
                    break;

                case ForStatementSyntax forStatement:
                    complexity.LoopCount++;
                    AnalyzeStatement(forStatement.Statement, complexity);
                    break;

                case WhileStatementSyntax whileStatement:
                    complexity.LoopCount++;
                    AnalyzeStatement(whileStatement.Statement, complexity);
                    break;

                case DoStatementSyntax doStatement:
                    complexity.LoopCount++;
                    AnalyzeStatement(doStatement.Statement, complexity);
                    break;

                case TryStatementSyntax tryStatement:
                    AnalyzeStatement(tryStatement.Block, complexity);
                    foreach (var catchClause in tryStatement.Catches)
                    {
                        complexity.ExceptionCount++;
                        complexity.ExceptionTypes.Add(catchClause.Declaration?.Type.ToString());
                        AnalyzeStatement(catchClause.Block, complexity);
                    }
                    if (tryStatement.Finally != null)
                    {
                        AnalyzeStatement(tryStatement.Finally.Block, complexity);
                    }
                    break;

                case BlockSyntax block:
                    foreach (var innerStatement in block.Statements)
                    {
                        AnalyzeStatement(innerStatement, complexity);
                    }
                    break;

                default:
                    break;
            }
        }

        private List<TestMethodDetails> GenerateTestDetails(string methodName, string returnType, List<string> parameters, MethodComplexity complexity)
        {
            var testDetails = new List<TestMethodDetails>();

            // Générer des tests pour chaque condition détectée
            foreach (var condition in complexity.Conditions)
            {
                var trueConditionPath = new List<string> { $"{condition} == true" };
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, FormatConditionForTestName(condition), "True", FormatExpectedResult(complexity.ReturnValues.FirstOrDefault() ?? returnType)),
                    Conditions = $"Condition: {condition} == true",
                    ExpectedReturnType = complexity.ReturnValues.FirstOrDefault() ?? returnType,
                    ConditionPath = trueConditionPath
                });

                var falseConditionPath = new List<string> { $"{condition} == false" };
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, FormatConditionForTestName(condition), "False", FormatExpectedResult(complexity.ReturnValues.Skip(1).FirstOrDefault() ?? returnType)),
                    Conditions = $"Condition: {condition} == false",
                    ExpectedReturnType = complexity.ReturnValues.Skip(1).FirstOrDefault() ?? returnType,
                    ConditionPath = falseConditionPath
                });
            }

            // Générer des tests pour les valeurs de cas dans un switch
            for (int i = 0; i < complexity.CaseValues.Count; i++)
            {
                var caseConditionPath = new List<string> { $"option == {complexity.CaseValues[i]}" };
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, FormatConditionForTestName($"option == {complexity.CaseValues[i]}"), "", FormatExpectedResult(complexity.ReturnValues.ElementAtOrDefault(i) ?? returnType)),
                    Conditions = $"Condition: Switch value == {complexity.CaseValues[i]}",
                    ExpectedReturnType = complexity.ReturnValues.ElementAtOrDefault(i) ?? returnType,
                    ConditionPath = caseConditionPath
                });
            }

            // Générer des tests pour les exceptions
            foreach (var exceptionType in complexity.ExceptionTypes)
            {
                var exceptionConditionPath = new List<string> { $"Throws {exceptionType}" };
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, "", "", $"Throws{exceptionType}"),
                    Conditions = $"Exception levée: {exceptionType}",
                    ExpectedReturnType = "Exception",
                    ConditionPath = exceptionConditionPath
                });
            }

            // Générer des tests pour les appels API
            foreach (var apiCall in complexity.ApiCalls)
            {
                var successConditionPath = new List<string> { $"{apiCall} returns true" };
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, $"Calls{apiCall}", "Success", "ApiCallSuccess"),
                    Conditions = $"Appel API réussi: {apiCall}",
                    ApiCall = apiCall,
                    ConditionPath = successConditionPath
                });

                var errorConditionPath = new List<string> { $"{apiCall} returns false" };
                testDetails.Add(new TestMethodDetails
                {
                    TestName = GenerateTestName(methodName, $"Calls{apiCall}", "Error", "ApiCallError"),
                    Conditions = $"Appel API échoué: {apiCall}",
                    ApiCall = apiCall,
                    ConditionPath = errorConditionPath
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
        // Méthode d'aide pour ajouter des tests pour une condition donnée
        private void AddTestForCondition(List<TestMethodDetails> testDetails, string methodName, string condition, bool isTrue, string expectedReturnType)
        {
            var conditionValue = isTrue ? "true" : "false";
            var conditionPath = new List<string> { $"{condition} == {conditionValue}" };

            testDetails.Add(new TestMethodDetails
            {
                TestName = GenerateTestName(methodName, FormatConditionForTestName(condition), isTrue ? "True" : "False", FormatExpectedResult(expectedReturnType)),
                Conditions = $"Condition: {condition} == {conditionValue}",
                ExpectedReturnType = expectedReturnType,
                ConditionPath = conditionPath
            });
        }

        // Méthode d'aide pour ajouter des tests pour les appels API
        private void AddApiTest(List<TestMethodDetails> testDetails, string methodName, string apiCall, bool isSuccess, string result)
        {
            var conditionValue = isSuccess ? "true" : "false";
            var conditionPath = new List<string> { $"{apiCall} returns {conditionValue}" };

            testDetails.Add(new TestMethodDetails
            {
                TestName = GenerateTestName(methodName, $"Calls{apiCall}", isSuccess ? "Success" : "Error", result),
                Conditions = $"Appel API {(isSuccess ? "réussi" : "échoué")}: {apiCall}",
                ApiCall = apiCall,
                ConditionPath = conditionPath
            });
        }


        private static string GenerateTestName(string methodName, string conditionPart, string resultCondition, string expectedPart)
        {
            // Formater la condition pour qu'elle soit valide dans un nom de méthode
            string formattedCondition = FormatConditionForTestName(conditionPart);

            // Construire le nom du test
            string condition = !string.IsNullOrEmpty(formattedCondition) ? $"{formattedCondition}" : "";
            string result = !string.IsNullOrEmpty(resultCondition) ? $"When{resultCondition}" : "";
            string expected = !string.IsNullOrEmpty(expectedPart) ? $"{expectedPart}" : "";

            return $"Test_{methodName}_{condition}{result}_{expected}"
                .Replace("__", "_") // Éviter les doubles underscores
                .Trim('_');         // Supprimer les underscores inutiles au début ou à la fin
        }

        private static string FormatConditionForTestName(string condition)
        {
            condition = condition.Replace("\"", "");
            condition = condition.Replace(">", "GreaterThan");
            condition = condition.Replace("<", "LessThan");
            condition = condition.Replace(">=", "GreaterThanOrEqual");
            condition = condition.Replace("<=", "LessThanOrEqual");
            condition = condition.Replace("==", "Equals");
            condition = condition.Replace("!=", "NotEqual");
            condition = condition.Replace("&&", "And");
            condition = condition.Replace("||", "Or");
            condition = condition.Replace("!", "Not");
            condition = condition.Replace("+", "Plus");
            condition = condition.Replace("-", "Minus");
            condition = condition.Replace("*", "Multiply");
            condition = condition.Replace("/", "Divide");
            condition = condition.Replace("== null", "IsNull");
            condition = condition.Replace("!= null", "IsNotNull");

            return condition.Replace(" ", "");
        }

        private   string FormatExpectedResult(string expectedValue)
        {
            if (!string.IsNullOrEmpty(expectedValue) && !expectedValue.Contains("Exception"))
            {
                return FormatConditionForTestName(expectedValue);
            }
            return expectedValue;
        }
    }

}
 