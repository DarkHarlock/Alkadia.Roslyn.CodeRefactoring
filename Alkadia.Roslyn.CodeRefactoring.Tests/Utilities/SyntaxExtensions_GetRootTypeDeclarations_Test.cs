namespace Alkadia.Roslyn.CodeRefactoring.Tests.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeRefactoring.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using Microsoft.CodeAnalysis.Text;
    using Xunit;
    public class SyntaxExtensions_GetRootTypeDeclarations_Test
    {
        public async Task VerifyGetRootTypeDeclarations(string code, params string[] expected)
        {
            var treeResult = await CSharpSyntaxTree.ParseText(code).GetRootAsync();
            var result = treeResult.GetRootTypeDeclarations().ToArray();

            Assert.Equal(expected.Length, result.Length);
            Assert.True(result
                .Zip(expected, (a, b) => a.Identifier.ValueText == b)
                .All(x => x)
            );
        }

        [Fact]
        public async Task Test1()
        {
            var code = @"class Test {}";
            await VerifyGetRootTypeDeclarations(
                code,
                "Test"
            );
        }

        [Fact]
        public async Task Test2()
        {
            var code = @"class Test {} class Other {}";
            await VerifyGetRootTypeDeclarations(
                code,
                "Test",
                "Other"
            );
        }

        [Fact]
        public async Task Test3()
        {
            var code = @" namespace TestSuite {
    class Test {} 
    class Other {}
}";
            await VerifyGetRootTypeDeclarations(
                code,
                "Test",
                "Other"
            );
        }

        [Fact]
        public async Task Test4()
        {
            var code =
@" namespace TestSuite {
    class Test {} 
}
namespace TestSuite.Inner {
    class Other {}
}";
            await VerifyGetRootTypeDeclarations(
                code,
                "Test",
                "Other"
            );
        }

        [Fact]
        public async Task Test5()
        {
            var code =
@" namespace TestSuite {
    class Test {
        class InnerClass { }
    } 
}
namespace TestSuite.Inner {
    class Other {}
}";

            await VerifyGetRootTypeDeclarations(
                code,
                "Test",
                "Other"
            );
        }

        [Fact]
        public async Task Test6()
        {
            var code =
@" namespace TestSuite {
    class Test {
        class InnerClass { }
    } 
}
namespace TestSuite.Inner {
    class Other {
        class InnerClass { }
    }
}";

            await VerifyGetRootTypeDeclarations(
                code,
                "Test",
                "Other"
            );
        }
    }
}