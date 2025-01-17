﻿/*-------------------------------------------------------------------------------------------
 * Copyright (c) esnya. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *------------------------------------------------------------------------------------------*/

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using UdonRabbit.Analyzer.Udon;

namespace UdonRabbit.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodIsNotExposedToUdon : DiagnosticAnalyzer
    {
        public const string ComponentId = "URA0001";
        private const string Category = UdonConstants.UdonCategory;
        private const string HelpLinkUri = "https://github.com/esnya/UdonRabbit.Analyzer/blob/master/docs/analyzers/URA0001.md";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.URA0001Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.URA0001MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.URA0001Description), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor RuleSet = new(ComponentId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true, Description, HelpLinkUri);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleSet);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax) context.Node;
            if (!UdonSharpBehaviourUtility.ShouldAnalyzeSyntax(context.SemanticModel, invocation))
                return;

            if (!UdonAssemblyLoader.IsAssemblyLoaded)
                UdonAssemblyLoader.LoadUdonAssemblies(context.Compilation.ExternalReferences.ToList());

            if (UdonSymbols.Instance == null)
                UdonSymbols.Initialize();

            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation);
            if (methodSymbol.Symbol is not IMethodSymbol method)
                return;

            // Workaround for Enum Methods
            if (invocation.Expression is MemberAccessExpressionSyntax expr)
            {
                var receiver = expr.Expression;
                var symbolInfo = context.SemanticModel.GetSymbolInfo(receiver);

                // direct access to enum
                if (symbolInfo.Symbol?.ContainingType != null && SymbolEqualityComparer.Default.Equals(symbolInfo.Symbol.ContainingType.BaseType, context.SemanticModel.Compilation.GetTypeByMetadataName("System.Enum")))
                {
                    // receiver is enum properties
                    if (UdonSymbols.Instance?.FindUdonMethodName(context.SemanticModel, method, symbolInfo.Symbol.ContainingType) == false)
                        UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, invocation, method.Name);
                    return;
                }

                // field access to enum
                var typeInfo = context.SemanticModel.GetTypeInfo(receiver);
                if (typeInfo.Type.BaseType?.Equals(context.SemanticModel.Compilation.GetTypeByMetadataName("System.Enum"), SymbolEqualityComparer.Default) == true)
                {
                    // receiver is enum properties
                    if (UdonSymbols.Instance?.FindUdonMethodName(context.SemanticModel, method, typeInfo.Type) == false)
                        UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, invocation, method.Name);
                    return;
                }
            }

            if (UdonSymbols.Instance?.FindUdonMethodName(context.SemanticModel, method) == false)
                UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, invocation, method.Name);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var expression = (ObjectCreationExpressionSyntax) context.Node;
            if (!UdonSharpBehaviourUtility.ShouldAnalyzeSyntax(context.SemanticModel, expression))
                return;

            if (expression.NewKeyword.Equals(default))
                return;

            if (!UdonAssemblyLoader.IsAssemblyLoaded)
                UdonAssemblyLoader.LoadUdonAssemblies(context.Compilation.ExternalReferences.ToList());

            if (UdonSymbols.Instance == null)
                UdonSymbols.Initialize();

            var methodSymbol = context.SemanticModel.GetSymbolInfo(expression);
            if (methodSymbol.Symbol is not IMethodSymbol method)
                return;

            if (UdonSymbols.Instance?.FindUdonMethodName(context.SemanticModel, method) == false)
                UdonSharpBehaviourUtility.ReportDiagnosticsIfValid(context, RuleSet, expression, method.Name);
        }
    }
}