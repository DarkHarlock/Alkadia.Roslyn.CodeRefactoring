namespace Alkadia.Roslyn.CodeRefactoring.CodeActions
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;
    public struct InitializeFieldsFromConstructorCodeActionContext
    {
        public Solution Solution { get; set; }
        public DocumentId DocumentId { get; set; }
        public TextSpan Span { get; set; }
        public string ParameterName { get; set; }
    }
}