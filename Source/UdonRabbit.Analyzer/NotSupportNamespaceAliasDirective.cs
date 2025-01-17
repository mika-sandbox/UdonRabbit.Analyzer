﻿using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using UdonRabbit.Analyzer.Udon;

namespace UdonRabbit.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotSupportNamespaceAliasDirective : DiagnosticAnalyzer
    {
        public const string ComponentId = "URA0031";
        private const string Category = UdonConstants.UdonSharpCategory;
        private const string HelpLinkUri = "https://github.com/esnya/UdonRabbit.Analyzer/blob/master/docs/analyzers/URA0031.md";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.URA0031Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.URA0031MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.URA0031Description), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor RuleSet = new(ComponentId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleSet);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        }

        private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            var directive = (UsingDirectiveSyntax) context.Node;
            var tree = context.Compilation.SyntaxTrees
                              .FirstOrDefault(w => w.GetCompilationUnitRoot().DescendantNodes().Contains(directive));

            var declaration = tree?.GetCompilationUnitRoot()
                                  .DescendantNodes()
                                  .Where(w => w is MemberDeclarationSyntax)
                                  .OfType<ClassDeclarationSyntax>()
                                  .First();

            if (declaration == null)
                return;

            if (!UdonSharpBehaviourUtility.ShouldAnalyzeSyntaxByClass(context.Compilation.GetSemanticModel(tree), declaration))
                return;

            if (directive.Alias != default)
                UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, directive);
        }
    }
}