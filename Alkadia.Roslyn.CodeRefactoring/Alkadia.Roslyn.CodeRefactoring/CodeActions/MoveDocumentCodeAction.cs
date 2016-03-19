namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Alkadia.Roslyn.CodeRefactoring.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    public class MoveDocumentCodeAction : CodeAction
    {
        private readonly string _folder;
        private readonly string _name;
        private readonly MoveDocumentCodeActionContext _fix;
        public MoveDocumentCodeAction(MoveDocumentCodeActionContext fix)
        {
            _fix = fix;
            _folder = fix.Folders.Join(@"\");
            if (!string.IsNullOrWhiteSpace(_folder))
                _folder = "\\" + _folder + "\\";
            _name = fix.Name;
        }
        public override string Title
        {
            get
            {
                return _fix.IsRename
                    ? $@"Rename File to '{_name}.cs'"
                    : $@"Move File to '{_folder}{_name}.cs'";
            }
        }
        public MoveDocumentCodeActionContext FixParameters
        {
            get { return _fix; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return MoveDocumentToFolderAsync(_fix, cancellationToken);
        }
        private static async Task<Solution> MoveDocumentToFolderAsync(MoveDocumentCodeActionContext fix, CancellationToken cancellationToken)
        {
            var solution = fix.Solution;
            var document = solution.GetDocument(fix.DocumentId);
            var projectId = document.Project.Id;
            solution = solution.RemoveDocument(fix.DocumentId);
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{fix.Name}.cs", await document.GetTextAsync(cancellationToken), fix.Folders);
            return solution;
        }
    }
}