using System.Runtime.CompilerServices;
using VerifyTests;

namespace INotifyPropertyChangedSourceGeneratorTests
{
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifySourceGenerators.Enable();
        }
    }
}