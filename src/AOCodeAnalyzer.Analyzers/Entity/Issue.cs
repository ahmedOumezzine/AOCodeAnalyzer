using AOCodeAnalyzer.Analyzers.Enum;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.Analyzers.Entity
{

    public class Issue
    {
        public IssueType Type { get; set; }
        public string Message { get; set; }
        public Location Location { get; set; }
        public string Suggestion { get; set; }
    }
}
