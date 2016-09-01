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
            const string TestCase = @"
using System;
namespace Test {
    public class Foo {
        private class Inner { }
        public Foo() {
            var innerDecl = new DateTime(2016, 3, 21, 0, 0, 0);
        }
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
                "Error",
                "innerDecl",
                "DateTime",
                "new"
            }.Select(c => GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf(c, StringComparison.Ordinal), c.Length),
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
        public async Task Should_register_move_class_when_more_than_one_declaration_exist_and_context_is_a_basetype_and_target_file_by_convention_does_not_exist(
            ICodeRefactoringContextSubscriber interceptor,
            MoveClassCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
using System;
namespace TestSuite {
    namespace Inner {
        public class Foo {
            private class Inner { }
        }
    }
    namespace Folder {
        public class OtherClass {
            public class InnerOther { }
        }
        public struct OtherStruct {
        }
        public enum OtherEnum {
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
                "Foo", //class Foo can only be extracted on folder Inner not on itself
                "OtherClass", //class OtherClass has right namespace, so can only be extracted in current folder
                "OtherStruct", //same as before
                "OtherEnum", //same as before
                "Alone" //class Alone is from a namespace not based on assembly so can be only extracted in current folder
            }.Select(c => GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf(c, StringComparison.Ordinal), c.Length),
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
                .MustHaveHappened(Repeated.Exactly.Times(cases.Count()));

            Assert.Equal("Move class into '\\Inner\\Foo.cs'", actions[0].Title);
            Assert.Equal("Move class into '\\Folder\\OtherClass.cs'", actions[1].Title);
            Assert.Equal("Move class into '\\Folder\\OtherStruct.cs'", actions[2].Title);
            Assert.Equal("Move class into '\\Folder\\OtherEnum.cs'", actions[3].Title);
            Assert.Equal("Move class into '\\Folder\\Alone.cs'", actions[4].Title);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_rename_and_move_document(
            ICodeRefactoringContextSubscriber interceptor,
            MoveClassCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
using System;
namespace TestSuite.Inner {
    public class Foo {
    }
}
";
            var context = GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf("Foo", StringComparison.Ordinal), "Foo".Length),
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

            Assert.Equal("Rename File to 'Foo.cs'", actions[0].Title);
            Assert.Equal("Move File to '\\Inner\\Foo.cs'", actions[1].Title);
        }
    }
}