
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace AOCodeAnalyzer.TestGenerator
{

    class CodeAnalyzer
    {
        public static List<TestMethodSuggestion> AnalyzeCode(string code)
        {
            // Parse le code source en arbre syntaxique
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            // Liste pour stocker les suggestions de tests
            var testSuggestions = new List<TestMethodSuggestion>();

            // Trouver toutes les méthodes publiques
            var publicMethods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)));

            foreach (var method in publicMethods)
            {
                // Extraire les paramètres de la méthode
                var parameters = method.ParameterList.Parameters.Select(param =>
                    $"{param.Identifier.Text} ({param.Type})").ToList();

                // Analyser la complexité de la méthode (récursivement)
                var complexity = AnalyzeMethodComplexity(method.Body);

                // Générer des détails de tests
                var testDetails = GenerateTestDetails(method.Identifier.Text, method.ReturnType.ToString(), parameters, complexity);

                // Ajouter les suggestions à la liste
                testSuggestions.Add(new TestMethodSuggestion
                {
                    MethodName = method.Identifier.Text,
                    Parameters = parameters,
                    Complexity = complexity,
                    TestDetails = testDetails
                });
            }

            return testSuggestions;
        }

        private static MethodComplexity AnalyzeMethodComplexity(BlockSyntax body)
        {
            var complexity = new MethodComplexity();

            if (body == null) return complexity;

            // Parcourir chaque instruction dans le corps de la méthode
            foreach (var statement in body.Statements)
            {
                AnalyzeStatement(statement, complexity);
            }

            return complexity;
        }

        private static void AnalyzeStatement(StatementSyntax statement, MethodComplexity complexity)
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

        private static List<TestMethodDetails> GenerateTestDetails(string methodName, string returnType, List<string> parameters, MethodComplexity complexity)
        {
            var testDetails = new List<TestMethodDetails>();
            int testIndex = 1;

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

            return testDetails;
        }

        private static string GenerateTestName(string methodName, string conditionPart, string resultCondition, string expectedPart)
        {
            string condition = !string.IsNullOrEmpty(conditionPart) ? $"{conditionPart}" : "";
            string result = !string.IsNullOrEmpty(resultCondition) ? $"When{resultCondition}" : "";
            string expected = !string.IsNullOrEmpty(expectedPart) ? $"{expectedPart}" : "";

            return $"Test_{methodName}_{condition}{result}_{expected}".Replace("__", "_").Trim('_');
        }

        private static string FormatConditionForTestName(string condition)
        {
            condition = condition.Replace(">", "GreaterThan");
            condition = condition.Replace("<", "LessThan");
            condition = condition.Replace("==", "Equals");
            condition = condition.Replace("!=", "NotEquals");
            condition = condition.Replace(">=", "GreaterThanOrEqual");
            condition = condition.Replace("<=", "LessThanOrEqual");
            return condition.Replace(" ", "");
        }

        private static string FormatExpectedResult(string expectedResult)
        {
            if (expectedResult.Contains("throws"))
            {
                return $"Throws{expectedResult.Replace("throws", "")}";
            }
            return $"Returns{expectedResult}";
        }
    }

    public class MethodComplexity
    {
        public int IfCount { get; set; }
        public int SwitchCount { get; set; }
        public int CaseCount { get; set; }
        public int LoopCount { get; set; }
        public int ExceptionCount { get; set; }
        public int ThrowCount { get; set; }
        public HashSet<string> ExceptionTypes { get; set; } = new HashSet<string>();
        public List<string> Conditions { get; set; } = new List<string>();
        public List<string> CaseValues { get; set; } = new List<string>();
        public List<string> ReturnValues { get; set; } = new List<string>();
        public List<string> ApiCalls { get; set; } = new List<string>();
    }

    public class TestMethodDetails
    {
        public string TestName { get; set; }
        public string Conditions { get; set; }
        public string ExpectedReturnType { get; set; }
        public string ApiCall { get; set; }
        public List<string> ConditionPath { get; set; } = new List<string>();
    }

    public class TestMethodSuggestion
    {
        public string MethodName { get; set; }
        public List<string> Parameters { get; set; }
        public MethodComplexity Complexity { get; set; }
        public List<TestMethodDetails> TestDetails { get; set; }
    }

    class Program
    {
        static void Main()
        {
            string code = @"
using System;

public class ComplexLogic
{
    public string ProcessOrder(int quantity, bool isPremiumCustomer)
    {
        if (quantity <= 0)
        {
            return ""InvalidQuantity"";
        }

        if (isPremiumCustomer)
        {
            var apiResult = CallInventoryApi();
            if (apiResult == ""InStock"")
            {
                return ""OrderProcessedWithDiscount"";
            }
            else
            {
                return ""OutOfStockForPremium"";
            }
        }
        else
        {
            var apiResult = CallInventoryApi();
            if (apiResult == ""InStock"")
            {
                return ""OrderProcessedWithoutDiscount"";
            }
            else
            {
                return ""OutOfStockForRegular"";
            }
        }
    }

    private string CallInventoryApi()
    {
        return ""InStock"";
    }
}";

            var testSuggestions = CodeAnalyzer.AnalyzeCode(code);

            foreach (var suggestion in testSuggestions)
            {
                Console.WriteLine($"Méthode: {suggestion.MethodName}");
                Console.WriteLine($"  Paramètres: {string.Join(", ", suggestion.Parameters)}");
                Console.WriteLine($"  Complexité: IF={suggestion.Complexity.IfCount}, SWITCH={suggestion.Complexity.SwitchCount}, CASE={suggestion.Complexity.CaseCount}, LOOP={suggestion.Complexity.LoopCount}, EXCEPTION={suggestion.Complexity.ExceptionCount}, THROW={suggestion.Complexity.ThrowCount}");
                Console.WriteLine("  Tests générés:");
                foreach (var testDetail in suggestion.TestDetails)
                {
                    Console.WriteLine($"    - {testDetail.TestName}");
                    Console.WriteLine($"      Conditions: {testDetail.Conditions}");
                    Console.WriteLine($"      Type de retour attendu: {testDetail.ExpectedReturnType}");
                    if (!string.IsNullOrEmpty(testDetail.ApiCall))
                    {
                        Console.WriteLine($"      Appel API: {testDetail.ApiCall}");
                    }
                    Console.WriteLine($"      Chemin des conditions: {string.Join(" -> ", testDetail.ConditionPath)}");
                }
            }
        }
    }
}