namespace Alkadia.Roslyn.CodeRefactoring.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Formatting;
    using Microsoft.CodeAnalysis.Simplification;
    using Microsoft.CodeAnalysis.Text;
    public static class SyntaxExtensions
    {
        public static IEnumerable<BaseTypeDeclarationSyntax> GetRootTypeDeclarations(this SyntaxNode node)
        {
            return node
                .DescendantNodes(n => !(n is BaseTypeDeclarationSyntax))
                .OfType<BaseTypeDeclarationSyntax>();
        }
        public static IEnumerable<NamespaceDeclarationSyntax> GetRootNamespaceDeclarations(this SyntaxNode node)
        {
            return node
                .DescendantNodes(x => x == node || !(x is NamespaceDeclarationSyntax))
                .OfType<NamespaceDeclarationSyntax>();
        }
        private class Node<T>
        {
            public Node<T> Parent { get; set; }
            public List<Node<T>> Child { get; } = new List<Node<T>>();
            public T Value { get; set; }

            public IEnumerable<T> GetPath()
            {
                var parent = this;
                while (parent.Value != null)
                {
                    yield return parent.Value;
                    parent = parent.Parent;
                }
            }
        }
        public static NamespaceDeclarationSyntax GetNamespaceDeclaration(this SyntaxNode node, string @namespace)
        {
            var nodeDict = new Dictionary<SyntaxNode, Node<SyntaxNode>>();

            var root = new Node<SyntaxNode> { Value = node };
            var queue = new Queue<Node<SyntaxNode>>(new[] { root });
            do
            {
                var nds = queue.Dequeue();
                nodeDict.Add(nds.Value, nds);
                var inner = nds.Value.GetRootNamespaceDeclarations().Select(n => new Node<SyntaxNode> { Value = n, Parent = nds });
                foreach (var c in inner)
                {
                    nds.Child.Add(c);
                    queue.Enqueue(c);
                }
            } while (queue.Count > 0);
            nodeDict[node].Value = null;

            var result = nodeDict.Values.Select(v => new
            {
                v,
                c = v.GetPath().OfType<NamespaceDeclarationSyntax>().Reverse().ToArray()
            })
            .Select(x => new
            {
                Value = x.v.Value as NamespaceDeclarationSyntax,
                c = x.c.Select(n => n.Name.ToString()).Join(".")
            })
            .Where(x => x.c == @namespace)
            .FirstOrDefault();

            return result?.Value;
        }
        public static NamespaceDeclarationSyntax IsNamespaceDeclaration(this SyntaxNode root, TextSpan span)
        {
            var node = root.FindNode(span);

            var namespaceIdentifier = node as IdentifierNameSyntax;
            if (namespaceIdentifier == null) return null;

            var namespaceDeclaration = namespaceIdentifier.Ancestors().FirstOrDefault();
            while (namespaceDeclaration is QualifiedNameSyntax)
            {
                namespaceDeclaration = namespaceDeclaration.Ancestors().FirstOrDefault();
            }

            var namespaceDecl = namespaceDeclaration as NamespaceDeclarationSyntax;
            if (namespaceDecl == null) return null;

            return namespaceDecl;
        }
        public static BaseTypeDeclarationSyntax[] GetRootBaseTypeDeclarations(this SyntaxNode root)
        {
            return root
                .DescendantNodes(n => !(n is BaseTypeDeclarationSyntax))
                .OfType<BaseTypeDeclarationSyntax>()
                .ToArray();
        }
    }
}
