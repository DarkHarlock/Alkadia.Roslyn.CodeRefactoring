﻿namespace Alkadia.Roslyn.CodeRefactoring.Tests.CodeActions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Xunit;
    using CodeRefactoring.CodeActions;

    public class ChangeNamespaceCodeActionTests
    {
        private class TestableChangeNamespaceCodeAction : ChangeNamespaceCodeAction
        {
            public TestableChangeNamespaceCodeAction(ChangeNamespaceCodeActionContext fix)
                : base(fix)
            {

            }

            public Task<Solution> Execute(CancellationToken cancellationToken)
            {
                return GetChangedSolutionAsync(cancellationToken);
            }
        }
        public Solution CreateSolution()
        {
            var workspace = new AdhocWorkspace();
            var solution = workspace.AddSolution(SolutionInfo.Create(
                SolutionId.CreateNewId(),
                VersionStamp.Default
            ));
            return solution;
        }
        public Project CreateProject(Solution solution, string assemblyName)
        {
            var projectId = ProjectId.CreateNewId();
            return solution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                assemblyName,
                assemblyName,
                LanguageNames.CSharp
            )).GetProject(projectId);
        }

        [Fact]
        public async Task Case1()
        {
            var refactorDocumentCode = @"namespace Foo.Bar {
    public class Test {
        public void Do() { new Helper(); }
    }
}
";
            var otherDocumentCode = @"namespace Foo.Bar {
    public class Tester {
        void Do() { new Test().Do(); }
    }
    public class Helper {}
}
";
            var project = CreateProject(CreateSolution(), "TestSuite.Inner");
            var tmpDoc = project.AddDocument("z.cs", otherDocumentCode);
            var otherDocumentId = tmpDoc.Id;
            tmpDoc = tmpDoc.Project.AddDocument("Test.cs", refactorDocumentCode);
            var refactorDocumentId = tmpDoc.Id;

            var solution = tmpDoc.Project.Solution;

            var fixedSolution = await new TestableChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
            {
                DocumentId = refactorDocumentId,
                Solution = solution,
                NamespaceToFix = "Foo.Bar",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"using Foo.Bar;
namespace TestSuite.Inner {
    public class Test {
        public void Do() { new Helper(); }
    }
}
";
            var otherDocumentCodeFixed = @"using TestSuite.Inner;
namespace Foo.Bar {
    public class Tester {
        void Do() { new Test().Do(); }
    }
    public class Helper {}
}
";
            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }

        [Fact]
        public async Task Case2()
        {
            var refactorDocumentCode = @"namespace Foo {
    public class Test {
        public void Do() { new Helper(); }
    }
}
";
            var otherDocumentCode = @"namespace Foo {
    public class Tester {
        void Do(Test test) { test.Do(); }
    }
    public class Helper {}
}
";
            var project = CreateProject(CreateSolution(), "TestSuite.Inner");
            var tmpDoc = project.AddDocument("z.cs", otherDocumentCode);
            var otherDocumentId = tmpDoc.Id;
            tmpDoc = tmpDoc.Project.AddDocument("Test.cs", refactorDocumentCode);
            var refactorDocumentId = tmpDoc.Id;

            var solution = tmpDoc.Project.Solution;

            var fixedSolution = await new TestableChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
            {
                DocumentId = refactorDocumentId,
                Solution = solution,
                NamespaceToFix = "Foo",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"using Foo;
namespace TestSuite.Inner {
    public class Test {
        public void Do() { new Helper(); }
    }
}
";
            var otherDocumentCodeFixed = @"using TestSuite.Inner;
namespace Foo {
    public class Tester {
        void Do(Test test) { test.Do(); }
    }
    public class Helper {}
}
";
            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }

        [Fact]
        public async Task Case3()
        {
            var refactorDocumentCode = @"namespace Foo {
    public class Test {
        public void Do() {}
    }
}
";
            var otherDocumentCode = @"namespace Foo {
    public class Tester {
        void Do(Test test) { test.Do(); }
    }
}
";
            var project = CreateProject(CreateSolution(), "TestSuite.Inner");
            var tmpDoc = project.AddDocument("z.cs", otherDocumentCode);
            var otherDocumentId = tmpDoc.Id;
            tmpDoc = tmpDoc.Project.AddDocument("Test.cs", refactorDocumentCode);
            var refactorDocumentId = tmpDoc.Id;

            var solution = tmpDoc.Project.Solution;

            var fixedSolution = await new TestableChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
            {
                DocumentId = refactorDocumentId,
                Solution = solution,
                NamespaceToFix = "Inner",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = refactorDocumentCode;
            var otherDocumentCodeFixed = otherDocumentCode;

            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }

        [Fact]
        public async Task Case4()
        {
            var refactorDocumentCode = @"public class Test {
    public void Do() {}
}
";
            var otherDocumentCode = @"public class Tester {
    public void Do(Test test) { test.Do(); }
}
";
            var project = CreateProject(CreateSolution(), "TestSuite.Inner");
            var tmpDoc = project.AddDocument("z.cs", otherDocumentCode);
            var otherDocumentId = tmpDoc.Id;
            tmpDoc = tmpDoc.Project.AddDocument("Test.cs", refactorDocumentCode);
            var refactorDocumentId = tmpDoc.Id;

            var solution = tmpDoc.Project.Solution;

            var fixedSolution = await new TestableChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
            {
                DocumentId = refactorDocumentId,
                Solution = solution,
                NamespaceToFix = null,
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"namespace TestSuite.Inner {
public class Test {
    public void Do() {}
}
}
";
            var otherDocumentCodeFixed = @"using TestSuite.Inner;
