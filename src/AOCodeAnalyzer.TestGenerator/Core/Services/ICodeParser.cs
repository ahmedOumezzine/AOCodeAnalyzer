using AOCodeAnalyzer.TestGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOCodeAnalyzer.TestGenerator.Core.Services
{
    public interface ICodeParser
    {
        List<TestMethodSuggestion> ParseCode(string code);
    }
}
