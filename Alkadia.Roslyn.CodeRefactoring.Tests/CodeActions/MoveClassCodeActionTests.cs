namespace Alkadia.Roslyn.CodeRefactoring.Tests.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;
    using Xunit;
    using CodeRefactoring.CodeActions;
    using Microsoft.CodeAnalysis.Formatting;
    public class MoveClassCodeActionTests
    {
        private class TestableMoveClassCodeAction: MoveClassCodeAction
        {
            public TestableMoveClassCodeAction(MoveClassCodeActionContext fix)
                :base(fix)
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
        public async Task Should_move_Foo_to_folder_inner_into_class_dot_cs()
        {
            const string CaseTest = @"using System;
namespace Test {
    namespace Inner {
        public class Foo {
            public string Hello() {return ""Hello"";}
        }
        public class Other {
            public string Hello() {return ""Hello"";}
        }
    }
}";
            const string ExpectedNewFileSource = @"using System;
namespace Test {
    namespace Inner {
        public class Foo {
            public string Hello() {return ""Hello"";}
        }
    }
}";
            const string ExpectedSource = @"using System;
namespace Test {
    namespace Inner {
        public class Other {
            public string Hello() {return ""Hello"";}
        }
    }
}";

            var project = CreateProject(CreateSolution(), "TestSuite");
            var document = project.AddDocument("Test.cs", CaseTest, new[] { "Folder" });
            var action = new TestableMoveClassCodeAction(new MoveClassCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Folders = new[] { "Inner" },
                Name = "Class",
                Span = new TextSpan(CaseTest.IndexOf("Foo", System.StringComparison.Ordinal), 3)
            });

            Assert.Equal("Move class into '\\Inner\\Class.cs'", action.Title);

            var newSolution = await action.Execute(CancellationToken.None);
            var newProject = newSolution.GetProject(project.Id);

            var exp = newProject.GetDocument(document.Id);
            var newDocument = newProject.Documents.FirstOrDefault(d => d.Name == "Class.cs");

            Assert.NotNull(exp);

            Assert.Equal(ExpectedSource, (await exp.GetTextAsync()).ToString());
            Assert.Equal(ExpectedNewFileSource, (await newDocument.GetTextAsync()).ToString());
        }

        [Fact]
        public async Task Should_accept_null_folders_to_target_project_root()
        {
            const string CaseTest = @"using System;
public class Foo {
    public string Hello() {return ""Hello"";}
}
public class Other {
    public string Hello() {return ""Hello"";}
}
";
            const string ExpectedNewFileSource = @"using System;
public class Foo {
    public string Hello() {return ""Hello"";}
}
";
            const string ExpectedSource = @"using System;
public class Other {
    public string Hello() {return ""Hello"";}
}
";
            var project = CreateProject(CreateSolution(), "TestSuite");
            var document = project.AddDocument("Test.cs", CaseTest, new[] { "Inner" });
            var action = new TestableMoveClassCodeAction(new MoveClassCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Folders = null,
                Name = "Foo",
                Span = new TextSpan(CaseTest.IndexOf("Foo", System.StringComparison.Ordinal), 3)
            });

            Assert.Equal("Move class into 'Foo.cs'", action.Title);

            var newSolution = await action.Execute(CancellationToken.None);
            var newProject = newSolution.GetProject(project.Id);

            var exp = newProject.GetDocument(document.Id);
            var newDocument = newProject.Documents.FirstOrDefault(d => d.Name == "Foo.cs");

            Assert.NotNull(exp);

            Assert.Equal(ExpectedSource, (await exp.GetTextAsync()).ToString());
            Assert.Equal(ExpectedNewFileSource, (await newDocument.GetTextAsync()).ToString());
        }
    }
}