namespace Alkadia.Roslyn.CodeRefactoring
{
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;
    using Alkadia.Roslyn.CodeRefactoring.Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Alkadia.Roslyn.CodeRefactoring.CodeActions;

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
            var node = root.FindNode(context.Span).AncestorsAndSelf().OfType< BaseTypeDeclarationSyntax>().FirstOrDefault();

            // only for a type declaration node 
            var typeDecl = node as BaseTypeDeclarationSyntax;
            if (typeDecl == null) return;
            // also omit all private classes
            if (typeDecl.Modifiers.Any(SyntaxKind.PrivateKeyword)) return;
            // and nested classes (move and add partial??)
            if (typeDecl.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()) return;

            var newFileName = $"{typeDecl.Identifier.ToString()}.cs";

            var assemblyName = project.AssemblyName;
            var conventionalNamespace = document.GetConventionalNamespace();
            var typeNamespace = typeDecl.GetNamespace();

            if (typeNamespace.StartsWith($"{assemblyName}") && typeNamespace != conventionalNamespace)
            {
                var folders = typeNamespace.Substring(assemblyName.Length).Split('.').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (project.SearchDocument(newFileName, folders) == null)
                {
                    context.RegisterRefactoring(new MoveClassCodeAction(new MoveClassCodeActionContext
                    {
                        Solution = solution,
                        DocumentId = document.Id,
                        Span = context.Span,
                        Folders = folders,
                        Name = newFileName
                    }));
                }
            }

            //where target file does not exist
            if(project.SearchDocument(newFileName, document.Folders.ToArray()) == null)
            {
                context.RegisterRefactoring(new MoveClassCodeAction(new MoveClassCodeActionContext
                {
                    Solution = solution,
                    DocumentId = document.Id,
                    Span = context.Span,
                    Folders = document.Folders.ToArray(),
                    Name = newFileName
                }));
            }
        }
    }
}
