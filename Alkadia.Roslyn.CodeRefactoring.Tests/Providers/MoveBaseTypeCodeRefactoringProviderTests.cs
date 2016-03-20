namespace Alkadia.Roslyn.CodeRefactoring.Tests.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeRefactoring.Providers;
    using FakeItEasy;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.Text;
    using Xunit;

    public class MoveBaseTypeCodeRefactoringProviderTests
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
        public async Task Should_not_register_when_context_is_not_a_basetype(
            ICodeRefactoringContextSubscriber interceptor,
            MoveClassCodeRefactoringProvider sut
        )
        {
            const string CaseTest = @"
using System;
namespace Test {
    public class Foo {
        private class Inner { }
    }
    public class Other {
        public class InnerOther { }
    }
    private class Error { }
}
";
            var cases = new[]
            {
                "namespace",
                "using",
                "System",
                "Test",
                "Inner",
                "InnerOther",
                "Error"
            }.Select(c => GetContext(
                CaseTest,
                new TextSpan(CaseTest.IndexOf(c, StringComparison.Ordinal), c.Length),
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
        public async Task Should_register_when_context_is_a_basetype_and_target_file_by_convention_does_not_exist(
            ICodeRefactoringContextSubscriber interceptor,
            MoveClassCodeRefactoringProvider sut
        )
        {
            const string CaseTest = @"
using System;
namespace TestSuite {
    namespace Inner {
        public class Foo {
            private class Inner { }
        }
    }
    namespace Folder {
        public class Other {
            public class InnerOther { }
        }
    }
}
namespace Test {
    public class Alone {
        private class Inner { }
    }
}
";
            var cases = new[]
            {
                "Foo",
                "Other",
                "Alone"
            }.Select(c => GetContext(
                CaseTest,
                new TextSpan(CaseTest.IndexOf(c, StringComparison.Ordinal), c.Length),
                projectName: "TestSuite",
                documentName: "Foo",
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ));

            var actions = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { actions.Add(act); });

            foreach (var context in cases)
                await sut.ComputeRefactoringsAsync(context);

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustHaveHappened(Repeated.Exactly.Times(3));

            Assert.Equal("Move class into '\\Inner\\Foo.cs'", actions[0].Title);
            Assert.Equal("Move class into '\\Folder\\Other.cs'", actions[1].Title);
            Assert.Equal("Move class into '\\Folder\\Alone.cs'", actions[2].Title);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_twice(
            ICodeRefactoringContextSubscriber interceptor,
            MoveClassCodeRefactoringProvider sut
        )
        {
            const string CaseTest = @"
using System;
namespace TestSuite.Inner {
    public class Foo {
        private class Inner { }
    }
}
";
            var context = GetContext(
                CaseTest,
                new TextSpan(CaseTest.IndexOf("Foo", StringComparison.Ordinal), "Foo".Length),
                projectName: "TestSuite",
                documentName: "Other",
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            );

            var actions = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { actions.Add(act); });

            await sut.ComputeRefactoringsAsync(context);

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustHaveHappened(Repeated.Exactly.Twice);

            Assert.Equal("Move class into '\\Inner\\Foo.cs'", actions[0].Title);
            Assert.Equal("Move class into '\\Folder\\Foo.cs'", actions[1].Title);
        }
    }
}