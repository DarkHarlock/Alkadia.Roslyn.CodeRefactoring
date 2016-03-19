namespace Alkadia.Roslyn.CodeRefactoring.Tests.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeItEasy;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.Text;
    using Ploeh.AutoFixture;
    using Xunit;
    using CodeRefactoring.CodeActions;
    using CodeRefactoring.Providers;
    using FluentAssertions;
    public class FixNamespaceCodeRefactoringProviderTests
    {
        //private readonly static Action<CodeAction> _empty = _ => { };
        public CodeRefactoringContext GetContext(
            string code,
            TextSpan span,
            string documentName = "Test",
            string projectName = "TestSuite",
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
        public async Task _Should_not_register_when_context_is_not_namespace_identifier(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
using System;
namespace Test {
    public class Foo {}
}
";
            await Task.WhenAll(new[]
            {
                "namespace",
                "using",
                "System",
                "public",
                "class",
                "Foo"
            }.Select(c => GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf(c, StringComparison.Ordinal), c.Length),
                interceptRegister: interceptor.Register
            )).Select(sut.ComputeRefactoringsAsync));

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustNotHaveHappened();
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_when_namespace_declaration_is_nested(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Test {
    namespace Inner {
        public class {
        }
    }
}
";

            var cases = new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("Inner", StringComparison.Ordinal), 4),
                projectName: "TestSuite",
                folders: new[] { "Folder" },
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
        public async Task Should_not_register_with_a_single_class_when_namespace_is_equal_to_assembly_and_class_is_equal_to_document(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase = @"
using System;
namespace TestSuite {
    public class Test {}
}
";
            var context = GetContext(
                TestCase,
                new TextSpan(TestCase.IndexOf("Suite", StringComparison.Ordinal), 4),
                interceptRegister: interceptor.Register
            );

            await sut.ComputeRefactoringsAsync(context);

            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .MustNotHaveHappened();
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_with_a_single_class_when_namespace_is_right_and_class_is_equal_to_document(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Folder {
    public class Test {}
}
";
            var cases = new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder" },
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
        public async Task Should_not_register_when_no_type_declarations_with_right_namespace(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Test {
}
";
            var cases = new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Test" },
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
        public async Task Should_register_ChangeNamespace_with_a_single_class_when_namespace_is_assembly_based_and_class_is_equal_to_document(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Folder {
    public class Test {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Inner", "Nested" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<ChangeNamespaceCodeAction>().ToArray();
            Assert.Equal(1, moveActions.Length);
            Assert.Equal("TestSuite.Folder", moveActions[0].FixParameters.NamespaceToFix);
            Assert.Equal("TestSuite.Inner.Nested", moveActions[0].FixParameters.NewNamespace);
            Assert.Equal($"Change Namespace to '{moveActions[0].FixParameters.NewNamespace}'", moveActions[0].Title);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_ChangeNamespace_when_class_has_no_typeidentifier_and_namespace_is_assembly_based(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Test {
    public class {
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<ChangeNamespaceCodeAction>().ToArray();
            Assert.Equal(1, moveActions.Length);

            Assert.Equal("TestSuite.Test", moveActions[0].FixParameters.NamespaceToFix);
            Assert.Equal("TestSuite.Folder", moveActions[0].FixParameters.NewNamespace);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_ChangeNamespace_when_namespace_is_not_assembly_name_based(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace Tests {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("est", StringComparison.Ordinal), 3),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<ChangeNamespaceCodeAction>().ToArray();
            Assert.Equal(1, moveActions.Length);

            Assert.NotNull(moveActions[0]);
            Assert.Equal("Tests", moveActions[0].FixParameters.NamespaceToFix);
            Assert.Equal("TestSuite.Folder", moveActions[0].FixParameters.NewNamespace);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_RenameDocument_when_namespace_is_equal_to_assembly_for_each_class_not_equal_to_document(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => m.FixParameters.IsRename).ToArray();
            Assert.Equal(2, moveActions.Length);

            Assert.Equal("Foo", moveActions[0].FixParameters.Name);
            moveActions[0].FixParameters.Folders.ShouldBeEquivalentTo(new string[0]);
            Assert.Equal($@"Rename File to '{moveActions[0].FixParameters.Name}.cs'", moveActions[0].Title);

            Assert.Equal("Other", moveActions[1].FixParameters.Name);
            moveActions[1].FixParameters.Folders.ShouldBeEquivalentTo(new string[0]);
            Assert.Equal($@"Rename File to '{moveActions[1].FixParameters.Name}.cs'", moveActions[1].Title);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_RenameDocument_when_namespace_is_right_for_each_class_not_equal_to_document(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Folder {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => m.FixParameters.IsRename).ToArray();
            Assert.Equal(2, moveActions.Length);

            Assert.Equal("Foo", moveActions[0].FixParameters.Name);
            Assert.Equal("Other", moveActions[1].FixParameters.Name);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_RenameDocument_when_namespace_is_assembly_based_for_each_class_not_equal_to_document(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Folder {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new string[] { },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => m.FixParameters.IsRename).ToArray();
            Assert.Equal(2, moveActions.Length);

            Assert.Equal("Foo", moveActions[0].FixParameters.Name);
            moveActions[0].FixParameters.Folders.ShouldBeEquivalentTo(new string[] { });

            Assert.Equal("Other", moveActions[1].FixParameters.Name);
            moveActions[1].FixParameters.Folders.ShouldBeEquivalentTo(new string[] { });
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_RenameDocument_when_namespace_is_not_assembly_name_based(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace Tests {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("est", StringComparison.Ordinal), 3),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().ToArray();
            Assert.Equal(2, moveActions.Length);

            var action = moveActions[0] as MoveDocumentCodeAction;
            Assert.NotNull(action);
            Assert.Equal("Foo", action.FixParameters.Name);
            Assert.True(action.FixParameters.IsRename);

            action = moveActions[1] as MoveDocumentCodeAction;
            Assert.NotNull(action);
            Assert.Equal("Other", action.FixParameters.Name);
            Assert.True(action.FixParameters.IsRename);
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_RenameDocument_when_class_has_no_typeidentifier(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Test {
    public class {
}
";

            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => m.FixParameters.IsRename).ToArray();
            Assert.Equal(0, moveActions.Length);
        }


        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_MoveDocument_when_namespace_is_equal_to_assembly_for_each_class(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => !m.FixParameters.IsRename).ToArray();
        }

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_not_register_MoveDocument_when_namespace_is_right_for_each_class(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Folder {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => !m.FixParameters.IsRename).ToArray();
            Assert.Equal(0, moveActions.Length);
        }

        /**/
        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_MoveDocument_when_namespace_is_assembly_based_for_each_class(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite {
    public class Foo {}
    public class Test {}
    public class Other {}
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder", "Inner" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => !m.FixParameters.IsRename).ToArray();
            Assert.Equal(3, moveActions.Length);

            Assert.Equal("Foo", moveActions[0].FixParameters.Name);
            moveActions[0].FixParameters.Folders.ShouldBeEquivalentTo(new string[] { });
            Assert.Equal($@"Move File to '{moveActions[0].FixParameters.Name}.cs'", moveActions[0].Title);

            Assert.Equal("Test", moveActions[1].FixParameters.Name);
            moveActions[1].FixParameters.Folders.ShouldBeEquivalentTo(new string[] { });
            Assert.Equal($@"Move File to '{moveActions[1].FixParameters.Name}.cs'", moveActions[1].Title);

            Assert.Equal("Other", moveActions[2].FixParameters.Name);
            moveActions[2].FixParameters.Folders.ShouldBeEquivalentTo(new string[] { });
            Assert.Equal($@"Move File to '{moveActions[2].FixParameters.Name}.cs'", moveActions[2].Title);
        }

        /**/

        [Theory]
        [AutoFakeItEasyData]
        public async Task Should_register_MoveDocument_when_class_has_no_typeidentifier_and_namespace_is_assembly_based(
            ICodeRefactoringContextSubscriber interceptor,
            FixNamespaceCodeRefactoringProvider sut
        )
        {
            const string TestCase1 = @"
using System;
namespace TestSuite.Test {
    public class {
}
";
            var created = new List<CodeAction>();
            A
                .CallTo(() => interceptor.Register(A<CodeAction>.Ignored))
                .Invokes((CodeAction act) => { created.Add(act); });

            await Task.WhenAll(new[]
            {
                TestCase1
            }.Select(c => GetContext(
                c,
                new TextSpan(c.IndexOf("estSuit", StringComparison.Ordinal), 4),
                folders: new[] { "Folder" },
                interceptRegister: interceptor.Register
            ))
            .Select(sut.ComputeRefactoringsAsync));

            var moveActions = created.OfType<MoveDocumentCodeAction>().Where(m => !m.FixParameters.IsRename).ToArray();
            Assert.Equal(1, moveActions.Length);

            Assert.Equal("Test", moveActions[0].FixParameters.Name);
            moveActions[0].FixParameters.Folders.ShouldBeEquivalentTo(new[] { "Test" });
        }



    }
}
