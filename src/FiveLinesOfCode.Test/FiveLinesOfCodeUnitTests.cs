using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = FiveLinesOfCode.Test.CSharpCodeFixVerifier<
    FiveLinesOfCode.CallOrPass,
    FiveLinesOfCode.CallOrPassCodeFixProvider>;

namespace FiveLinesOfCode.Test
{
    [TestClass]
    public class CallOrPassTest
    {
        [TestMethod]
        public async Task ShouldAlertToAccessWithCall()
        {
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        AClass instance = new AClass();

        // Property access.
        int value = instance.Property;

        // Pass to method.
        Method(instance);
    }

    static void Method(AClass instance)
    {
    }
}

class AClass
{
    public int Property = 1;
}
";

            DiagnosticResult expected =
                VerifyCS
                    .Diagnostic("CallOrPass")
                    .WithSpan(9, 21, 9, 29)
                    .WithArguments("instance");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        //No diagnostics expected to show up
        [TestMethod]
        public async Task ShouldNotALertToAccess()
        {
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        AClass instance = new AClass();

        // Property access.
        int value = instance.Property;
    }
}

class AClass
{
    public int Property = 1;
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //No diagnostics expected to show up
        [TestMethod]
        public async Task ShouldNotAlertToPass()
        {
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        AClass instance = new AClass();
        Method(instance);
    }

    static void Method(AClass instance)
    {
    }
}

class AClass
{
    public int Property = 1;
}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
