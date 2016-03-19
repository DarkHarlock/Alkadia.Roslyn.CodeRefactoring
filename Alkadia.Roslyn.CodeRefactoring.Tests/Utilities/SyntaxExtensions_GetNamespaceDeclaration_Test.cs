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
//    public class SyntaxExtensions_GetNamespaceDeclarations_Test
//    {
//        public async Task VerifyGetNamespaceDeclarations(string code, params string[] expected)
//        {
//            var treeResult = await CSharpSyntaxTree.ParseText(code).GetRootAsync();
//            var result = treeResult.GetNamespaceDeclarations().ToArray();

//            Assert.Equal(expected.Length, result.Length);
//            Assert.True(result
//                .Zip(expected, (a, b) => a.Name.ToString() == b)
//                .All(x => x)
//            );
//        }

//        [Fact]
//        public async Task Test1()
//        {
//            var code = @"namespace Test {}";
//            await VerifyGetNamespaceDeclarations(
//                code,
//                "Test"
//            );
//        }

//        [Fact]
//        public async Task Test2()
//        {
//            var code = @"namespace Test {} namespace Other {}";
//            await VerifyGetNamespaceDeclarations(
//                code,
//                "Test",
//                "Other"
//            );
//        }

//        [Fact]
//        public async Task Test3()
//        {
//            var code = @" namespace TestSuite {
//    namespace Test {} 
//    namespace Other {}
//}";
//            await VerifyGetNamespaceDeclarations(
//                code,
//                "TestSuite",
//                "Test",
//                "Other"
//            );
//        }

//        [Fact]
//        public async Task Test4()
//        {
//            var code =
//@" namespace TestSuite {
//    namespace Test {} 
//}
//namespace TestSuite.Inner {
//    namespace Other {}
//}";
//            await VerifyGetNamespaceDeclarations(
//                code,
//                "TestSuite",
//                "Test",
//                "TestSuite.Inner",
//                "Other"
//            );
//        }

//        [Fact]
//        public async Task Test5()
//        {
//            var code =
//@" namespace TestSuite {
//    namespace Test {
//        namespace Inner { }
//    } 
//    namespace Other {}
//}
//namespace TestSuite.Inner {
//    namespace Other {}
//}";

//            await VerifyGetNamespaceDeclarations(
//                code,
//                "TestSuite",
//                "Test",
//                "Inner",
//                "Other",
//                "TestSuite.Inner",
//                "Other"
//            );
//        }
//    }
    public class SyntaxExtensions_GetNamespaceDeclaration_Test
    {
        public async Task VerifyGetNamespaceDeclaration(string code, string @namespace, string expected)
        {
            var treeResult = await CSharpSyntaxTree.ParseText(code).GetRootAsync();
            var result = treeResult.GetNamespaceDeclaration(@namespace);

            Assert.Equal(expected, result?.Name.ToString());
        }

        [Fact]
        public async Task Test1()
        {
            var code = @"namespace Test {}";
            await VerifyGetNamespaceDeclaration(
                code,
                "Test",
                "Test"
            );
        }

        [Fact]
        public async Task Test2()
        {
            var code = @"namespace Test {} namespace Other {}";
            await VerifyGetNamespaceDeclaration(
                code,
                "Other",
                "Other"
            );
        }

        [Fact]
        public async Task Test3()
        {
            var code = @" namespace TestSuite {
    namespace Test {} 
    namespace Other {}
}";
            await VerifyGetNamespaceDeclaration(
                code,
                "TestSuite.Other",
                "Other"
            );
        }

        [Fact]
        public async Task Test4()
        {
            var code =
@" namespace TestSuite {
    namespace Test {} 
}
namespace TestSuite.Inner {
    namespace Other {}
}";
            await VerifyGetNamespaceDeclaration(
                code,
                "TestSuite.Inner.Other",
                "Other"
            );
        }

        [Fact]
        public async Task Test5()
        {
            var code =
@" namespace TestSuite {
    namespace Test {} 
}
namespace TestSuite {
    namespace Inner.Other {}
}";
            await VerifyGetNamespaceDeclaration(
                code,
                "TestSuite.Inner.Other",
                "Inner.Other"
            );
        }

        [Fact]
        public async Task Test6()
        {
            var code =
@" namespace TestSuite {
    namespace Test {} 
}
namespace TestSuite {
    namespace Inner.Other {}
}";
            await VerifyGetNamespaceDeclaration(
                code,
                "Test",
                null
            );
        }
    }
}