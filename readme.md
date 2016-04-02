Roslyn CodeRefactoring
================

Roslyn refactorings and code actions

A refactoring (`Ctrl + .`) which allows you to:

1.   when on Class declaration:
    *   extract a class into its own file using current folder
    *   extract a class into its own file using namespace based folder
2.   when on Namespace declaration:
    *   fix namespace to be folder based
    *   rename file to any class declaration using current folder
    *   rename file to any class declaration using namespace based folder
3.   when on constructor parameter:
    *   initialize a `private readonly` field named `_{parameterName}` with the following rules:
        -   field existance is detected so no duplicated fields are generated
        -   fields are generated with the same order of parameters
        -   fields assignments are generated with the same order of parameters
