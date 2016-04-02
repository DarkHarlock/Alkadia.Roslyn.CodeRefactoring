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
    public class InitializeFieldsFromConstructorCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string RefactoringId = "Alkadia.CodeRefactoring.InitializeFieldsFromConstructor";

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var project = document.Project;
            var solution = project.Solution;

            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var parameter = root.FindNode(context.Span).FirstAncestorOrSelf<ParameterSyntax>();
            if (parameter == null) return;

            var parameterName = parameter.GetParameterName();
            if (string.IsNullOrWhiteSpace(parameterName)) return;

            var constructor = parameter.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (constructor == null) return;
            var constructorStatements = constructor.Body.Statements;
            var isAssigned = constructorStatements
                .OfType<ExpressionStatementSyntax>()
                .Select(s => new { statement = s, expression = s.Expression as AssignmentExpressionSyntax })
                .Where(s => s.expression != null)
                .Where(a => a.expression.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                .Where(a => a.expression.Right.IsKind(SyntaxKind.IdentifierName)) //ensure safety in the following cast
                .Any(a => (a.expression.Right as IdentifierNameSyntax).Identifier.ValueText == parameterName);

            if (isAssigned) return;

            context.RegisterRefactoring(new InitializeFieldsFromConstructorCodeAction(new InitializeFieldsFromConstructorCodeActionContext
            {
                Solution = solution,
                DocumentId = document.Id,
                Span = context.Span,
                ParameterName = parameterName
            }));
        }
    }
}