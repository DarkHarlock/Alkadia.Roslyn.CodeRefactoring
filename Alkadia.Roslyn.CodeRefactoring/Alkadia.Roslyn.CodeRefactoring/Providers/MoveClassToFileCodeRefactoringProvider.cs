namespace Alkadia.Roslyn.CodeRefactoring.Providers
{
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using CodeActions;
    using Utilities;

    [ExportCodeRefactoringProvider(RefactoringId, LanguageNames.CSharp), Shared]
    public class MoveClassCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string RefactoringId = "Alkadia.CodeRefactoring.MoveClass";

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var project = document.Project;
            var solution = project.Solution;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            // only for a type declaration node 
            var typeDecl = node as BaseTypeDeclarationSyntax;
            if (typeDecl == null) return;
            // also omit all private classes
            if (typeDecl.Modifiers.Any(SyntaxKind.PrivateKeyword)) return;
            // and nested classes (move and add partial??)
            if (typeDecl.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()) return;

            var baseTypeDeclarations = root.GetRootBaseTypeDeclarations();
            var isSingleDeclaration = baseTypeDeclarations.Length == 1;

            var newFileName = typeDecl.Identifier.ValueText;
            var newFileNameToSearch = $"{newFileName}.cs";

            var assemblyName = project.AssemblyName;
            var conventionalNamespace = document.GetConventionalNamespace();
            var typeNamespace = typeDecl.GetNamespace();

            var originalFolders = document.Folders.ToArray();
            var folders = originalFolders;
            var tryMove = typeNamespace.StartsWith($"{assemblyName}", System.StringComparison.Ordinal) && typeNamespace != conventionalNamespace;
            if (tryMove)
            {
                folders = typeNamespace
                    .Substring(assemblyName.Length)
                    .Split('.')
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }
            var canMove = tryMove && project.SearchDocument(newFileNameToSearch, folders) == null;
            var canExtract = project.SearchDocument(newFileNameToSearch, document.Folders.ToArray()) == null;

            if (canExtract && !isSingleDeclaration)
            {
                //extract class in the current document folder
                context.RegisterRefactoring(new MoveClassCodeAction(new MoveClassCodeActionContext
                {
                    Solution = solution,
                    DocumentId = document.Id,
                    Span = context.Span,
                    Folders = originalFolders,
                    Name = newFileName
                }));
            }
            if (canExtract && isSingleDeclaration)
            {
                //rename file
                context.RegisterRefactoring(new RenameDocumentCodeAction(new RenameDocumentCodeActionContext
                {
                    Solution = solution,
                    DocumentId = document.Id,
                    Name = newFileName
                }));
            }
            if (canMove && !isSingleDeclaration)
            {
                //extract class in a namespace based folder
                context.RegisterRefactoring(new MoveClassCodeAction(new MoveClassCodeActionContext
                {
                    Solution = solution,
                    DocumentId = document.Id,
                    Span = context.Span,
                    Folders = folders,
                    Name = newFileName
                }));
            }
            if (canMove && isSingleDeclaration)
            {
                //move & rename file
                context.RegisterRefactoring(new MoveDocumentCodeAction(new MoveDocumentCodeActionContext
                {
                    Solution = solution,
                    DocumentId = document.Id,
                    Folders = folders,
                    Name = newFileName
                }));
            }
        }
    }
}
