namespace Alkadia.Roslyn.CodeRefactoring.Providers
{
    using System;
    using System.Composition;
    using System.Linq;
    using System.Threading.Tasks;
    using Utilities;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Alkadia.Roslyn.CodeRefactoring.CodeActions;

    [ExportCodeRefactoringProvider(RefactoringId, LanguageNames.CSharp), Shared]
    public class FixNamespaceCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string RefactoringId = "Alkadia.CodeRefactoring.FixNamespace";

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var project = document.Project;
            var solution = project.Solution;
            var currentName = document.Name.Substring(0, document.Name.Length - 3); // .cs -> 3 char

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            //target must be a namespace declaration
            var namespaceDecl = root.IsNamespaceDeclaration(context.Span);
            if (namespaceDecl == null) return;

            //target must not be contained in an another namespace declaration 
            if (namespaceDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().Any()) return;

            //target must be not conventional
            var currentNamespace = namespaceDecl.Name.ToString();
            var @namespace = document.GetConventionalNamespace();

            var isSameNamespace = currentNamespace == @namespace;
            var isAssemblyBasedNamespace = currentNamespace.StartsWith(context.Document.Project.AssemblyName);

            if (!isSameNamespace)
            {
                context.RegisterRefactoring(new ChangeNamespaceCodeAction(new ChangeNamespaceCodeActionContext
                {
                    DocumentId = document.Id,
                    Solution = solution,
                    NamespaceToFix = currentNamespace,
                    NewNamespace = @namespace
                }));
            }

            var typeDeclarations = namespaceDecl.GetRootBaseTypeDeclarations();
            if (!typeDeclarations.Any()) return;

            var folders = 
                isAssemblyBasedNamespace 
                    ? currentNamespace
                        .Substring(context.Document.Project.AssemblyName.Length)
                        .Split('.')
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray()
                    : new string[] { };

            //check for file rename
            foreach (var typedecl in typeDeclarations)
            {
                var typeName = typedecl.Identifier.ValueText;
                if (string.IsNullOrWhiteSpace(typeName))
                    typeName = currentName;

                if (string.Compare(currentName, typeName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    context.RegisterRefactoring(new MoveDocumentCodeAction(new MoveDocumentCodeActionContext
                    {
                        DocumentId = document.Id,
                        Solution = solution,
                        Name = typeName,
                        Folders = document.Folders,
                        IsRename = true
                    }));
                }

                if (isAssemblyBasedNamespace && !isSameNamespace)
                {
                    context.RegisterRefactoring(new MoveDocumentCodeAction(new MoveDocumentCodeActionContext
                    {
                        DocumentId = document.Id,
                        Solution = solution,
                        Name = typeName,
                        Folders = folders
                    }));
                }
            }
        }
    }
}
