namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CSharp;
    using Utilities;
    public class MoveClassCodeAction : CodeAction
    {
        private readonly string _folders;
        private readonly MoveClassCodeActionContext _fixContext;
        public MoveClassCodeAction(MoveClassCodeActionContext fixContrext)
        {
            _fixContext = fixContrext;
            var bs = "\\";
            var folder = (fixContrext.Folders ?? new string[0]).Join(bs);
            var sep = string.IsNullOrEmpty(folder) ? string.Empty : bs;
            _folders = $@"{sep}{folder}{sep}";
        }
        public override string Title
        {
            get
            {
                return $"Move class into '{_folders}{_fixContext.Name}.cs'";
            }
        }
        protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            var document = _fixContext.Solution.GetDocument(_fixContext.DocumentId);
            var root = (await document.GetSyntaxTreeAsync(cancellationToken)).GetCompilationUnitRoot(cancellationToken);

            var newContent = root.ExtractClass(_fixContext.Span);
            var newDocument = document.Project.AddDocument($"{_fixContext.Name}.cs", newContent, _fixContext.Folders);

            document = newDocument.Project.GetDocument(document.Id);
            root = root.RemoveNode(root.FindNode(_fixContext.Span), SyntaxRemoveOptions.KeepNoTrivia);

            document = document.WithSyntaxRoot(root);
            return document.Project.Solution;
        }
    }
}