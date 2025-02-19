using System;
using AOCodeAnalyzer.TestGenerator.TestGenerators;

namespace AOCodeAnalyzer.TestGenerator
{
    public class TestGenerationService
    {
        public static void GenerateTests(string code, string language)
        {
            if (language.Equals("C#", StringComparison.OrdinalIgnoreCase))
            {
                var tests = CSharpTestGenerator.GenerateTests(code);
                Console.WriteLine("Génération des tests pour C# :");
                Console.WriteLine(tests);
            }
            else if (language.Equals("Angular", StringComparison.OrdinalIgnoreCase))
            {
                var tests = AngularTestGenerator.GenerateTests(code);
                Console.WriteLine("Génération des tests pour Angular :");
                Console.WriteLine(tests);
            }
            else
            {
                Console.WriteLine("Langage non supporté.");
            }
        }
    }
}
