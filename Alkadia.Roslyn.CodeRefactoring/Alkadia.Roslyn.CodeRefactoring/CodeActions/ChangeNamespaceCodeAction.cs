namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.FindSymbols;
    using Utilities;
    public class ChangeNamespaceCodeAction : CodeAction
    {
        private readonly string _namespace;
        private readonly ChangeNamespaceCodeActionContext _fix;
        public ChangeNamespaceCodeAction(ChangeNamespaceCodeActionContext fix)
        {
            _fix = fix;
            _namespace = fix.NewNamespace;
        }
        public override string Title
        {
            get { return $"Change Namespace to '{_namespace}'"; }
        }
        public ChangeNamespaceCodeActionContext FixParameters
        {
            get { return _fix; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return FixAsync(_fix, cancellationToken);
        }
        private static async Task<Solution> FixAsync(ChangeNamespaceCodeActionContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;
            var options = solution.Workspace.Options;
            var document = solution.GetDocument(context.DocumentId);
            var tree = await document.GetSyntaxTreeAsync();
            var root = tree.GetCompilationUnitRoot(cancellationToken);

            var toChange = root as SyntaxNode;
            if (context.NamespaceToFix != null)
            {
                toChange = root.GetNamespaceDeclaration(context.NamespaceToFix);
                if (toChange == null) return solution;
            }
            var typeDeclarationToChange = toChange.GetRootTypeDeclarations();

            // symbol representing the type
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbols = typeDeclarationToChange.Select(type => new { type, symbol = semanticModel.GetDeclaredSymbol(type, cancellationToken) });
            // get all references
            var getReferencesTasks = typeSymbols.Select(type => new { type.type, type.symbol, refTask = SymbolFinder.FindReferencesAsync(type.symbol, solution) });
            var getReferencesResult = await Task.WhenAll(getReferencesTasks.Select(r => r.refTask));
            // get all documents with references
            var references = getReferencesTasks.SelectMany(x => x.refTask.Result.Select(reference => new { x.type, x.symbol, reference }));
            var documents = references
                .SelectMany(r => r.reference.Locations)
                .Select(l => new { l.Document.Id, l.Location.SourceTree, l.Location.SourceSpan });

            foreach (var d in documents)
            {
                var docRoot = d.SourceTree.GetCompilationUnitRoot();
                var tn = docRoot
                    .FindNode(d.SourceSpan)
                    .AncestorsAndSelf()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .First()
                    .GetNamespace();

                docRoot = docRoot.AddUsing(context.NewNamespace, tn);
                solution = solution.GetDocument(d.Id).WithSyntaxRoot(docRoot).Project.Solution;
            }

            root = root.ChangeNamespace(context.NamespaceToFix, context.NewNamespace);
            solution = solution.GetDocument(context.DocumentId).WithSyntaxRoot(root).Project.Solution;

            return solution;
        }
    }
}