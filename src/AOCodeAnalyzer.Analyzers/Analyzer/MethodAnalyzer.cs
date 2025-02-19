using System;
using System.Collections.Generic;
using System.Linq;
using AOCodeAnalyzer.Analyzers.Entity;
using AOCodeAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AOCodeAnalyzer.Analyzers.Analyzer
{

    public class MethodAnalyzer : ICodeAnalyzer<MethodInfoResult>
    {
        private readonly List<MethodInfoResult> _results = new();

        public static List<MethodInfo> GetMethods(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Le code à analyser ne peut pas être nul ou vide.");
            }

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                var methods = new List<MethodInfo>();

                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    var methodInfo = new MethodInfo
                    {
                        Name = method.Identifier.Text,
                        ReturnType = method.ReturnType?.ToString() ?? "void",
                        Parameters = method.ParameterList.Parameters
                            .Select(p => new ParameterInfo
                            {
                                Name = p.Identifier.Text,
                                Type = p.Type?.ToString() ?? "var",
                                DefaultValue = p.Default?.Value?.ToString()
                            })
                            .ToList(),
                        AccessModifiers = method.Modifiers
                            .Where(m => IsAccessModifier(m.Kind()))
                            .Select(m => m.Text)
                            .ToList(),
                        Attributes = method.AttributeLists
                            .SelectMany(a => a.Attributes)
                            .Select(a => a.Name.ToString())
                            .ToList(),
                        Location = (method.GetLocation())
                    };
                    methods.Add(methodInfo);
                }

                return methods;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'extraction des méthodes : {ex.Message}");
                return new List<MethodInfo>();
            }
        }

        public void Analyze(string code)
        {
            _results.Clear();
            var methods = GetMethods(code);

            foreach (var method in methods)
            {
                _results.Add(new MethodInfoResult
                {
                    Name = method.Name,
                    ReturnType = method.ReturnType,
                    Parameters = method.Parameters,
                    AccessModifiers = method.AccessModifiers,
                    Attributes = method.Attributes,
                    Location = ToCodeLocation(method.Location)
                });
            }
        }

        public void DisplayReport()
        {
            if (!_results.Any())
            {
                Console.WriteLine("✅ Aucune méthode détectée.");
                return;
            }

            Console.WriteLine($"📋 Rapport des méthodes ({_results.Count} méthodes) :\n");
            foreach (var result in _results)
            {
                Console.WriteLine($"Méthode : {result.Name}");
                Console.WriteLine($"Retour : {result.ReturnType}");
                Console.WriteLine($"Modificateurs : {string.Join(", ", result.AccessModifiers)}");
                Console.WriteLine($"Attributs : {string.Join(", ", result.Attributes)}");
                Console.WriteLine($"Emplacement : Ligne {result.Location.StartLine}\n");
            }
        }

        public IEnumerable<MethodInfoResult> GetResults() => _results;

        private static bool IsAccessModifier(SyntaxKind kind)
        {
            return kind == SyntaxKind.PublicKeyword ||
                   kind == SyntaxKind.PrivateKeyword ||
                   kind == SyntaxKind.ProtectedKeyword ||
                   kind == SyntaxKind.InternalKeyword;
        }

        private static CodeLocation ToCodeLocation(Location location)
        {
            if (location == null || !location.IsInSource)
            {
                return new CodeLocation(-1, -1, -1, -1);
            }

            var lineSpan = location.GetLineSpan();
            return new CodeLocation(
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.EndLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                lineSpan.EndLinePosition.Character + 1
            );
        }
    }

}