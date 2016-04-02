namespace Alkadia.Roslyn.CodeRefactoring.Tests.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeRefactoring.CodeActions;
    using CodeRefactoring.Providers;
    using FakeItEasy;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.Text;
    using Xunit;


    public class InitializeFieldsFromConstructorCodeRefactoringProviderTests
    {
        //private readonly static Action<CodeAction> _empty = _ => { };
        public CodeRefactoringContext GetContext(
            string code,
            TextSpan span,
            string documentName = "Test",
            string projectName = "Test",
            IEnumerable<string> folders = null,
            Action<CodeAction> interceptRegister = null)
        {
            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject(ProjectInfo.Create(
                ProjectId.CreateNewId(),
                VersionStamp.Default,
                projectName,
                projectName,
                LanguageNames.CSharp
            ));
            var document = project.AddDocument($"{documentName}.cs", code, folders);

            var context = new CodeRefactoringContext(
                document,
                span,
                interceptRegister,
                CancellationToken.None
            );

            return context;
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_when_context_is_not_a_constructor_parameter_or_parameter_name_is_missing(
            ICodeRefactoringContextSubscriber interceptor,
            InitializeFieldsFromConstructorCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
namespace Test {
    public class Foo {
        private class Inner { }
        public Foo() {}
        public Foo(int intParam, MissingParameterName) {
            var innerDecl = new DateTime(2016, 3, 21, 0, 0, 0);
        }
        public void MyMethod(MyType boolParam) { }
    }
}
";
            var cases = new[]
            {
                "MissingParameterName",
                "namespace",
                "Test",
                "public",
                "Foo",
                "class I",
                "Inner",
                "Foo(",
                "innerDecl",
                "DateTime",
                "MyMethod",
                "MyType",
                "boolParam",
                "()"
            }.Select(c => GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf(c, StringComparison.Ordinal) + 1, 0),
                projectName: "TestSuite",
                interceptRegister: interceptor.Register
            ));

            foreach (var context in cases)
                await sut.ComputeRefactoringsAsync(context);

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustNotHaveHappened();
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_if_parameter_is_already_assigned(
            ICodeRefactoringContextSubscriber interceptor,
            InitializeFieldsFromConstructorCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
namespace Test {
    public class Foo {
        public Foo(int intParam) {
            _tmp = intParam;
        }
    }
    public class Foo2 {
        public Foo(bool boolParam) {
            this.tmp = boolParam;
        }
    }
}
";
            var cases = new[]
            {
                "intParam",
                "boolParam"
            }.Select(c => GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf(c, StringComparison.Ordinal) + 1, 0),
                projectName: "TestSuite",
                interceptRegister: interceptor.Register
            ));

            foreach (var context in cases)
                await sut.ComputeRefactoringsAsync(context);

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustNotHaveHappened();
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_with_correct_parameters(
            ICodeRefactoringContextSubscriber interceptor,
            InitializeFieldsFromConstructorCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
namespace Test {
    public class Foo {
        public Foo(int intParam) {
            field = otherField;
            intParam = 12;
        }
    }
}
";
            var cases = new[]
            {
                "intParam"
            }.Select(c => GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf(c, StringComparison.Ordinal) + 1, 0),
                projectName: "TestSuite",
                interceptRegister: interceptor.Register
            )).ToArray();

            var actions = new List<InitializeFieldsFromConstructorCodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { actions.Add(act as InitializeFieldsFromConstructorCodeAction); });

            foreach (var context in cases)
                await sut.ComputeRefactoringsAsync(context);

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustHaveHappened(Repeated.Exactly.Once);

            Assert.Equal("Initialize field '_intParam'", actions[0].Title);

            Assert.Equal(cases[0].Document.Id, actions[0].FixParameters.DocumentId);
            Assert.Equal(cases[0].Document.Project.Solution.Id, actions[0].FixParameters.Solution.Id);
            Assert.Equal("intParam", actions[0].FixParameters.ParameterName);
            Assert.Equal(cases[0].Span, actions[0].FixParameters.Span);
        }

    }
}