using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestRefactoring
{
    public class TestActionOperation : CodeActionWithOptions
    {
        private readonly Project _testProject;
        private readonly Document _document;
        private readonly TypeDeclarationSyntax _typeDecl;
        public override string Title => "test";

        public TestActionOperation(Project testProject, Document document, TypeDeclarationSyntax typeDecl)
        {
            _testProject = testProject;
            _document = document;
            _typeDecl = typeDecl;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
           

            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            // todo: perfom main work here. then return action for the opening of document

           

            throw new NotImplementedException();
        }
    }
}