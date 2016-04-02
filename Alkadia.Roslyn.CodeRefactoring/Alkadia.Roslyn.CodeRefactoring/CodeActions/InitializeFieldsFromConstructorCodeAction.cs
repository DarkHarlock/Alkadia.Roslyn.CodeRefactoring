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
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using Utilities;
    public class InitializeFieldsFromConstructorCodeAction : CodeAction
    {
        private readonly InitializeFieldsFromConstructorCodeActionContext _fixContext;
        public InitializeFieldsFromConstructorCodeAction(InitializeFieldsFromConstructorCodeActionContext fixContext)
        {
            _fixContext = fixContext;
        }
        public override string Title
        {
            get { return $"Initialize field '_{_fixContext.ParameterName}'"; }
        }
        public InitializeFieldsFromConstructorCodeActionContext FixParameters
        {
            get { return _fixContext; }
        }
        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return FixAsync(_fixContext, cancellationToken);
        }
        private static async Task<Solution> FixAsync(InitializeFieldsFromConstructorCodeActionContext context, CancellationToken cancellationToken)
        {
            var solution = context.Solution;
            var options = solution.Workspace.Options;
            var document = solution.GetDocument(context.DocumentId);
            var tree = await document.GetSyntaxTreeAsync();
            var root = tree.GetCompilationUnitRoot(cancellationToken);

            var parameter = root.FindNode(context.Span).FirstAncestorOrSelf<ParameterSyntax>();
            var parameterName = parameter.GetParameterName();
            var fieldName = $"_{parameterName}";

            var constructor = parameter.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            var classDeclaration = constructor.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var hasField = classDeclaration.Members.OfType<FieldDeclarationSyntax>().Any(f => f.Declaration.Variables.Any(v => v.Identifier.ValueText == fieldName));

            var fieldPosition = constructor.ParameterList.Parameters.IndexOf(parameter);
            var parameterFieldMap = constructor.ParameterList.Parameters
                .Select(Utilities.SyntaxExtensions.GetParameterName)
                .Select(pName => new
                {
                    parameterName = pName,
                    fieldName = $"_{pName}"
                })
                .Take(fieldPosition)
                .ToArray();


            var constructorStatements = constructor.Body.Statements;
            var insertPosition = 0;
            if (constructorStatements.Any())
            {
                for (var i = parameterFieldMap.Length - 1; i >= 0; i--)
                {
                    var currentParameterName = parameterFieldMap[i].parameterName;
                    var assignment = constructorStatements
                        .OfType<ExpressionStatementSyntax>()
                        .Select(s => new { statement = s, expression = s.Expression as AssignmentExpressionSyntax })
                        .Where(s => s.expression != null)
                        .Where(a => a.expression.OperatorToken.IsKind(SyntaxKind.EqualsToken))
                        .Where(a => a.expression.Right.IsKind(SyntaxKind.IdentifierName))
                        .Where(a => (a.expression.Right as IdentifierNameSyntax).Identifier.ValueText == currentParameterName)
                        .Select(a => a.statement)
                        .FirstOrDefault();

                    if (assignment == null) continue;
                    insertPosition = constructor.Body.Statements.IndexOf(assignment) + 1;
                    break;
                }
            }

            var fieldInsertPosition = 0;
            if (!hasField)
            {
                for (var i = parameterFieldMap.Length - 1; i >= 0; i--)
                {
                    var currentFieldName = parameterFieldMap[i].fieldName;
                    var fDecl = classDeclaration.Members.OfType<FieldDeclarationSyntax>().FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.ValueText == currentFieldName));
                    if (fDecl == null) continue;

                    fieldInsertPosition = classDeclaration.Members.IndexOf(fDecl) + 1;
                    break;
                }
            }

            var assignStatement = Formatter.Format(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(fieldName),
                        SyntaxFactory.IdentifierName(parameterName)
                    )
                ).WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed)),
                solution.Workspace
            ) as ExpressionStatementSyntax;

            constructorStatements = constructorStatements.Insert(insertPosition, assignStatement);

            var newConstructorBody = constructor.Body.WithStatements(constructorStatements);
            var newClass = classDeclaration.ReplaceNode(constructor.Body, newConstructorBody);

            if (!VariableExists(newClass, fieldName))
            {
                var type = GetParameterType(parameter);
                var field = Formatter.Format(CreateFieldDeclaration(type, fieldName), solution.Workspace) as FieldDeclarationSyntax;

                newClass = newClass.WithMembers(newClass.Members.Insert(fieldInsertPosition, field));
            }

            root = root.ReplaceNode(classDeclaration, newClass);
            document = document.WithSyntaxRoot(root);
            return document.Project.Solution;
        }

        public static string GetParameterType(ParameterSyntax parameter)
        {
            return parameter
                .DescendantNodes()
                .First(node => node is PredefinedTypeSyntax || node is IdentifierNameSyntax)
                .GetFirstToken()
                .ValueText;
        }

        public static FieldDeclarationSyntax CreateFieldDeclaration(string type, string name)
        {
            return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(type))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name)))))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
                .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.CarriageReturnLineFeed))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        public static bool VariableExists(ClassDeclarationSyntax root, params string[] variableNames)
        {
            return root
                .Members
                .OfType<FieldDeclarationSyntax>()
                .Select(f => f.Declaration)
                .SelectMany(ps => ps.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken) && variableNames.Contains(t.ValueText)))
                .Any();
        }
    }
}