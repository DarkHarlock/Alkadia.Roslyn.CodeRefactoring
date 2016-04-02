namespace Alkadia.Roslyn.CodeRefactoring.Tests.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Xunit;
    using CodeRefactoring.CodeActions;
    public class MoveDocumentCodeActionTests
    {
        private class TestableMoveDocumentCodeAction : MoveDocumentCodeAction
        {
            public TestableMoveDocumentCodeAction(MoveDocumentCodeActionContext fix)
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
        public async Task Should_move_file()
        {
            var documentToMove = @"namespace Foo {
    public class Test {
        public void Do() {}
    }
}
";
            var project = CreateProject(CreateSolution(), "TestSuite");
            var doc = project.AddDocument("test.cs", documentToMove);

            var folders = new[] { "Inner", "Nested" };
            var action = new TestableMoveDocumentCodeAction(new MoveDocumentCodeActionContext
            {
                DocumentId = doc.Id,
                Solution = doc.Project.Solution,
                Folders = folders,
                Name = "Success"
            });

            Assert.Equal(@"Move File to '\Inner\Nested\Success.cs'", action.Title);

            var result = await action.Execute(CancellationToken.None);

            Assert.Null(result.GetDocument(doc.Id));
            var newDoc = result.GetProject(project.Id).Documents.FirstOrDefault(d => d.Name == "Success.cs");
            Assert.NotNull(newDoc);
            Assert.Equal(2, newDoc.Folders.Zip(folders, (a, b) => a == b).Count());

            Assert.Equal(documentToMove, (await newDoc.GetTextAsync()).ToString());
        }
    }

}