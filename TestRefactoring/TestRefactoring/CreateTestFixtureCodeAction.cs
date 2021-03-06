using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestRefactoring.Extensions;

namespace TestRefactoring
{
        /// <summary>
        /// CodeAction for creating a TestFixture for a specified class
        /// </summary>
        public class CreateTestFixtureCodeAction : CodeActionWithOptions
        {
            private readonly Project _testProject;
            private readonly Document _document;
            private readonly TypeDeclarationSyntax _typeDecl;
            private readonly SemanticModel _semanticModel;
            private readonly Func<string, string, string, string, string> _codeGenerationFunction;
            public override string Title { get; }


            public CreateTestFixtureCodeAction(Project testProject, Document document, TypeDeclarationSyntax typeDecl, SemanticModel semanticModel, string title, Func<string, string, string, string, string> codeGenerationFunction)
            {
                _testProject = testProject;
                _document = document;
                _typeDecl = typeDecl;
                _semanticModel = semanticModel;
                Title = title;
                _codeGenerationFunction = codeGenerationFunction;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                return new Object();
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                var addedDocument = CreateTextFixtureDocument(cancellationToken);

                IEnumerable<CodeActionOperation> operations = new CodeActionOperation[]
                {
                    new ApplyChangesOperation(addedDocument.Project.Solution),
                    new OpenDocumentOperation(addedDocument.Id, true)
                };

                return Task.FromResult(operations);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                var addedDocument = CreateTextFixtureDocument(cancellationToken);

                IEnumerable<CodeActionOperation> operations = new CodeActionOperation[]
                {
                    new ApplyChangesOperation(addedDocument.Project.Solution),
                };

                return await Task.FromResult(operations).ConfigureAwait(false);
            }

  
            private Document CreateTextFixtureDocument(CancellationToken cancellationToken)
            {
                // Get the symbol representing the type for which a test is being created
                var typeSymbol = _semanticModel.GetDeclaredSymbol(_typeDecl, cancellationToken);


                // get the name and namepsace of current type 
                var typeName = _typeDecl.Identifier.Text;
                var typeNamespace = typeSymbol.GetContainingNamespace();

                // define name and namepsace for testfixture type
                var testTypeName = typeName + "Tests";
                var testTypeNamespace = _testProject.Name + "." + typeNamespace.Replace($"{_document.Project.Name}.", "");

                // generate code with testfixture type declaration
                var code = _codeGenerationFunction(typeName, typeNamespace, testTypeName, testTypeNamespace);

                // define filename and folders for testfixture file
                var fileName = $"{typeName}Tests.cs";
                var fileFolders = typeNamespace.Replace($"{_document.Project.Name}.", "").Split('.');

                // add textfixture declaration file to the test project
                var addedDocument = _testProject.AddDocument(fileName, code, folders: fileFolders);

                return addedDocument;
            }
    }
}