using System;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestRefactoring.Extensions;

namespace TestRefactoring
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(TestRefactoringCodeRefactoringProvider)), Shared]
    internal class TestRefactoringCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly CodeGenerator _codeGenerator = new CodeGenerator();

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // TODO: Replace the following code with your own analysis, generating a CodeAction for each refactoring to offer

            // syntax 
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a type declaration node.
            if (!(node is TypeDeclarationSyntax typeDecl))
            {
                return;
            }

            var currProject = context.Document.Project;
            var solution = currProject.Solution;

            // try to find Integration.NUnit project in the same folder 
            var sameFolderProjects = solution.Projects
                .Where(p => GetProjectFolderRootPath(currProject.FilePath) == GetProjectFolderRootPath(p.FilePath)).ToArray();

            var integrationTestProject = sameFolderProjects
                .SingleOrDefault(p => p.Name.EndsWith(".Integration.NUnit"));

            var unitTestProject = sameFolderProjects
                .SingleOrDefault(p => !p.Name.EndsWith(".Integration.NUnit")
                    && p.Name.EndsWith(".NUnit"));


            // if integration tests project exists, then add the corresponding refactoring option
            if (integrationTestProject != null)
            {
                // For any type declaration node, create a code action to reverse the identifier text.
                var action = CodeAction.Create(
                    $"Create {typeDecl.Identifier.Text}Tests.cs integration test for this class in the {integrationTestProject.Name} project", 
                    c => CreateTestFixtureAsync(context.Document, typeDecl, c, integrationTestProject, CodeGenerator.GetIntegrationTestCode));

                // Register this code action.
                context.RegisterRefactoring(action);
            }

            // if unit tests project exists, then add the corresponding refactoring option
            if (unitTestProject != null)
            {
                // For any type declaration node, create a code action to reverse the identifier text.
                var action = CodeAction.Create(
                    $"Create {typeDecl.Identifier.Text}Tests.cs unit test for this class in the {unitTestProject.Name} project",
                    c => CreateTestFixtureAsync(context.Document, typeDecl, c, unitTestProject, CodeGenerator.GetUnitTestCode));

                // Register this code action.
                context.RegisterRefactoring(action);
            }
            
        }

        /// <summary>
        /// gets the path to the folder containing the specified project's folder
        /// </summary>
        /// <param name="csprojPath"></param>
        /// <returns></returns>
        private string GetProjectFolderRootPath(string csprojPath)
        {
            return new DirectoryInfo(csprojPath).Parent?.Parent?.FullName;
        }


        private async Task<Solution> CreateTestFixtureAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken, Project testProject, Func<string, string, string, string, string> codeGenerationFunction)
        {
            // Get the symbol representing the type to be renamed.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);

            
            // get the name and namepsace of current type 
            var typeName = typeDecl.Identifier.Text;
            var typeNamespace = typeSymbol.GetContainingNamespace();

            // define name and namepsace for testfixture type
            var testTypeName = typeName + "Tests";
            var testTypeNamespace = testProject.Name + "." + typeNamespace.Replace($"{document.Project.Name}.", "");
                
            // generate code with testfixture type declaration
            var code = codeGenerationFunction(typeName, typeNamespace, testTypeName, testTypeNamespace);

            // define filename and folders for testfixture file
            var fileName = $"{typeName}Tests.cs";
            var fileFolders = typeNamespace.Replace($"{document.Project.Name}.", "").Split('.');

            // add textfixture declaration file to the integration test project
            testProject = testProject.AddDocument(fileName, code, folders: fileFolders).Project;

            // obtain the new solution
            var newSolution = testProject.Solution;


            // Return the new solution with the testfixture file
            return newSolution;
        }
    }
}
