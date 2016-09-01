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
    public class SyntaxUtilities_ExtractClass_Test
    {
        public CompilationUnitSyntax GetNode(string code)
        {
            var treeResult = CSharpSyntaxTree.ParseText(code);
            return treeResult.GetCompilationUnitRoot();
        }

        [Fact]
        public void TestCase1()
        {
            const string TestCase = @"namespace Test {
    public class Foo {}
}";
            var caseNode = GetNode(TestCase);
            var result = caseNode.ExtractClass(new TextSpan(TestCase.IndexOf("Foo", StringComparison.Ordinal), 1));

            const string TestExpected = TestCase;

            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());

        }

        [Fact]
        public void TestCase2()
        {
            const string TestCase = @"using System;
namespace Test {
    public class Foo {}
}";
            var caseNode = GetNode(TestCase);
            var result = caseNode.ExtractClass(new TextSpan(TestCase.IndexOf("Test", StringComparison.Ordinal), 1));

            Assert.Null(result);
        }

        [Fact]
        public void TestCase3()
        {
            const string TestCase = @"using System;
namespace Test {
    namespace Inner {
        public class Foo {}
    }
}";
            var caseNode = GetNode(TestCase);
            var result = caseNode.ExtractClass(new TextSpan(TestCase.IndexOf("Foo", StringComparison.Ordinal), 1));

            const string TestExpected = TestCase;

            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }

        [Fact]
        public void TestCase4()
        {
            const string TestCase = @"using System;
namespace Test {
    namespace Inner {
        public class Foo {
            public string Hello() {return ""Hello"";}
        }
        public class Other {}
    }
}";
            var caseNode = GetNode(TestCase);
            var result = caseNode.ExtractClass(new TextSpan(TestCase.IndexOf("Foo", StringComparison.Ordinal), 1));

            const string TestExpected = @"using System;
namespace Test {
    namespace Inner {
        public class Foo {
            public string Hello() {return ""Hello"";}
        }
    }
}"; 

            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }
    }
}