namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using Microsoft.CodeAnalysis;
    public struct ChangeNamespaceCodeActionContext
    {
        public Solution Solution { get; set; }
        public DocumentId DocumentId { get; set; }
        public string NamespaceToFix { get; set; }
        public string NewNamespace { get; set; }
    }
}