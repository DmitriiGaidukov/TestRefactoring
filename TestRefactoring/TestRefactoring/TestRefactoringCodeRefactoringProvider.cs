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
                .Where(p => GetProjectFoldersAreSiblings(currProject.FilePath, p.FilePath)).ToArray();

            var integrationTestProject = sameFolderProjects
                .SingleOrDefault(p => p.Name.EndsWith(".Integration.NUnit"));

            var unitTestProject = sameFolderProjects
                .SingleOrDefault(p => !p.Name.EndsWith(".Integration.NUnit") 
                    && p.Name.EndsWith(".NUnit"));


            // if integration tests project exists, then add the corresponding refactoring option
            if (integrationTestProject != null)
            {
                // semantic
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                var createTestFixtureCodeAction = new CreateTestFixtureCodeAction(
                    integrationTestProject, 
                    context.Document, 
                    typeDecl, 
                    semanticModel,
                    $"Create {typeDecl.Identifier.Text}Tests.cs integration test for this class in the {integrationTestProject.Name} project", 
                    CodeGenerator.GetIntegrationTestCode);

                // Register this code action.
                context.RegisterRefactoring(createTestFixtureCodeAction);
            }

            // if unit tests project exists, then add the corresponding refactoring option
            if (unitTestProject != null)
            {
                // semantic
                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

                var createTestFixtureCodeAction = new CreateTestFixtureCodeAction(
                    unitTestProject,
                    context.Document,
                    typeDecl,
                    semanticModel,
                    $"Create {typeDecl.Identifier.Text}Tests.cs unit test for this class in the {unitTestProject.Name} project",
                    CodeGenerator.GetUnitTestCode);

                // Register this code action.
                context.RegisterRefactoring(createTestFixtureCodeAction);
            }

        }

        /// <summary>
        /// checks wether the projects' folders are in the same folder
        /// </summary>
        /// <param name="csprojPath"></param>
        /// <returns></returns>
        private bool GetProjectFoldersAreSiblings(string csprojPath1, string csprojPath2)
        {
            if (csprojPath1 == null || csprojPath2 == null)
                return false;

            var proj1Directory = new DirectoryInfo(csprojPath1).Parent?.Parent?.FullName;
            var proj2Directory = new DirectoryInfo(csprojPath2).Parent?.Parent?.FullName;

            if (proj1Directory == null || proj2Directory == null)
                return false;

            return proj1Directory == proj2Directory;
        }
    }
}
