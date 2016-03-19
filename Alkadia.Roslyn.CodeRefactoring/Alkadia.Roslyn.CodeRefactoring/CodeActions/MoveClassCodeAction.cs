namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Alkadia.Roslyn.CodeRefactoring.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CSharp;
    public class MoveClassCodeAction : CodeAction
    {
        private readonly string _folders;
        private readonly MoveClassCodeActionContext _fix;
        public MoveClassCodeAction(MoveClassCodeActionContext fix)
        {
            _fix = fix;
            var bs = "\\";
            var folder = (fix.Folders ?? new string[0]).Join(bs);
            var sep = string.IsNullOrEmpty(folder) ? string.Empty : bs;
            _folders = $@"{sep}{folder}{sep}";
        }
        public override string Title
        {
            get
            {
                return $"Move class into '{_folders}{_fix.Name}'";
            }
        }
        protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            var document = _fix.Solution.GetDocument(_fix.DocumentId);
            var root = (await document.GetSyntaxTreeAsync(cancellationToken)).GetCompilationUnitRoot(cancellationToken);

            var newContent = root.ExtractClass(_fix.Span);
            var newDocument = document.Project.AddDocument(_fix.Name, newContent, _fix.Folders);

            document = newDocument.Project.GetDocument(document.Id);
            root = root.RemoveNode(root.FindNode(_fix.Span), SyntaxRemoveOptions.KeepNoTrivia);

            document = document.WithSyntaxRoot(root);
            return document.Project.Solution;
        }
    }
}