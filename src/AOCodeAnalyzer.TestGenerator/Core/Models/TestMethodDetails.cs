using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.TestGenerator.Core.Models
{
    public class TestMethodDetails
    {
        public string TestName { get; set; }
        public string Conditions { get; set; }
        public string ExpectedReturnType { get; set; }
        public string ApiCall { get; set; }
        public List<string> ConditionPath { get; set; } = new();
    }
}
