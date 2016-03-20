namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using Microsoft.CodeAnalysis;

    public struct RenameDocumentCodeActionContext
    {
        public Solution Solution { get; set; }
        public DocumentId DocumentId { get; set; }
        public string Name { get; set; }
    }
}