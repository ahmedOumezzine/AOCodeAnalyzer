//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.RegularExpressions;

//namespace AOCodeAnalyzer.TestGenerator.TestGenerators
//{
//    class AngularTestGenerator
//    {
//        public static void GenerateSpecFile(string tsFilePath, string outputSpecFilePath)
//        {
//            // Lire le contenu du fichier TypeScript
//            string tsContent = File.ReadAllText(tsFilePath);

//            // Extraire la classe et ses méthodes publiques
//            var className = ExtractClassName(tsContent);
//            var methods = ExtractPublicMethods(tsContent);

//            if (string.IsNullOrEmpty(className))
//            {
//                Console.WriteLine("Aucune classe trouvée dans le fichier TypeScript.");
//                return;
//            }

//            // Générer le contenu du fichier de tests
//            string specContent = GenerateSpecContent(className, methods, tsContent);

//            // Écrire le fichier de tests
//            File.WriteAllText(outputSpecFilePath, specContent);
//            Console.WriteLine($"Fichier de tests généré : {outputSpecFilePath}");
//        }

//        private static string ExtractClassName(string tsContent)
//        {
//            // Rechercher le nom de la classe dans le fichier TypeScript
//            var classMatch = Regex.Match(tsContent, @"export\s+class\s+(\w+)");
//            return classMatch.Success ? classMatch.Groups[1].Value : null;
//        }

//        private static List<MethodDetails> ExtractPublicMethods(string tsContent)
//        {
//            var methods = new List<MethodDetails>();

//            // Rechercher toutes les méthodes publiques dans la classe
//            var methodMatches = Regex.Matches(tsContent, @"public\s+(\w+)\s*\(([^)]*)\)\s*:\s*(\w+)?\s*\{([^}]*)\}");
//            foreach (Match match in methodMatches)
//            {
//                var methodName = match.Groups[1].Value;
//                var parameters = match.Groups[2].Value.Split(',').Select(p => p.Trim()).ToList();
//                var returnType = match.Groups[3].Value;
//                var body = match.Groups[4].Value;

//                // Calculer la complexité cyclomatique
//                int complexity = CalculateComplexity(body);

//                methods.Add(new MethodDetails
//                {
//                    Name = methodName,
//                    Parameters = parameters,
//                    ReturnType = returnType,
//                    Body = body,
//                    Complexity = complexity
//                });
//            }

//            return methods;
//        }

//        private static int CalculateComplexity(string methodBody)
//        {
//            // Calculer la complexité cyclomatique basée sur le nombre de conditions et boucles
//            int complexity = 0;

//            // Compter les conditions (if, else, switch)
//            complexity += Regex.Matches(methodBody, @"if\s*\(|else\s*{|switch\s*\(").Count;

//            // Compter les boucles (for, while, do-while)
//            complexity += Regex.Matches(methodBody, @"for\s*\(|while\s*\(|do\s*\{").Count;

//            // Compter les opérateurs logiques (&&, ||)
//            complexity += Regex.Matches(methodBody, @"\|\||&&").Count;

//            return complexity;
//        }

//        private static string GenerateSpecContent(string className, List<MethodDetails> methods, string tsContent)
//        {
//            var specContent = $@"
//import {{ ComponentFixture, TestBed }} from '@angular/core/testing';
//import {{ {className} }} from './{className.ToLower()}';

//describe('{className}', () => {{
//  let component: {className};

//  beforeEach(() => {{
//    component = new {className}();
//  }});
//";

//            foreach (var method in methods)
//            {
//                specContent += $@"
//  describe('{method.Name}', () => {{
//    // Complexité cyclomatique : {method.Complexity}
//    {(method.Complexity > 5 ? "// SUGGESTION DE REFACTORING : Cette méthode est complexe. Considérez extraire les conditions imbriquées ou diviser la méthode.\n" : "")}
//{GenerateTestsForMethod(method, tsContent)}
//  }});
//";
//            }

//            specContent += "});";
//            return specContent;
//        }

//        private static string GenerateTestsForMethod(MethodDetails method, string tsContent)
//        {
//            var tests = new List<string>();

//            // Générer des tests pour les conditions (if/else)
//            var ifMatches = Regex.Matches(method.Body, @"if\s*\(([^)]+)\)");
//            foreach (Match match in ifMatches)
//            {
//                var condition = match.Groups[1].Value;
//                tests.Add(GenerateIfElseTests(method.Name, condition));
//            }

//            // Générer des tests pour les comparaisons
//            var comparisonMatches = Regex.Matches(method.Body, @"(\w+)\s*(>|<|==|!=|>=|<=)\s*(\w+)");
//            foreach (Match match in comparisonMatches)
//            {
//                var left = match.Groups[1].Value;
//                var operatorSymbol = match.Groups[2].Value;
//                var right = match.Groups[3].Value;
//                tests.Add(GenerateComparisonTests(method.Name, left, operatorSymbol, right));
//            }

