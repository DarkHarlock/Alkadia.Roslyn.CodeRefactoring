namespace Alkadia.Roslyn.CodeRefactoring.Tests
{
    using Microsoft.CodeAnalysis.CodeActions;
    public interface ICodeRefactoringContextSubscriber
    {
        void Register(CodeAction action);
    }
}