using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AOCodeAnalyzer.TestGenerator.TestGenerators
{
    public class AngularTestGenerator
    {
        public static string GenerateTests(string angularCode)
        {
            StringBuilder testCode = new StringBuilder();

            // Simulation de la génération d'un test de composant Angular
            testCode.AppendLine("import { ComponentFixture, TestBed } from '@angular/core/testing';");
            testCode.AppendLine("import { MyComponent } from './my-component.component';");
            testCode.AppendLine();
            testCode.AppendLine("describe('MyComponent', () => {");
            testCode.AppendLine("  let component: MyComponent;");
            testCode.AppendLine("  let fixture: ComponentFixture<MyComponent>;");
            testCode.AppendLine();
            testCode.AppendLine("  beforeEach(async () => {");
            testCode.AppendLine("    await TestBed.configureTestingModule({");
            testCode.AppendLine("      declarations: [MyComponent]");
            testCode.AppendLine("    }).compileComponents();");
            testCode.AppendLine("  });");
            testCode.AppendLine();
            testCode.AppendLine("  beforeEach(() => {");
            testCode.AppendLine("    fixture = TestBed.createComponent(MyComponent);");
            testCode.AppendLine("    component = fixture.componentInstance;");
            testCode.AppendLine("    fixture.detectChanges();");
            testCode.AppendLine("  });");
            testCode.AppendLine();
            testCode.AppendLine("  it('should create', () => {");
            testCode.AppendLine("    expect(component).toBeTruthy();");
            testCode.AppendLine("  });");
            testCode.AppendLine("});");

            return testCode.ToString();
        }
    }
}
