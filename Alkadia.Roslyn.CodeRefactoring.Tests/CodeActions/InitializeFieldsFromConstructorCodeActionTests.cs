namespace Alkadia.Roslyn.CodeRefactoring.Tests.CodeActions
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Text;
    using Xunit;
    using CodeRefactoring.CodeActions;
    using Microsoft.CodeAnalysis.Formatting;
    using System;

    public class InitializeFieldsFromConstructorCodeActionTests
    {
        private class TestableInitializeFieldsFromConstructorCodeAction : InitializeFieldsFromConstructorCodeAction
        {
            public TestableInitializeFieldsFromConstructorCodeAction(InitializeFieldsFromConstructorCodeActionContext fix)
                : base(fix)
            {

            }

            public Task<Solution> Execute(CancellationToken cancellationToken)
            {
                return GetChangedSolutionAsync(cancellationToken);
            }
        }
        public Document CreateDocument(string text)
        {
            var workspace = new AdhocWorkspace();
            var solution = workspace.AddSolution(SolutionInfo.Create(
                SolutionId.CreateNewId(),
                VersionStamp.Default
            ));
            var projectId = ProjectId.CreateNewId();
            var project = solution.AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                "TestSuite",
                "TestSuite",
                LanguageNames.CSharp
            )).GetProject(projectId);
            var document = project.AddDocument("Test.cs", text);
            return document;
        }

        [Fact]
        public async Task Should_add_initialialization_on_parameter_identifier()
        {
            const string TestCase = @"namespace TestSuite {
    public class Foo {
        public Foo(string tparam) 
        {
        }
    }
}";
            const string ExpectedSource = @"namespace TestSuite {
    public class Foo {
private readonly string _tparam;
        public Foo(string tparam) 
        {
_tparam = tparam;
        }
    }
}";
            const string FormattedExpectedSource = @"namespace TestSuite
{
    public class Foo
    {
        private readonly string _tparam;
        public Foo(string tparam)
        {
            _tparam = tparam;
        }
    }
}";
            var document = CreateDocument(TestCase);
            var action = new TestableInitializeFieldsFromConstructorCodeAction(new InitializeFieldsFromConstructorCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Span = new TextSpan(TestCase.IndexOf("tparam", StringComparison.Ordinal), 3),
                ParameterName = "tparam"
            });

            var newSolution = await action.Execute(CancellationToken.None);
            var newDocument = newSolution.GetDocument(document.Id);

            Assert.Equal("Initialize field '_tparam'", action.Title);
            Assert.Equal(ExpectedSource, (await newDocument.GetTextAsync()).ToString());

            newDocument = await Formatter.FormatAsync(newDocument);
            Assert.Equal(FormattedExpectedSource, (await newDocument.GetTextAsync()).ToString());
        }

        [Fact]
        public async Task Should_add_initialialization_on_parameter_type()
        {
            const string TestCase = @"namespace TestSuite {
    public class Foo {
        public Foo(Foo tparam) 
        {
        }
    }
}";
            const string ExpectedSource = @"namespace TestSuite {
    public class Foo {
private readonly Foo _tparam;
        public Foo(Foo tparam) 
        {
_tparam = tparam;
        }
    }
}";
            const string FormattedExpectedSource = @"namespace TestSuite
{
    public class Foo
    {
        private readonly Foo _tparam;
        public Foo(Foo tparam)
        {
            _tparam = tparam;
        }
    }
}";
            var document = CreateDocument(TestCase);
            var action = new TestableInitializeFieldsFromConstructorCodeAction(new InitializeFieldsFromConstructorCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Span = new TextSpan(TestCase.IndexOf("Foo tp", StringComparison.Ordinal) + 1, 1)
            });

            var newSolution = await action.Execute(CancellationToken.None);
            var newDocument = newSolution.GetDocument(document.Id);

            Assert.Equal(ExpectedSource, (await newDocument.GetTextAsync()).ToString());

            newDocument = await Formatter.FormatAsync(newDocument);
            Assert.Equal(FormattedExpectedSource, (await newDocument.GetTextAsync()).ToString());
        }

        [Fact]
        public async Task Should_add_initialialization_using_existing_field()
        {
            const string TestCase = @"namespace TestSuite {
    public class Foo {
        private readonly string _tparam;
        public Foo(string tparam) 
        {
        }
    }
}";
            const string ExpectedSource = @"namespace TestSuite {
    public class Foo {
        private readonly string _tparam;
        public Foo(string tparam) 
        {
_tparam = tparam;
        }
    }
}";
            var document = CreateDocument(TestCase);
            var action = new TestableInitializeFieldsFromConstructorCodeAction(new InitializeFieldsFromConstructorCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Span = new TextSpan(TestCase.IndexOf("tparam)", StringComparison.Ordinal), 3)
            });

            var newSolution = await action.Execute(CancellationToken.None);
            var newDocument = newSolution.GetDocument(document.Id);

            Assert.Equal(ExpectedSource, (await newDocument.GetTextAsync()).ToString());
        }

        [Fact]
        public async Task Should_add_initialialization_with_parameter_order()
        {
            const string TestCase = @"namespace TestSuite {
    public class Foo {
        private readonly string _tparam1;
        private readonly string _tparam2;
        public Foo(string tparam1, string tparam2) 
        {
            _tparam1 = tparam1;
        }
    }
}";
            const string ExpectedSource = @"namespace TestSuite {
    public class Foo {
        private readonly string _tparam1;
        private readonly string _tparam2;
        public Foo(string tparam1, string tparam2) 
        {
            _tparam1 = tparam1;
_tparam2 = tparam2;
        }
    }
}";
            var document = CreateDocument(TestCase);
            var action = new TestableInitializeFieldsFromConstructorCodeAction(new InitializeFieldsFromConstructorCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Span = new TextSpan(TestCase.IndexOf("tparam2)", StringComparison.Ordinal), 3)
            });

            var newSolution = await action.Execute(CancellationToken.None);
            var newDocument = newSolution.GetDocument(document.Id);

            Assert.Equal(ExpectedSource, (await newDocument.GetTextAsync()).ToString());
        }

        [Fact]
        public async Task Should_add_initialialization_with_field_order()
        {
            const string TestCase = @"namespace TestSuite {
    public class Foo {
        private readonly string _tparam1;
        public Foo(string tparam1, string tparam2, string tparam3) 
        {
            _tparam1 = tparam1;
        }
    }
}";
            const string ExpectedSource = @"namespace TestSuite {
    public class Foo {
        private readonly string _tparam1;
private readonly string _tparam3;
        public Foo(string tparam1, string tparam2, string tparam3) 
        {
            _tparam1 = tparam1;
_tparam3 = tparam3;
        }
    }
}";
            var document = CreateDocument(TestCase);
            var action = new TestableInitializeFieldsFromConstructorCodeAction(new InitializeFieldsFromConstructorCodeActionContext
            {
                Solution = document.Project.Solution,
                DocumentId = document.Id,
                Span = new TextSpan(TestCase.IndexOf("tparam3)", StringComparison.Ordinal), 3)
            });

            var newSolution = await action.Execute(CancellationToken.None);
            var newDocument = newSolution.GetDocument(document.Id);

            Assert.Equal(ExpectedSource, (await newDocument.GetTextAsync()).ToString());
        }

    }
}