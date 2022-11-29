using INotifyPropertyChangedSourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace INotifyPropertyChangedSourceGeneratorTests
{
    [UsesVerify]
    public class GeneratorTests
    {
        [Fact]
        public Task ItShallGeneratePersonViewModelImplementation()
        {
            // Given person view model example
            CSharpCompilation compilation = GivenPersonViewModelExample();

            // and a the generator
            var generator = new PropertyChangedGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // When the generator processes the compilation containing the example
            driver = driver.RunGenerators(compilation);

            // Then the generated sources match expectation
            return Verifier.Verify(driver);
        }

        [Fact]
        public void ItShallGenerateCompilableImplementation()
        {
            // Given person view model example
            CSharpCompilation compilation = GivenPersonViewModelExample();

            // and a the generator
            var generator = new PropertyChangedGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // When the generator processes the compilation containing the example
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

            // And the resulting compilation is emitted
            var dllName = Guid.NewGuid().ToString() + ".dll";
            try
            {
                var emitResult = updatedCompilation.Emit(dllName);
                // Then no compilation error or diagnostic ocurred
                Assert.True(emitResult.Success);
                Assert.Empty(emitResult.Diagnostics);
            }
            finally
            {
                File.Delete(dllName);
            }

        }

        private static CSharpCompilation GivenPersonViewModelExample()
        {
            return CSharpCompilation.Create(nameof(ItShallGeneratePersonViewModelImplementation),
                new[]
                {
                    CSharpSyntaxTree.ParseText(@"
using AutoNotify;
namespace Foo {
public interface IPersonViewModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

[AutoNotify(typeof(IPersonViewModel))]
public partial class PersonViewModel : IPersonViewModel
{
}
}",
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11))
                },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location)!, "System.Runtime.dll"))
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
            );
        }
    }
}