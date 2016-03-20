namespace Alkadia.Roslyn.CodeRefactoring.Tests.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Xunit;
    using CodeRefactoring.CodeActions;

    public class RenameDocumentCodeActionTests
    {
        private class TestableRenameDocumentCodeAction : RenameDocumentCodeAction
        {
            public TestableRenameDocumentCodeAction(RenameDocumentCodeActionContext fix)
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
        public async Task Should_rename_file()
        {
            var documentToMove = @"namespace Foo {
    public class Test {
        public void Do() {}
    }
}
";
            var project = CreateProject(CreateSolution(), "TestSuite");
            var doc = project.AddDocument("test.cs", documentToMove);

            var action = new TestableRenameDocumentCodeAction(new RenameDocumentCodeActionContext
            {
                DocumentId = doc.Id,
                Solution = doc.Project.Solution,
                Name = "Success"
            });

            Assert.Equal(@"Rename File to 'Success.cs'", action.Title);

            var result = await action.Execute(CancellationToken.None);

            var document = result.GetDocument(doc.Id);
            Assert.Null(document);
            document = result.GetProject(project.Id).Documents.Where(d => d.Name == "Success.cs").FirstOrDefault();
            Assert.NotNull(document);
            Assert.Equal(documentToMove, (await document.GetTextAsync()).ToString());
        }
    }

}