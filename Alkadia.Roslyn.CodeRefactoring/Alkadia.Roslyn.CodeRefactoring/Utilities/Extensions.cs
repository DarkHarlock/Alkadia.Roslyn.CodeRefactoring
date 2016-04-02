namespace Alkadia.Roslyn.CodeRefactoring.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class Extensions
    {
        public static string Join(this IEnumerable<string> items, string separator)
        {
            return string.Join(separator, items);
        }

        public static string GetConventionalNamespace(this Document document)
        {
            var dot = document.Folders.Any() ? "." : string.Empty;
            return $"{document.Project.AssemblyName}{dot}{document.Folders.Join(".")}";
        }

        public static string GetNamespace(this BaseTypeDeclarationSyntax node)
        {
            var ns = node
                .Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .Reverse();
            return ns.Select(n => n.Name.ToString()).Join(".");
        }

        public static Document SearchDocument(this Project project, string name, string[] folders)
        {
            var newFileNameLower = name.ToLowerInvariant();
            return project.Documents
                .Where(d => d.Name.ToLowerInvariant() == newFileNameLower)
                //and that target new file does not exist in project
                .Where(d => d.Folders.Count == folders.Length)
                .FirstOrDefault(d =>
                    d.Folders.Zip(
                        folders,
                        (f, s) => string.Compare(f, s, StringComparison.OrdinalIgnoreCase) == 0
                    ).All(f => f)
                );
        }
    }
}