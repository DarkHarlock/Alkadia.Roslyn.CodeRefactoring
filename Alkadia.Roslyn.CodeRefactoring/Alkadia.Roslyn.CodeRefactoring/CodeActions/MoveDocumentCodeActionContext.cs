namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    public struct MoveDocumentCodeActionContext
    {
        public Solution Solution { get; set; }
        public DocumentId DocumentId { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Folders { get; set; }
        public bool IsRename { get; set; }
    }
}