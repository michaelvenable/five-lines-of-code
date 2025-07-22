using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace FiveLinesOfCode
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallOrPassCodeFixProvider)), Shared]
    public class CallOrPassCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CallOrPass.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var block = root.FindNode(diagnosticSpan) as BlockSyntax;

            if (block == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Extract to method",
                    createChangedDocument: c => ExtractMethodAsync(context.Document, block, c),
                    equivalenceKey: "Extract to method"),
                diagnostic);
        }
        
        private async Task<Document> ExtractMethodAsync(Document document, BlockSyntax block, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var parentMethod = block.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (parentMethod == null)
                return document;

            // Select statements to extract – here we just extract all the block's statements
            var statementsToExtract = block.Statements;

            // Create a new method name
            var newMethodName = "ExtractedMethod";

            // Create the new method declaration
            var newMethod = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    newMethodName)
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithBody(SyntaxFactory.Block(statementsToExtract))
                .NormalizeWhitespace();

            // Create a method invocation
            var invocation = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(newMethodName)))
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

            // Replace the old statements with the method call
            var newBlock = block.WithStatements(SyntaxFactory.List(new StatementSyntax[] { invocation }));

            editor.ReplaceNode(block, newBlock);
            editor.InsertAfter(parentMethod, newMethod);

            return editor.GetChangedDocument();
        }
    }
}
