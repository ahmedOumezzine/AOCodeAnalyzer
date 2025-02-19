//using System;
//using AOCodeAnalyzer.Analyzers;
//using AOCodeAnalyzer.Refactoring;
//using AOCodeAnalyzer.Refactoring;

//namespace AOCodeAnalyzer.Refactoring
//{
//    public class RefactoringService
//    {
//        public static void AnalyzeAndSuggestRefactoring(string code)
//        {
//            Console.WriteLine("🔍 Début de l'analyse du code...");

//            LongMethodAnalyzer.Analyze(code);
//            var duplicates = CodeDuplicationAnalyzer.FindDuplicateMethodes(code);
//            foreach (var dup in duplicates) Console.WriteLine(dup);

//            LoopRefactoring.SuggestForToForeachRefactoring(code);
//            IfElseToTernaryRefactoring.SuggestTernaryRefactoring(code);
//        }
//    }
//}