//            // Générer des tests pour les exceptions
//            var throwMatches = Regex.Matches(method.Body, @"throw\s+new\s+(\w+)\s*\(([^)]*)\)");
//            foreach (Match match in throwMatches)
//            {
//                var exceptionType = match.Groups[1].Value;
//                tests.Add(GenerateExceptionTests(method.Name, exceptionType));
//            }

//            // Générer des tests pour les appels API
//            var apiCallMatches = Regex.Matches(method.Body, @"this\.(\w+)\s*\(([^)]*)\)");
//            foreach (Match match in apiCallMatches)
//            {
//                var apiName = match.Groups[1].Value;
//                var apiParams = match.Groups[2].Value.Split(',').Select(p => p.Trim()).ToList();
//                tests.Add(GenerateApiCallTests(method.Name, apiName, apiParams));
//            }

//            return string.Join("\n", tests);
//        }

//        private static string GenerateIfElseTests(string methodName, string condition)
//        {
//            return $@"
//    it('should handle true case for {methodName} with condition {condition}', () => {{
//      // TODO: Mock inputs to satisfy {condition} == true
//      const result = component.{methodName}();
//      // TODO: Add assertions for the expected result
//      console.log(result);
//    }});

//    it('should handle false case for {methodName} with condition {condition}', () => {{
//      // TODO: Mock inputs to satisfy {condition} == false
//      const result = component.{methodName}();
//      // TODO: Add assertions for the expected result
//      console.log(result);
//    }});
//";
//        }

//        private static string GenerateComparisonTests(string methodName, string left, string operatorSymbol, string right)
//        {
//            var testCases = new List<string>();
//            switch (operatorSymbol)
//            {
//                case ">":
//                    testCases.Add($"{left} = {right} - 1"); // Inférieur
//                    testCases.Add($"{left} = {right}");      // Égal
//                    testCases.Add($"{left} = {right} + 1"); // Supérieur
//                    break;
//                case "<":
//                    testCases.Add($"{left} = {right} + 1"); // Supérieur
//                    testCases.Add($"{left} = {right}");      // Égal
//                    testCases.Add($"{left} = {right} - 1"); // Inférieur
//                    break;
//                case "==":
//                    testCases.Add($"{left} = {right}");      // Égal
//                    testCases.Add($"{left} = {right} + 1"); // Différent
//                    break;
//                case "!=":
//                    testCases.Add($"{left} = {right}");      // Égal
//                    testCases.Add($"{left} = {right} + 1"); // Différent
//                    break;
//                case ">=":
//                    testCases.Add($"{left} = {right} - 1"); // Inférieur
//                    testCases.Add($"{left} = {right}");      // Égal
//                    testCases.Add($"{left} = {right} + 1"); // Supérieur
//                    break;
//                case "<=":
//                    testCases.Add($"{left} = {right} + 1"); // Supérieur
//                    testCases.Add($"{left} = {right}");      // Égal
//                    testCases.Add($"{left} = {right} - 1"); // Inférieur
//                    break;
//            }

//            var tests = new List<string>();
//            foreach (var testCase in testCases)
//            {
//                tests.Add($@"
//    it('should handle comparison {testCase} for {methodName}', () => {{
//      // TODO: Mock inputs to satisfy {testCase}
//      const result = component.{methodName}();
//      // TODO: Add assertions for the expected result
//      console.log(result);
//    }});
//");
//            }

//            return string.Join("\n", tests);
//        }

//        private static string GenerateExceptionTests(string methodName, string exceptionType)
//        {
//            return $@"
//    it('should throw {exceptionType} for {methodName}', () => {{
//      expect(() => {{
//        component.{methodName}();
//      }}).toThrowError({exceptionType});
//    }});
//";
//        }

//        private static string GenerateApiCallTests(string methodName, string apiName, List<string> apiParams)
//        {
//            return $@"
//    it('should handle successful API call for {methodName}', () => {{
//      spyOn(component['{apiName}'], 'call').and.returnValue(Promise.resolve('success'));
//      component.{methodName}().then(result => {{
//        expect(result).toBe('success');
//      }});
//    }});

//    it('should handle failed API call for {methodName}', () => {{
//      spyOn(component['{apiName}'], 'call').and.returnValue(Promise.reject('error'));
//      component.{methodName}().catch(error => {{
//        expect(error).toBe('error');
//      }});
//    }});
//";
//        }
//    }

//    public class MethodDetails
//    {
//        public string Name { get; set; }
//        public List<string> Parameters { get; set; }
//        public string ReturnType { get; set; }
//        public string Body { get; set; }
//        public int Complexity { get; set; } // Complexité cyclomatique
//    }

//    class Program
//    {
//        static void Main(string[] args)
//        {
//            // Chemin du fichier TypeScript à analyser
//            string tsFilePath = "path/to/your/file.ts";

//            // Chemin du fichier de tests à générer
//            string outputSpecFilePath = "path/to/your/file.spec.ts";

//            // Générer le fichier de tests
//            AngularTestGenerator.GenerateSpecFile(tsFilePath, outputSpecFilePath);
//        }
//    }
//}

