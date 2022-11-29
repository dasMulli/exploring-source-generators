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

            var propertyChangedGenerationInputs = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                  fullyQualifiedMetadataName: "AutoNotify.AutoNotifyAttribute",
                  predicate: static (syntax, _) => syntax is ClassDeclarationSyntax cds,
                  transform: static (attributeSyntaxContext, _) =>
                  {
                      var interfacesToImplement = new List<INamedTypeSymbol>(attributeSyntaxContext.Attributes.Length);

                      foreach (var attributeData in attributeSyntaxContext.Attributes)
                      {
                          if (attributeData.ConstructorArguments.Length == 1
                            && attributeData.ConstructorArguments[0].Value is INamedTypeSymbol namedTypeSymbol
                            && namedTypeSymbol.TypeKind == TypeKind.Interface)
                          {
                              interfacesToImplement.Add(namedTypeSymbol);
                          }
                      }

                      return new ImplementationToGenerate((INamedTypeSymbol)attributeSyntaxContext.TargetSymbol, interfacesToImplement);
                  }
                )
                .WithComparer(EqualityComparer<ImplementationToGenerate>.Default)
                .WithTrackingName("ImplementationsToGenerate")
                .Combine(useNrtAnnotationsProvider);

            context.RegisterSourceOutput(propertyChangedGenerationInputs, (ctx, inputItem) => 
            {
                var (implementationToGenerate, useNrtAnnotations) = inputItem;
                ctx.AddSource(implementationToGenerate.TypeName + ".gen.cs", GenerateImplementation(implementationToGenerate, useNrtAnnotations));
            });
        }

        private static string GenerateImplementation(ImplementationToGenerate implementationToGenerate, bool useNrtAnnotations)
        {
            var @namespace = implementationToGenerate.Namespace;

            var sb = new StringBuilder();
            if (@namespace is not null)
            {
                sb.Append($$"""
    
                            namespace {{@namespace}}
                            {
    
                            """);
            }

            sb.Append($$"""
                        {{SyntaxFacts.GetText(implementationToGenerate.DeclaredAccessibility)}} partial class {{implementationToGenerate.TypeName}} : System.ComponentModel.INotifyPropertyChanged
                        {
                            public event System.ComponentModel.PropertyChangedEventHandler{{(useNrtAnnotations ? "?" : "")}} PropertyChanged;

                            protected void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    
                        """);

            foreach (var interfaceToGenerate in implementationToGenerate.Interfaces)
            {
                foreach (var property in interfaceToGenerate.PropertiesToGenerate)
                {
                    string assignment = string.Empty;
                    if (useNrtAnnotations && property.IsReferenceType && property.NullableAnnotation != NullableAnnotation.Annotated)
                    {
                        assignment = " = null!";
                    }
                    sb.Append($$"""
    
                                [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                                private {{property.TypeName}} {{property.Name}}BackingField{{assignment}};
                                public {{property.TypeName}} {{property.Name}}
                                {
                                    get
                                    {
                                        return this.{{property.Name}}BackingField;
                                    }
                                    set
                                    {
                                        if (value != this.{{property.Name}}BackingField)
                                        {
                                            this.{{property.Name}}BackingField = value;
                                            this.OnPropertyChanged(nameof({{property.Name}}));
                                        }
                                    }
                                }
    
                            """);
                }
            }

            sb.Append("""
                        
                        }
                        
                        """);

            if (@namespace is not null)
            {
                sb.Append("""
                            
                            }
                            
                            """);
            }

            return sb.ToString();
        }

        public class ImplementationToGenerate : IEquatable<ImplementationToGenerate>
        {
            public ImplementationToGenerate(INamedTypeSymbol type, IList<INamedTypeSymbol> interfaces)
            {
                TypeName = type.Name;
                Namespace = type.ContainingNamespace?.IsGlobalNamespace == true ? null : type.ContainingNamespace?.Name;
                DeclaredAccessibility = type.DeclaredAccessibility;
                this.Interfaces = interfaces.Select(static i => new InterfaceToGenerate(i)).ToList();
            }

            public string TypeName { get; }
            
            public string? Namespace { get; }

            public Accessibility DeclaredAccessibility { get; }

            public IList<InterfaceToGenerate> Interfaces { get; }

            public bool Equals(ImplementationToGenerate? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return TypeName == other.TypeName && Namespace == other.Namespace && DeclaredAccessibility == other.DeclaredAccessibility && Interfaces.SequenceEqual(other.Interfaces);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ImplementationToGenerate) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = TypeName.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Namespace != null ? Namespace.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int) DeclaredAccessibility;
                    hashCode = (hashCode * 397) ^ Interfaces.GetHashCode();
                    return hashCode;
                }
            }
        }

        public class InterfaceToGenerate : IEquatable<InterfaceToGenerate>
        {
            public InterfaceToGenerate(INamedTypeSymbol interfaceType)
            {
                this.Name = interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                this.PropertiesToGenerate = interfaceType
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .Select(static p => new PropertyToGenerate(p))
                    .ToList();
            }
            
            public string Name { get; }

            public IList<PropertyToGenerate> PropertiesToGenerate { get; }

            public bool Equals(InterfaceToGenerate? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Name == other.Name && PropertiesToGenerate.SequenceEqual(other.PropertiesToGenerate);
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((InterfaceToGenerate) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Name.GetHashCode() * 397) ^ PropertiesToGenerate.GetHashCode();
                }
            }
        }

        public class PropertyToGenerate : IEquatable<PropertyToGenerate>
        {
            public PropertyToGenerate(IPropertySymbol propertySymbol)
            {
                this.Name = propertySymbol.Name;
                this.TypeName = propertySymbol.Type.ToDisplayString();
                this.IsReferenceType = propertySymbol.Type.IsReferenceType;
                this.NullableAnnotation = propertySymbol.Type.NullableAnnotation;
            }

            public string Name { get; }

            public string TypeName { get; }

            public bool IsReferenceType { get; }

            public NullableAnnotation NullableAnnotation { get; }

            public bool Equals(PropertyToGenerate? other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Name == other.Name && TypeName == other.TypeName && IsReferenceType == other.IsReferenceType && NullableAnnotation == other.NullableAnnotation;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PropertyToGenerate) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Name.GetHashCode();
                    hashCode = (hashCode * 397) ^ TypeName.GetHashCode();
                    hashCode = (hashCode * 397) ^ IsReferenceType.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int) NullableAnnotation;
                    return hashCode;
                }
            }
        }
    }
}