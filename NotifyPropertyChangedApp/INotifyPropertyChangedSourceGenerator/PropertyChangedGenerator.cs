using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace INotifyPropertyChangedSourceGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class PropertyChangedGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(postInitContext => postInitContext.AddSource("AutoGenerateAttribute.gen.cs",
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

            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var useNrtAnnotations = context.Compilation.Options.NullableContextOptions != NullableContextOptions.Disable;
            var implementationsToGenerate = ((SyntaxReceiver)context.SyntaxContextReceiver!).ImplementationsToGenerate;

            foreach (var implementationToGenerate in implementationsToGenerate)
            {
                var implementation = GenerateImplementation(implementationToGenerate, useNrtAnnotations);
                context.AddSource(implementationToGenerate.Type.Name + ".gen.cs", SourceText.From(implementation, Encoding.UTF8));
            }
        }

        private string GenerateImplementation(ImplementationToGenerate implementationToGenerate, bool useNrtAnnotations)
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
            this.{property.Name}BackingField = value;
            this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({property.Name})));
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

        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<ImplementationToGenerate> ImplementationsToGenerate { get; set; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax
                    || classDeclarationSyntax.AttributeLists.Count == 0)
                {
                    return;
                }

                var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
                if (typeSymbol is null)
                {
                    return;
                }

                var interfacesToImplement = typeSymbol.GetAttributes()
                    .Where(attr => attr.AttributeClass!.ToDisplayString() == "AutoNotify.AutoNotifyAttribute"
                    && attr.ConstructorArguments.Count() == 1
                    && attr.ConstructorArguments[0].Type is not null // ensure no type determination error exists
                    && attr.ConstructorArguments[0].Value is INamedTypeSymbol)
                    .Select(attr => (INamedTypeSymbol)attr.ConstructorArguments[0].Value!)
                    .ToList();

                if (interfacesToImplement.Count > 0)
                {
                    ImplementationsToGenerate.Add(new(typeSymbol, interfacesToImplement));
                }
            }
        }

        public class ImplementationToGenerate {
            public ImplementationToGenerate(INamedTypeSymbol Type, IList<INamedTypeSymbol> Interfaces)
            {
                this.Type = Type;
                this.Interfaces = Interfaces;
            }

            public INamedTypeSymbol Type { get; }

            public IList<INamedTypeSymbol> Interfaces { get; }
        };
    }


}