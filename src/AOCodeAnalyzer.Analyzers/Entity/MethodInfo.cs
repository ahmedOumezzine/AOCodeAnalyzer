using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.Analyzers.Entity
{
    public class MethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
        public List<string> AccessModifiers { get; set; }
        public List<string> Attributes { get; set; }
        public Location Location { get; set; }

        public override string ToString()
        {
            var modifiers = string.Join(" ", AccessModifiers);
            var parameters = string.Join(", ", Parameters.Select(p => p.ToString()));
            var attributes = Attributes.Any() ? $"[{string.Join(", ", Attributes)}] " : "";

            return $"{attributes}{modifiers} {ReturnType} {Name}({parameters})";
        }
    }

}
