using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.TestGenerator.Core.Models
{
    public class MethodComplexity
    {
        public int IfCount { get; set; }
        public int SwitchCount { get; set; }
        public int LoopCount { get; set; }
        public int ThrowCount { get; set; }
        public int ExceptionCount { get; set; }
        public HashSet<string> ExceptionTypes { get; set; } = new();
        public List<string> Conditions { get; set; } = new();
        public List<string> CaseValues { get; set; } = new();
        public List<string> ReturnValues { get; set; } = new();
        public List<string> ApiCalls { get; set; } = new();
    }
}
