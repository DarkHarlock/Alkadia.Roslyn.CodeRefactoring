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
//    public class SyntaxExtensions_GetUsingDirectives_Test
//    {
//        public async Task VerifyGetUsing(string code, string[] expected, params string[] additional)
//        {
//            var treeResult = await CSharpSyntaxTree.ParseText(code).GetRootAsync();
//            var result = treeResult.GetUsingDirectives(additional).ToArray();

//            Assert.Equal(expected.Length, result.Length);
//            Assert.True(result
//                .Zip(expected, (a, b) => a.Name.ToString() == b)
//                .All(x => x)
//            );
//        }

//        [Fact]
//        public async Task Test1()
//        {
//            var code = @"using System;";
//            await VerifyGetUsing(
//                code,
//                new[] { "System" }
//            );
//        }

//        [Fact]
//        public async Task Test2()
//        {
//            var code = @"using System; using System.IO;";
//            await VerifyGetUsing(
//                code,
//                new[] { "System", "System.IO" }
//            );
//        }

//        [Fact]
//        public async Task Test3()
//        {
//            var code = @"using System; 
//using System.IO;

//namespace Test {
//    using System.Threading;
//}";
//            await VerifyGetUsing(
//                code,
//                new[] { "System", "System.IO" }
//            );
//        }

//        [Fact]
//        public async Task Test4()
//        {
//            var code = @"using System; 
//using System.IO;

//namespace Test {
//    using System.Threading;
//}";
//            await VerifyGetUsing(
//                code,
//                new[] { "System", "System.IO", "System.Collections" },
//                "System.Collections"
//            );
//        }
//    }
    public class SyntaxExtensions_IsNamespaceDeclaration_Test
    {
        [Fact]
        public async Task Should_not_match_as_namespace_declaration()
        {
            const string TestCase = @"
using System;
namespace Test {
    public class Foo {}
}
";

            var tree = CSharpSyntaxTree.ParseText(TestCase);
            var root = await tree.GetRootAsync();

            var cases = new[]
            {
                "namespace",
                "using",
                "System",
                "public",
                "class",
                "Foo"
            }
            .Select(testcase => new TextSpan(TestCase.IndexOf(testcase, StringComparison.Ordinal), testcase.Length))
            .Select(span => root.IsNamespaceDeclaration(span))
            .Where(x => x != null);

            Assert.Equal(0, cases.Count());
        }
        [Fact]
        public async Task Should_match_as_namespace_declaration()
        {
            const string TestCase = @"
using System;
namespace Test {
    public class Foo {}
}
";

            var tree = CSharpSyntaxTree.ParseText(TestCase);
            var root = await tree.GetRootAsync();

            var cases = new[]
            {
                "Test"
            }
            .Select(testcase => new TextSpan(TestCase.IndexOf(testcase, StringComparison.Ordinal), testcase.Length))
            .Select(span => root.IsNamespaceDeclaration(span))
            .Where(x => x != null)
            .ToArray();

            Assert.Equal(1, cases.Length);
            Assert.Equal("Test", cases[0].Name.ToString());
        }
        [Fact]
        public async Task Should_match_as_namespace_declaration2()
        {
            const string TestCase = @"
using System;
namespace TestSuite.Inner {
    public class Foo {}
}
";

            var tree = CSharpSyntaxTree.ParseText(TestCase);
            var root = await tree.GetRootAsync();

            var cases = new[]
            {
                "Test",
                "Suite",
                "Inner"
            }
            .Select(testcase => new TextSpan(TestCase.IndexOf(testcase, StringComparison.Ordinal), testcase.Length))
            .Select(span => root.IsNamespaceDeclaration(span))
            .Where(x => x != null)
            .ToArray();

            Assert.Equal(3, cases.Length);
            Assert.Equal("TestSuite.Inner", cases[0].Name.ToString());
            Assert.Equal("TestSuite.Inner", cases[1].Name.ToString());
            Assert.Equal("TestSuite.Inner", cases[2].Name.ToString());
        }
        [Fact]
        public async Task Should_match_as_namespace_declaration3()
        {
            const string TestCase = @"
using System;
namespace TestSuite.Inner.Other {
    public class Foo {}
}
";

            var tree = CSharpSyntaxTree.ParseText(TestCase);
            var root = await tree.GetRootAsync();

            var cases = new[]
            {
                "Test",
                "Suite",
                "Inner",
                "Other"
            }
            .Select(testcase => new TextSpan(TestCase.IndexOf(testcase, StringComparison.Ordinal), testcase.Length))
            .Select(span => root.IsNamespaceDeclaration(span))
            .Where(x => x != null)
            .ToArray();

            Assert.Equal(4, cases.Length);
            Assert.Equal("TestSuite.Inner.Other", cases[0].Name.ToString());
            Assert.Equal("TestSuite.Inner.Other", cases[1].Name.ToString());
            Assert.Equal("TestSuite.Inner.Other", cases[2].Name.ToString());
            Assert.Equal("TestSuite.Inner.Other", cases[3].Name.ToString());
        }
    }
}