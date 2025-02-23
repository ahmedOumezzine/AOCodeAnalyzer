using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.TestGenerator.Core.Models
{
    public class TestMethodSuggestion
    {
        public string ClassName { get; set; } // Nom de la classe analysée
        public string MethodName { get; set; } // Nom de la méthode analysée
        public List<string> Parameters { get; set; } = new(); // Paramètres de la méthode
        public MethodComplexity Complexity { get; set; } = new();
        public List<TestMethodDetails> TestDetails { get; set; } = new();
        public List<string> ConstructorParameters { get; set; } = new(); // Paramètres du constructeur
    }
}
