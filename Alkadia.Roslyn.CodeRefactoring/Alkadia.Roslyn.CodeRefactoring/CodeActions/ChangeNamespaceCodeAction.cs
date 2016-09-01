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
        private readonly ChangeNamespaceCodeActionContext _fixContext;
        public ChangeNamespaceCodeAction(ChangeNamespaceCodeActionContext fixContext)
        {
            _fixContext = fixContext;
            _namespace = fixContext.NewNamespace;
        }
        public override string Title
        {
            get { return $"Change Namespace to '{_namespace}'"; }
        }
        public ChangeNamespaceCodeActionContext FixParameters
        {
            get { return _fixContext; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return FixAsync(_fixContext, cancellationToken);
        }

        private static string ResolveNamespace(INamespaceSymbol nsSymbol)
        {
            var ret = nsSymbol.Name;
            while (nsSymbol.ContainingNamespace != null)
            {
                if (string.IsNullOrWhiteSpace(nsSymbol.ContainingNamespace.Name))
                    break;

                ret = $"{nsSymbol.ContainingNamespace.Name}.{ret}";
                nsSymbol = nsSymbol.ContainingNamespace;
            }
            return ret;
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
            var typeSymbols = typeDeclarationToChange
                .Select(type => new { type, symbol = (ISymbol)semanticModel.GetDeclaredSymbol(type, cancellationToken) }).ToList();

            typeSymbols = typeSymbols.Concat(
                typeDeclarationToChange.OfType<ClassDeclarationSyntax>()
                .SelectMany(type => type.Members)
                .Select(m => new { type = (BaseTypeDeclarationSyntax)m.Parent, symbol = semanticModel.GetDeclaredSymbol(m, cancellationToken) as IMethodSymbol })
                .Where(m => m.symbol != null)
                .Where(m => m.symbol.IsExtensionMethod)
                .Select(m => new { m.type, symbol = (ISymbol)m.symbol })
            ).ToList();

            // get all references
            var getReferencesTasks = typeSymbols.Select(async type => new { type.type, type.symbol, @ref = await SymbolFinder.FindReferencesAsync(type.symbol, solution) });
            var getReferencesResult = await Task.WhenAll(getReferencesTasks);
            // get all documents with references
            var references = getReferencesResult.SelectMany(x => x.@ref.Select(reference => new { x.type, x.symbol, reference }));
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
            document = solution.GetDocument(context.DocumentId).WithSyntaxRoot(root);
            var model = await document.GetSemanticModelAsync(cancellationToken);

            var unresolved = (await document.GetSyntaxTreeAsync(cancellationToken))
                .GetCompilationUnitRoot(cancellationToken)
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(x => model.GetSymbolInfo(x).Symbol == null);                

            var candidateList = await Task.WhenAll(unresolved.Select(id => SymbolFinder.FindDeclarationsAsync(document.Project, id.Identifier.ValueText, false)));
            var candidates = candidateList
                .SelectMany(x => x)
                .Select(x => ResolveNamespace(x.ContainingNamespace))
                .Distinct()
                .ToList();

            foreach (var candidate in candidates)
            {
                if (context.NamespaceToFix.Contains(candidate))
                    root = root.AddUsing(candidate, null);
            }

            document = solution.GetDocument(context.DocumentId).WithSyntaxRoot(root);
            solution = document.Project.Solution;

            return solution;
        }
    }
}