namespace Alkadia.Roslyn.CodeRefactoring.Utilities
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using Microsoft.CodeAnalysis.Text;

    public static class SyntaxUtilities
    {
        private static UsingDirectiveSyntax CreateUsing(string @namespace)
        {
            return SyntaxFactory.UsingDirective(
                SyntaxFactory
                .IdentifierName(@namespace)
                .WithLeadingTrivia(SyntaxFactory.Space)
            )
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .WithAdditionalAnnotations(Formatter.Annotation)
            .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public static CompilationUnitSyntax ChangeNamespace(this CompilationUnitSyntax root, string @namespace, string newNamespace)
        {
            if (@namespace == null)
            {
                var members = root.Members;
                root = root.WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.IdentifierName(newNamespace)
                            .WithLeadingTrivia(SyntaxFactory.Space)
                            .WithTrailingTrivia(SyntaxFactory.ElasticSpace)
                            .WithAdditionalAnnotations(Formatter.Annotation)
                        )
                        .WithOpenBraceToken(
                            SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                            .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
                            .WithAdditionalAnnotations(Formatter.Annotation)
                        )
                        .WithAdditionalAnnotations(Formatter.Annotation)
                        .WithMembers(SyntaxFactory.List(members))
                    )
                )
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                .WithAdditionalAnnotations(Formatter.Annotation);

                return root;
            }

            var decl = root.GetNamespaceDeclaration(@namespace);
            if (decl == null) return root;

            var newdecl = decl.WithName(SyntaxFactory.IdentifierName(newNamespace).WithTrailingTrivia(SyntaxFactory.ElasticSpace));
            root = root.ReplaceNode(decl, newdecl);
            return root;
        }
        public static CompilationUnitSyntax AddUsing(this CompilationUnitSyntax root, string @namespace, string targetNamespace)
        {
            var usings = root.DescendantNodes(n => true).OfType<UsingDirectiveSyntax>();
            var lastUsing = usings.LastOrDefault();
            var namespaceDecl = !string.IsNullOrWhiteSpace(targetNamespace) ? root.GetNamespaceDeclaration(targetNamespace) : null;

            if (namespaceDecl == null || lastUsing == null || !lastUsing.Ancestors().OfType<NamespaceDeclarationSyntax>().Any())
            {
                root = root.AddUsings(CreateUsing(@namespace));
                return root;
            }

            var nsTTokens = targetNamespace.Split('.');
            var nsTokens = @namespace.Split('.');
            var i = 0;
            while (i < nsTokens.Length && i < nsTTokens.Length && nsTTokens[i] == nsTokens[i]) { i++; }
            nsTokens = nsTokens.Skip(i).ToArray();
            if (nsTokens.Length == 0)
                return root;

            @namespace = nsTokens.Join(".");
            var newNamespaceDecl = namespaceDecl.AddUsings(CreateUsing(@namespace));

            root = root.ReplaceNode(namespaceDecl, newNamespaceDecl);
            return root;
        }

        public static CompilationUnitSyntax ExtractClass(this CompilationUnitSyntax root, TextSpan span)
        {
            var node = root.FindNode(span);
            var typeDecl = node as BaseTypeDeclarationSyntax;
            if (typeDecl == null) return null;

            var member = node as MemberDeclarationSyntax;
            var namespaceDeclarations = typeDecl
                .Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .Select(n => (member = n.WithMembers(SyntaxFactory.SingletonList(member))));
            var namespaceDecl = namespaceDeclarations.LastOrDefault() as MemberDeclarationSyntax;
            if (namespaceDecl == null)
                namespaceDecl = typeDecl;

            var usings = root
                .DescendantNodesAndSelf(n => true)
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.WithAdditionalAnnotations(Simplifier.Annotation));

            var extAlias = root
                .ChildNodes()
                .OfType<ExternAliasDirectiveSyntax>();

            var ret = SyntaxFactory.CompilationUnit(
                    SyntaxFactory.List(extAlias),
                    SyntaxFactory.List(usings),
                    SyntaxFactory.List<AttributeListSyntax>(),
                    SyntaxFactory.SingletonList(namespaceDecl)
            );

            return ret;
        }
    }
}