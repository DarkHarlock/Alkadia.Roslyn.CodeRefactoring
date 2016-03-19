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
    public class SyntaxUtilities_AddUsing_Test
    {
        public CompilationUnitSyntax GetNode(string code)
        {
            var treeResult = CSharpSyntaxTree.ParseText(code);
            return treeResult.GetCompilationUnitRoot();
        }

        [Fact]
        public void TestCase1()
        {
            const string CaseTest = @"namespace Test {
    public class Foo {}
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Changed", "Test");

            const string TestExpected = @"using Changed;
namespace Test {
    public class Foo {}
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());

        }

        [Fact]
        public void TestCase2()
        {
            const string CaseTest = @"namespace Test {
    using System;
    public class Foo {}
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Changed", "Test");

            const string TestExpected = @"namespace Test {
    using System;
using Changed;
    public class Foo {}
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());

        }

        [Fact]
        public void TestCase3()
        {
            const string CaseTest = @"Using System;
namespace Test {
    using System.IO;
    namespace Inner {
        using Sistem.Threading;
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Changed", "Test.Inner");

            const string TestExpected = @"Using System;
namespace Test {
    using System.IO;
    namespace Inner {
        using Sistem.Threading;
using Changed;
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());

        }

        [Fact]
        public void TestCase4()
        {
            var workspace = new AdhocWorkspace();
            const string CaseTest = @"namespace Test {
    namespace Inner {
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Changed", "Test.Inner");

            const string TestExpected = @"using Changed;
namespace Test {
    namespace Inner {
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());

        }

        [Fact]
        public void TestCase5()
        {
            var workspace = new AdhocWorkspace();
            const string CaseTest = @"namespace Test {
    namespace Inner {
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            caseNode = caseNode.AddUsing("Test.Changed", "Test.Inner");
            var result = Formatter.Format(caseNode, workspace);

            const string TestExpected = @"using Test.Changed;
namespace Test {
    namespace Inner {
        public class Foo {}
    }
}";
            Assert.Equal(Formatter.Format(GetNode(TestExpected), workspace).ToString(), result.ToString());

        }

        [Fact]
        public void TestCase6()
        {
            const string CaseTest = @"namespace Test {
    using System;
    namespace Inner {
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Test.Changed", "Test.Inner");

            const string TestExpected = @"namespace Test {
    using System;
    namespace Inner {
using Changed;
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }

        [Fact]
        public void TestCase6_format()
        {
            const string CaseTest = @"namespace Test {
    using System;
    namespace Inner {
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Test.Changed", "Test.Inner");

            const string TestExpected = @"namespace Test {
    using System;
    namespace Inner {
        using Changed;
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), Formatter.Format(result, Formatter.Annotation, new AdhocWorkspace()).ToString());
        }

        [Fact]
        public void TestCase7()
        {
            const string CaseTest = @"namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("System", "Test.Inner");

            const string TestExpected = @"namespace Test {
    namespace Inner {
        using System;
using System;
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }

        [Fact]
        public void TestCase7_format()
        {
            const string CaseTest = @"namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("System", "Test.Inner");

            const string TestExpected = @"namespace Test {
    namespace Inner {
        using System;
        using System;
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), Formatter.Format(result, Formatter.Annotation, new AdhocWorkspace()).ToString());
        }

        [Fact]
        public async Task TestCase7_simplified()
        {
            const string CaseTest = @"namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("System", "Test.Inner");

            const string TestExpected = @"namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";

            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject(ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Default,
                "Test",
                "Test",
                LanguageNames.CSharp
            ));
            var document = project.AddDocument("Test.cs", result, new string[] { });
            document = await Simplifier.ReduceAsync(document, Simplifier.Annotation, workspace.Options);
            result = (await document.GetSyntaxTreeAsync()).GetCompilationUnitRoot();

            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }

        [Fact]
        public void TestCase8()
        {
            const string CaseTest = @"namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("System", null);

            const string TestExpected = @"using System;
namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";
            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }

        [Fact]
        public void TestCase9()
        {
            const string CaseTest = @"namespace Test {
    namespace Inner {
        using System;
        public class Foo {}
    }
}";
            var caseNode = GetNode(CaseTest);
            var result = caseNode.AddUsing("Test", "Test");

            const string TestExpected = CaseTest;

            Assert.Equal(GetNode(TestExpected).ToString(), result.ToString());
        }
    }
}