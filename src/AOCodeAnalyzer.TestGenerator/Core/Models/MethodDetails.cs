using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.TestGenerator.Core.Models
{
    public class MethodDetails
    {
        public string Name { get; set; }
        public List<string> Parameters { get; set; } = new();
        public string ReturnType { get; set; }
        public string Body { get; set; }
        public MethodComplexity Complexity { get; set; } // Complexité cyclomatique
    }
}
