using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lumi.Generators
{
    /// <summary>
    /// Incremental source generator that finds classes with [Observable] and properties
    /// marked [Observable], then generates partial class with INotifyPropertyChanged plumbing.
    /// </summary>
    [Generator]
    public class ObservableGenerator : IIncrementalGenerator
    {
        private const string AttributeFullName = "Lumi.Generators.ObservableAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Emit the attribute source so consumers don't need a runtime reference
            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("ObservableAttribute.g.cs", @"
using System;

namespace Lumi.Generators
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
    internal sealed class ObservableAttribute : Attribute { }
}
");
            });

            // Find class declarations with [Observable]
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsObservableCandidate(node),
                    transform: static (ctx, _) => GetClassInfo(ctx))
                .Where(static info => info is not null);

            // Combine with compilation
            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses,
                static (spc, source) => Execute(source.Left, source.Right!, spc));
        }

        private static bool IsObservableCandidate(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax classDecl
                   && classDecl.AttributeLists.Count > 0
                   && classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        private static ClassDeclarationSyntax? GetClassInfo(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            foreach (var attrList in classDecl.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(attr).Symbol;
                    if (symbol is IMethodSymbol methodSymbol)
                    {
                        var fullName = methodSymbol.ContainingType.ToDisplayString();
                        if (fullName == AttributeFullName)
                            return classDecl;
                    }
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation,
            ImmutableArray<ClassDeclarationSyntax?> classes,
            SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty) return;

            var distinct = classes.Where(c => c is not null).Cast<ClassDeclarationSyntax>()
                .GroupBy(c => GetFullyQualifiedName(c, compilation))
                .Select(g => g.First())
                .ToList();

            foreach (var classDecl in distinct)
            {
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                var observableProperties = classDecl.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(p => HasObservableAttribute(p, model))
                    .ToList();

                if (observableProperties.Count == 0) continue;

                var source = GeneratePartialClass(classSymbol, observableProperties, model);
                var fileName = $"{classSymbol.Name}.Binding.g.cs";
                context.AddSource(fileName, source);
            }
        }

        private static bool HasObservableAttribute(PropertyDeclarationSyntax prop, SemanticModel model)
        {
            foreach (var attrList in prop.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var symbol = model.GetSymbolInfo(attr).Symbol;
                    if (symbol is IMethodSymbol methodSymbol)
                    {
                        var fullName = methodSymbol.ContainingType.ToDisplayString();
                        if (fullName == AttributeFullName)
                            return true;
                    }
                }
            }
            return false;
        }

        private static string GeneratePartialClass(INamedTypeSymbol classSymbol,
            System.Collections.Generic.List<PropertyDeclarationSyntax> properties,
            SemanticModel model)
        {
            var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : classSymbol.ContainingNamespace.ToDisplayString();

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine();

            if (ns != null)
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    partial class {classSymbol.Name} : INotifyPropertyChanged");
            sb.AppendLine("    {");
            sb.AppendLine("        public event PropertyChangedEventHandler? PropertyChanged;");
            sb.AppendLine();
            sb.AppendLine("        protected void OnPropertyChanged(string propertyName)");
            sb.AppendLine("        {");
            sb.AppendLine("            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));");
            sb.AppendLine("        }");

            foreach (var prop in properties)
            {
                var propSymbol = model.GetDeclaredSymbol(prop) as IPropertySymbol;
                if (propSymbol == null) continue;

                var propName = propSymbol.Name;
                var propType = propSymbol.Type.ToDisplayString();
                var fieldName = $"_generated_{ToCamelCase(propName)}";

                sb.AppendLine();
                sb.AppendLine($"        private {propType} {fieldName};");
                sb.AppendLine($"        public {propType} {propName}");
                sb.AppendLine("        {");
                sb.AppendLine($"            get => {fieldName};");
                sb.AppendLine("            set");
                sb.AppendLine("            {");
                sb.AppendLine($"                if (!Equals({fieldName}, value))");
                sb.AppendLine("                {");
                sb.AppendLine($"                    {fieldName} = value;");
                sb.AppendLine($"                    OnPropertyChanged(\"{propName}\");");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");

            if (ns != null)
                sb.AppendLine("}");

            return sb.ToString();
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static string GetFullyQualifiedName(ClassDeclarationSyntax classDecl, Compilation compilation)
        {
            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDecl);
            return symbol?.ToDisplayString() ?? classDecl.Identifier.Text;
        }
    }
}
