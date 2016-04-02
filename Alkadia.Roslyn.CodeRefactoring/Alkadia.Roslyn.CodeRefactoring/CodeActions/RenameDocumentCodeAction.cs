namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;

    public class RenameDocumentCodeAction : CodeAction
    {
        private readonly string _name;
        private readonly RenameDocumentCodeActionContext _fixContext;
        public RenameDocumentCodeAction(RenameDocumentCodeActionContext fixContext)
        {
            _fixContext = fixContext;
            _name = fixContext.Name;
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
            get { return _fixContext; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return RenameDocumentToFolderAsync(_fixContext, cancellationToken);
        }
        private static async Task<Solution> RenameDocumentToFolderAsync(RenameDocumentCodeActionContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;
            var document = solution.GetDocument(context.DocumentId);
            var projectId = document.Project.Id;
            solution = solution.RemoveDocument(context.DocumentId);
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{context.Name}.cs", await document.GetTextAsync(cancellationToken), document.Folders);
            return solution;
        }
    }

}