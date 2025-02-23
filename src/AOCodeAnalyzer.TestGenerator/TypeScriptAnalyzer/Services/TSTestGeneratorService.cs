

using AOCodeAnalyzer.TestGenerator.Core.Models;

namespace AOCodeAnalyzer.TestGenerator.TypeScriptAnalyzer.Services
{
    public class TSTestGeneratorService
    {
        public void GenerateTests(List<TestMethodSuggestion> testSuggestions, string outputPath)
        {
            foreach (var suggestion in testSuggestions)
            {
                var specContent = GenerateSpecContent(suggestion.MethodName, suggestion.TestDetails);
                File.WriteAllText(Path.Combine(outputPath, $"{suggestion.MethodName}.spec.ts"), specContent);
            }
        }

        private string GenerateSpecContent(string methodName, List<TestMethodDetails> testDetails)
        {
            var specContent = $@"
import {{ ComponentFixture, TestBed }} from '@angular/core/testing';
import {{ ComponentName }} from './component-name';

describe('{methodName}', () => {{
  let component: ComponentName;

  beforeEach(() => {{
    component = new ComponentName();
  }});

";

            foreach (var detail in testDetails)
            {
                specContent += $@"
  it('{detail.TestName}', () => {{
    // Arrange
    // Act
    const result = component.{methodName}();
    // Assert
    console.log(result);
  }});
";
            }

            specContent += "});";
            return specContent;
        }
    }
}