using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace INotifyPropertyChangedSourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class PropertyChangedGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(postInitContext => postInitContext.AddSource("AutoGenerateAttribute.gen.cs",
                SourceText.From(@"
using System;
namespace AutoNotify
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    [System.Diagnostics.Conditional(""PropertyChangedGenerator_DEBUG"")]
    sealed class AutoNotifyAttribute : Attribute
    {
        private readonly Type propertyInterfaceType;

        public AutoNotifyAttribute(Type propertyInterfaceType)
        {
            this.propertyInterfaceType = propertyInterfaceType;
        }

        public Type PropertyInterfaceType
        {
            get { return this.propertyInterfaceType; }
        }
    }
}", Encoding.UTF8)));

            var useNrtAnnotationsProvider = context.CompilationProvider.Select(static (compilation, _) => compilation.Options.NullableContextOptions != NullableContextOptions.Disable);

            var outputs = context.SyntaxProvider
                .CreateSyntaxProvider(static (syntax, _) => syntax is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0,
                static (ctx, _) =>
                {
                    var cds = ctx.Node as ClassDeclarationSyntax;

                    var typeSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    if (typeSymbol is null)
                    {
                        return null;
                    }

                    var interfacesToImplement = typeSymbol.GetAttributes()
                        .Where(attr => attr.AttributeClass!.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"
                        && attr.ConstructorArguments.Count() == 1
                        && attr.ConstructorArguments[0].Type is not null // ensure no type determination error exists
                        && attr.ConstructorArguments[0].Value is INamedTypeSymbol)
                        .Select(attr => (INamedTypeSymbol)attr.ConstructorArguments[0].Value!)
                        .ToList();

                    if (interfacesToImplement.Count == 0)
                    {
                        return null;
                    }

                    return new ImplementationToGenerate(typeSymbol, interfacesToImplement);
                })
                .Where(x => x is not null)
                .Combine(useNrtAnnotationsProvider)
                .Select(static (inputs, _) =>
                {
                    var (implementationToGenerate, useNrtAnnotations) = inputs;
                    return (Name: implementationToGenerate!.Type.Name + ".gen.cs", Content: GenerateImplementation(implementationToGenerate!, useNrtAnnotations));
                });

            context.RegisterSourceOutput(outputs, (ctx, item) => ctx.AddSource(item.Name, item.Content));
        }

        private static string GenerateImplementation(ImplementationToGenerate implementationToGenerate, bool useNrtAnnotations)
        {
            var targetType = implementationToGenerate.Type;
            var @namespace = targetType.ContainingNamespace?.IsGlobalNamespace == true ? null : targetType.ContainingNamespace?.Name;

            var sb = new StringBuilder();
            if (@namespace is not null)
            {
                sb.Append($@"
namespace {@namespace}
{{");
            }

            sb.Append($@"
{SyntaxFacts.GetText(targetType.DeclaredAccessibility)} partial class {targetType.Name} : System.ComponentModel.INotifyPropertyChanged
{{
    public event System.ComponentModel.PropertyChangedEventHandler{(useNrtAnnotations ? "?" : "")} PropertyChanged;

    protected void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
");

            foreach (var interfaceType in implementationToGenerate.Interfaces)
            {
                foreach (var property in interfaceType.GetMembers().OfType<IPropertySymbol>())
                {
                    string assignment = string.Empty;
                    if (useNrtAnnotations && property.Type.IsReferenceType && property.Type.NullableAnnotation != NullableAnnotation.Annotated)
                    {
                        assignment = " = null!";
                    }
                    sb.Append($@"
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    private {property.Type.ToDisplayString()} {property.Name}BackingField{assignment};
    public {property.Type.ToDisplayString()} {property.Name}
    {{
        get
        {{
            return this.{property.Name}BackingField;
        }}
        set
        {{
            if (value != this.{property.Name}BackingField)
            {{
                this.{property.Name}BackingField = value;
                this.OnPropertyChanged(nameof({property.Name}));
            }}
        }}
    }}
");
                }
            }

            sb.Append(@"
}
");

            if (@namespace is not null)
            {
                sb.Append(@"
}
");
            }

            return sb.ToString();
        }

        public class ImplementationToGenerate : IEquatable<ImplementationToGenerate?>
        {
            public ImplementationToGenerate(INamedTypeSymbol Type, IList<INamedTypeSymbol> Interfaces)
            {
                this.Type = Type;
                this.Interfaces = Interfaces;
            }

            public INamedTypeSymbol Type { get; }

            public IList<INamedTypeSymbol> Interfaces { get; }

            public override bool Equals(object? obj)
            {
                return Equals(obj as ImplementationToGenerate);
            }

            public bool Equals(ImplementationToGenerate? other)
            {
                return other is not null &&
                       EqualityComparer<INamedTypeSymbol>.Default.Equals(Type, other.Type) &&
                       EqualityComparer<IList<INamedTypeSymbol>>.Default.Equals(Interfaces, other.Interfaces);
            }

            public override int GetHashCode()
            {
                int hashCode = -466023922;
                hashCode = hashCode * -1521134295 + EqualityComparer<INamedTypeSymbol>.Default.GetHashCode(Type);
                hashCode = hashCode * -1521134295 + EqualityComparer<IList<INamedTypeSymbol>>.Default.GetHashCode(Interfaces);
                return hashCode;
            }

            public static bool operator ==(ImplementationToGenerate? left, ImplementationToGenerate? right)
            {
                return EqualityComparer<ImplementationToGenerate>.Default.Equals(left, right);
            }

            public static bool operator !=(ImplementationToGenerate? left, ImplementationToGenerate? right)
            {
                return !(left == right);
            }
        }
    }


}