public class Tester {
    public void Do(Test test) { test.Do(); }
}
";
            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }

        [Fact]
        public async Task Case5()
        {
            var refactorDocumentCode = @"namespace Foo.Bar {
    public class Tester {
        public void Do() { }
    }
    public class Test {
        public void Do() { new Tester().Exec(); }
    }
}
";
            var otherDocumentCode = @"namespace Foo.Bar {
    public static class Helper {
        public static void Exec(this Tester t) { t.Do(); }
    }
}
";
            var project = CreateProject(CreateSolution(), "TestSuite.Inner");
            var tmpDoc = project.AddDocument("z.cs", otherDocumentCode);
            var otherDocumentId = tmpDoc.Id;
            tmpDoc = tmpDoc.Project.AddDocument("Test.cs", refactorDocumentCode);
            var refactorDocumentId = tmpDoc.Id;

            var solution = tmpDoc.Project.Solution;

            var fixedSolution = await new TestableChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
            {
                DocumentId = refactorDocumentId,
                Solution = solution,
                NamespaceToFix = "Foo.Bar",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"using Foo.Bar;
namespace TestSuite.Inner {
    public class Tester {
        public void Do() { }
    }
    public class Test {
        public void Do() { new Tester().Exec(); }
    }
}
";
            var otherDocumentCodeFixed = @"using TestSuite.Inner;
namespace Foo.Bar {
    public static class Helper {
        public static void Exec(this Tester t) { t.Do(); }
    }
}
";
            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }

        [Fact]
        public async Task Case6()
        {
            var refactorDocumentCode = @"namespace Foo.Bar {
    public static class Test {
        public static void Exec(this Tester t) { t.Do(); }
    }
}
";
            var otherDocumentCode = @"namespace Foo.Bar {
    public class Tester {
        public void Do() { }
    }
}
";
            var project = CreateProject(CreateSolution(), "TestSuite.Inner");
            var tmpDoc = project.AddDocument("z.cs", otherDocumentCode);
            var otherDocumentId = tmpDoc.Id;
            tmpDoc = tmpDoc.Project.AddDocument("Test.cs", refactorDocumentCode);
            var refactorDocumentId = tmpDoc.Id;

            var solution = tmpDoc.Project.Solution;

            var fixedSolution = await new TestableChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
            {
                DocumentId = refactorDocumentId,
                Solution = solution,
                NamespaceToFix = "Foo.Bar",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"using Foo.Bar;
namespace TestSuite.Inner {
    public static class Test {
        public static void Exec(this Tester t) { t.Do(); }
    }
}
";
            var otherDocumentCodeFixed = @"namespace Foo.Bar {
    public class Tester {
        public void Do() { }
    }
}
";
            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }

    }
}