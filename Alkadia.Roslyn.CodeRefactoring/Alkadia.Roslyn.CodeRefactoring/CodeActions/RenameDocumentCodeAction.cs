namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;

    public class RenameDocumentCodeAction : CodeAction
    {
        private readonly string _name;
        private readonly RenameDocumentCodeActionContext _fix;
        public RenameDocumentCodeAction(RenameDocumentCodeActionContext fix)
        {
            _fix = fix;
            _name = fix.Name;
        }
        public override string Title
        {
            get
            {
                return $@"Rename File to '{_name}.cs'";
            }
        }
        public RenameDocumentCodeActionContext FixParameters
        {
            get { return _fix; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return RenameDocumentToFolderAsync(_fix, cancellationToken);
        }
        private static async Task<Solution> RenameDocumentToFolderAsync(RenameDocumentCodeActionContext fix, CancellationToken cancellationToken)
        {
            var solution = fix.Solution;
            var document = solution.GetDocument(fix.DocumentId);
            var projectId = document.Project.Id;
            solution = solution.RemoveDocument(fix.DocumentId);
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{fix.Name}.cs", await document.GetTextAsync(cancellationToken), document.Folders);
            return solution;
        }
    }

}