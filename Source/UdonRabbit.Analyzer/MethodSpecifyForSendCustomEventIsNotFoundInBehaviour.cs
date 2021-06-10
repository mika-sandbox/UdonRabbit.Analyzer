using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using UdonRabbit.Analyzer.Extensions;
using UdonRabbit.Analyzer.Udon;

namespace UdonRabbit.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodSpecifyForSendCustomEventIsNotFoundInBehaviour : DiagnosticAnalyzer
    {
        public const string ComponentId = "URA0045";
        private const string Category = UdonConstants.UdonCategory;
        private const string HelpLinkUri = "https://github.com/mika-f/UdonRabbit.Analyzer/blob/master/docs/analyzers/URA0045.md";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.URA0045Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.URA0045MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.URA0045Description), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor RuleSet = new(ComponentId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true, Description, HelpLinkUri);

        private static readonly HashSet<(string, int)> ScannedMethodLists = new()
        {
            ("SendCustomEvent", 0),
            ("SendCustomNetworkEvent", 1),
            ("SendCustomEventDelayedSeconds", 0),
            ("SendCustomEventDelayedFrames", 0)
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleSet);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax) context.Node;
            if (!UdonSharpBehaviourUtility.ShouldAnalyzeSyntax(context.SemanticModel, invocation))
                return;

            var symbol = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbol.Symbol is not IMethodSymbol method)
                return;

            if (ScannedMethodLists.All(w => w.Item1 != method.Name))
                return;

            // has receiver
            if (invocation.Expression is MemberAccessExpressionSyntax expression)
            {
                var receiver = expression.Expression;
                var t = context.SemanticModel.GetTypeInfo(receiver);
                if (!UdonSharpBehaviourUtility.IsUserDefinedTypes(context.SemanticModel, t.Type, t.Type.TypeKind))
                    return;

                var i = ScannedMethodLists.First(v => v.Item1 == method.Name).Item2;
                var arg = invocation.ArgumentList.Arguments.ElementAtOrDefault(i);
                if (arg == null)
                    return;

                var name = arg.Expression.ParseValue();

                var m = t.Type.GetMembers().Where(w => w is IMethodSymbol).FirstOrDefault(w => w.Name == name);
                if (m == null)
                    UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, invocation, name, method.Name, t.Type.Name);
            }
            else
            {
                var i = ScannedMethodLists.First(v => v.Item1 == method.Name).Item2;
                var arg = invocation.ArgumentList.Arguments.ElementAtOrDefault(i);
                if (arg == null)
                    return;

                var name = arg.Expression.ParseValue();

                var m = context.SemanticModel.LookupSymbols(invocation.SpanStart).Where(w => w is IMethodSymbol).FirstOrDefault(w => w.Name == name);
                if (m == null)
                {
                    var cls = invocation.FirstAncestorOrSelf<ClassDeclarationSyntax>();
                    var sym = context.SemanticModel.GetDeclaredSymbol(cls);
                    UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, invocation, name, method.Name, sym.Name);
                }
            }
        }
    }
}