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
    public class SyntaxUtilities_ChangeNamespace_Test
    {
        public CompilationUnitSyntax GetNode(string code)
        {
            var treeResult = CSharpSyntaxTree.ParseText(code);
            return treeResult.GetCompilationUnitRoot();
        }

        public void DoAssert(string testCase, string expected, string targetNamespace, string newNamespace)
        {
            var caseNode = GetNode(testCase);
            var result = caseNode.ChangeNamespace(targetNamespace, newNamespace);
            Assert.Equal(GetNode(expected).ToString(), result.ToString());
        }

        [Fact]
        public void TestCase1()
        {
            const string TestCase = @"using System;
namespace Test {
    public class Foo {}
}";
            const string Expected = @"using System;
namespace Changed {
    public class Foo {}
}";
            DoAssert(TestCase, Expected, "Test", "Changed");           
        }

        [Fact]
        public void TestCase2()
        {
            const string TestCase = @"
using System;
namespace Test {
    public class Foo {}
    public class Other {}
}";
            const string Expected = @"
using System;
namespace Changed {
    public class Foo {}
    public class Other {}
}";
            DoAssert(TestCase, Expected, "Test", "Changed");

        }

        [Fact]
        public void TestCase3()
        {
            var workspace = new AdhocWorkspace();
            const string TestCase = @"
using System;
namespace Test {
    namespace Inner {
        public class Foo {}
    }
    public class Other {}
}";
            const string Expected = @"
using System;
namespace Test {
    namespace Changed {
        public class Foo {}
    }
    public class Other {}
}";
            DoAssert(TestCase, Expected, "Test.Inner", "Changed");
        }

        [Fact]
        public void TestCase4()
        {
            var workspace = new AdhocWorkspace();
            const string TestCase = @"
using System;
namespace Test {
    namespace Inner {
        public class Foo {}
    }
    public class Other {}
}";
            const string Expected = @"
using System;
namespace Changed {
    namespace Inner {
        public class Foo {}
    }
    public class Other {}
}";
            DoAssert(TestCase, Expected, "Test", "Changed");
        }

        [Fact]
        public void TestCase5()
        {
            var workspace = new AdhocWorkspace();
            const string TestCase = @"
using System;
namespace Test {
    namespace Inner.Other {
        public class Foo {}
    }
    public class Other {}
}";
            const string Expected = @"
using System;
namespace Test {
    namespace Changed {
        public class Foo {}
    }
    public class Other {}
}";
            DoAssert(TestCase, Expected, "Test.Inner.Other", "Changed");
        }

        [Fact]
        public void TestCase6()
        {
            const string TestCase = @"using System;
namespace Test {
    public class Foo {}
}";
            const string Expected = @"using System;
namespace Test {
    public class Foo {}
}";
            DoAssert(TestCase, Expected, "TestSuite", "Changed");
        }
    }
}