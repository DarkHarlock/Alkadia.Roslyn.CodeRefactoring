namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Utilities;
    public class MoveDocumentCodeAction : CodeAction
    {
        private readonly string _folder;
        private readonly string _name;
        private readonly MoveDocumentCodeActionContext _fixContext;
        public MoveDocumentCodeAction(MoveDocumentCodeActionContext fixContext)
        {
            _fixContext = fixContext;
            _folder = fixContext.Folders.Join(@"\");
            if (!string.IsNullOrWhiteSpace(_folder))
                _folder = "\\" + _folder + "\\";
            _name = fixContext.Name;
        }
        public override string Title
        {
            get
            {
                return $@"Move File to '{_folder}{_name}.cs'";
            }
        }
        public MoveDocumentCodeActionContext FixParameters
        {
            get { return _fixContext; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return MoveDocumentToFolderAsync(_fixContext, cancellationToken);
        }
        private static async Task<Solution> MoveDocumentToFolderAsync(MoveDocumentCodeActionContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;
            var document = solution.GetDocument(context.DocumentId);
            var projectId = document.Project.Id;
            solution = solution.RemoveDocument(context.DocumentId);
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{context.Name}.cs", await document.GetTextAsync(cancellationToken), context.Folders);
            return solution;
        }
    }

}