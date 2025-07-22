using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FiveLinesOfCode
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CallOrPass : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CallOrPass";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Five Lines of Code";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethodBody, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethodBody(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            var semanticModel = context.SemanticModel;

            var memberAccess = methodDeclaration
                .DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Select(ma => ma.Expression)
                .OfType<IdentifierNameSyntax>()
                .ToList();

            var arguments = methodDeclaration
                .DescendantNodes()
                .OfType<ArgumentSyntax>()
                .Select(arg => arg.Expression)
                .OfType<IdentifierNameSyntax>()
                .ToList();

            foreach (var access in memberAccess)
            {
                foreach (var arg in arguments)
                {
                    if (access.Identifier.Text == arg.Identifier.Text)
                    {
                        var diagnostic = Diagnostic.Create(Rule, access.GetLocation(), access.Identifier.Text);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
