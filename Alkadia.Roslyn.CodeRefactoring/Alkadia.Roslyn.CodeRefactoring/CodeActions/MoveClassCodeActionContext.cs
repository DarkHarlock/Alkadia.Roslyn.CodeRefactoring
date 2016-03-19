namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;
    public struct MoveClassCodeActionContext
    {
        public Solution Solution { get; set; }
        public DocumentId DocumentId { get; set; }
        public TextSpan Span { get; set; }
        public string Name { get; set; }
        public string[] Folders { get; set; }
    }
}