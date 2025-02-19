using AOCodeAnalyzer.Analyzers.Enum;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AOCodeAnalyzer.Analyzers.Analyzer
{
    public class DeadCodeAnalyzer : ICodeAnalyzer<NamingConventionResult>
    {
        private readonly List<NamingConventionResult> _results = new();

        public void Analyze(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Le code à analyser ne peut pas être nul ou vide.");
            }

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                AnalyzeUnusedUsings(root);
                AnalyzeUnusedMethods(root);
                AnalyzeUnusedVariables(root);
                AnalyzeUnusedClasses(root);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'analyse du code : {ex.Message}");
            }
        }

        public void DisplayReport()
        {
            if (!_results.Any())
            {
                Console.WriteLine("✅ Aucun code mort détecté.");
                return;
            }

            Console.WriteLine($"🚨 Rapports de code mort ({_results.Count} problèmes) :\n");
            foreach (var result in _results)
            {
                Console.WriteLine($"[{result.Severity}] {result.Message}");
                Console.WriteLine($"Location: Ligne {result.Location.StartLine}");
                Console.WriteLine($"Suggestion: {result.Suggestion}\n");
            }
        }

        public IEnumerable<NamingConventionResult> GetResults() => _results;

        private void AnalyzeUnusedUsings(SyntaxNode root)
        {
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
            var usedNamespaces = new HashSet<string>();

            foreach (var node in root.DescendantNodes())
            {
                if (node is QualifiedNameSyntax qualifiedName)
                {
                    usedNamespaces.Add(qualifiedName.Left.ToString());
                }
                else if (node is MemberAccessExpressionSyntax memberAccess)
                {
                    usedNamespaces.Add(memberAccess.Expression.ToString());
                }
            }

            foreach (var usingDirective in usings)
            {
                var namespaceName = usingDirective.Name.ToString();
                if (!usedNamespaces.Contains(namespaceName))
                {
                    AddIssue(
                        IssueType.CodeStyle,
                        $"Directive 'using {namespaceName}' non utilisée.",
                        (usingDirective.GetLocation()),
                        "Supprimez cette directive using inutile."
                    );
                }
            }
        }

        private void AnalyzeUnusedMethods(SyntaxNode root)
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            var methodCalls = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(i => (i.Expression as IdentifierNameSyntax)?.Identifier.Text)
                .Where(name => name != null)
                .ToHashSet();

            foreach (var method in methods)
            {
                var methodName = method.Identifier.Text;
                if (!methodCalls.Contains(methodName) && !IsEntryPoint(method))
                {
                    AddIssue(
                        IssueType.BestPractice,
                        $"Méthode '{methodName}' non utilisée.",
                        (method.GetLocation()),
                        "Supprimez cette méthode ou vérifiez son utilisation."
                    );
                }
            }
        }

        private void AnalyzeUnusedVariables(SyntaxNode root)
        {
            var variables = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().ToList();
            var variableUsages = root.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Select(i => i.Identifier.Text)
                .ToHashSet();

            foreach (var variable in variables)
            {
                var variableName = variable.Identifier.Text;
                if (!variableUsages.Contains(variableName))
                {
                    AddIssue(
                        IssueType.CodeStyle,
                        $"Variable '{variableName}' non utilisée.",
                        (variable.GetLocation()),
                        "Supprimez cette variable ou vérifiez son utilisation."
                    );
                }
            }
        }

        private void AnalyzeUnusedClasses(SyntaxNode root)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            var classUsages = root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Select(o => (o.Type as IdentifierNameSyntax)?.Identifier.Text)
                .Where(name => name != null)
                .ToHashSet();

            foreach (var classDecl in classes)
            {
                var className = classDecl.Identifier.Text;
                if (!classUsages.Contains(className) && !IsProgramEntryClass(classDecl))
                {
                    AddIssue(
                        IssueType.BestPractice,
                        $"Classe '{className}' non utilisée.",
                        (classDecl.GetLocation()),
                        "Supprimez cette classe ou vérifiez son utilisation."
                    );
                }
            }
        }

        private static bool IsEntryPoint(MethodDeclarationSyntax method)
        {
            return method.Identifier.Text == "Main" && method.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        private static bool IsProgramEntryClass(ClassDeclarationSyntax classDecl)
        {
            return classDecl.Identifier.Text == "Program";
        }

        private void AddIssue(IssueType issueType, string message, Location location, string suggestion)
        {
            _results.Add(new NamingConventionResult(
                ruleId: "DC001",
                message: message,
                location: location,
                severity: SeverityLevel.Warning,
                invalidName: null,
                expectedPattern: null,
                suggestion: suggestion
            ));
        }

    }
}