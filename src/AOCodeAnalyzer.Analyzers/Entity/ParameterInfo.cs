using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.Analyzers.Entity
{
    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DefaultValue { get; set; }

        public override string ToString()
        {
            return DefaultValue != null ? $"{Type} {Name} = {DefaultValue}" : $"{Type} {Name}";
        }
    }

}
