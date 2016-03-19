namespace Alkadia.Roslyn.CodeRefactoring.Tests.CodeActions
{
    using System.Threading;
    using System.Threading.Tasks;
    using CodeRefactoring.CodeActions;
    using Microsoft.CodeAnalysis;
    using Xunit;

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
            var refactorDocumentCode = @"namespace Foo {
    public class Test {
        public void Do() {}
    }
}
";
            var otherDocumentCode = @"namespace Foo {
    public class Tester {
        void Do() { new Test().Do(); }
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
                NamespaceToFix = "Foo",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"namespace TestSuite.Inner {
    public class Test {
        public void Do() {}
    }
}
";
            var otherDocumentCodeFixed = @"using TestSuite.Inner;
namespace Foo {
    public class Tester {
        void Do() { new Test().Do(); }
    }
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
                NamespaceToFix = "Foo",
                NewNamespace = "TestSuite.Inner"
            }).Execute(CancellationToken.None);

            var refactorDocumentCodeFixed = @"namespace TestSuite.Inner {
    public class Test {
        public void Do() {}
    }
}
";
            var otherDocumentCodeFixed = @"using TestSuite.Inner;
namespace Foo {
    public class Tester {
        void Do(Test test) { test.Do(); }
    }
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
    void Do(Test test) { test.Do(); }
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
    void Do(Test test) { test.Do(); }
}
";
            var refactorText = (await fixedSolution.GetDocument(refactorDocumentId).GetTextAsync()).ToString();
            var otherText = (await fixedSolution.GetDocument(otherDocumentId).GetTextAsync()).ToString();

            Assert.Equal(refactorDocumentCodeFixed, refactorText);
            Assert.Equal(otherDocumentCodeFixed, otherText);
        }
    }
}