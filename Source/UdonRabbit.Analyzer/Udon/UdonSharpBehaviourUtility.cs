﻿using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UdonRabbit.Analyzer.Udon
{
    public static class UdonSharpBehaviourUtility
    {
        public static bool ShouldAnalyzeSyntax(SemanticModel semanticModel, SyntaxNode node)
        {
            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return false;

            var declSymbol = (INamedTypeSymbol) semanticModel.GetDeclaredSymbol(classDecl);
            return declSymbol.BaseType.Equals(semanticModel.Compilation.GetTypeByMetadataName(UdonConstants.UdonSharpBehaviourFullName), SymbolEqualityComparer.Default);
        }

        public static bool ShouldAnalyzeSyntaxByClass(SemanticModel semanticModel, ClassDeclarationSyntax @class)
        {
            var decl = (INamedTypeSymbol) semanticModel.GetDeclaredSymbol(@class);
            return decl.BaseType.Equals(semanticModel.Compilation.GetTypeByMetadataName(UdonConstants.UdonSharpBehaviourFullName), SymbolEqualityComparer.Default);
        }

        public static bool IsUdonSharpDefinedTypes(SemanticModel model, ITypeSymbol symbol)
        {
            return
                symbol.Equals(model.Compilation.GetTypeByMetadataName(UdonConstants.UdonSharpBehaviourFullName), SymbolEqualityComparer.Default) ||
                symbol.Equals(model.Compilation.GetTypeByMetadataName(UdonConstants.UdonSharpBehaviourSyncModeFullName), SymbolEqualityComparer.Default) ||
                symbol.Equals(model.Compilation.GetTypeByMetadataName(UdonConstants.UdonSharpSyncModeFullName), SymbolEqualityComparer.Default);
        }

        public static bool IsUdonSharpDefinedTypes(SemanticModel model, ITypeSymbol symbol, TypeKind kind)
        {
            return kind switch
            {
                TypeKind.Array when symbol is IArrayTypeSymbol a => IsUdonSharpDefinedTypes(model, a.ElementType),
                TypeKind.Class => IsUdonSharpDefinedTypes(model, symbol),
                _ => throw new NotSupportedException(kind.ToString())
            };
        }

        [Obsolete]
        public static bool IsUserDefinedTypes(SemanticModel model, ITypeSymbol symbol)
        {
            return IsUserDefinedTypesInternal(model, symbol);
        }

        private static bool IsUserDefinedTypesInternal(SemanticModel model, ITypeSymbol symbol)
        {
            if (symbol.BaseType == null)
                return false;
            return symbol.BaseType.Equals(model.Compilation.GetTypeByMetadataName(UdonConstants.UdonSharpBehaviourFullName), SymbolEqualityComparer.Default);
        }

        public static bool IsUserDefinedTypes(SemanticModel model, ITypeSymbol symbol, TypeKind kind)
        {
            return kind switch
            {
                TypeKind.Array when symbol is IArrayTypeSymbol a => IsUserDefinedTypes(model, a.ElementType, a.ElementType.TypeKind),
                TypeKind.Class => IsUserDefinedTypesInternal(model, symbol), // UdonSharp currently support user-defined types only.
                _ => false
            };
        }

        public static bool HasUdonSyncedAttribute(SemanticModel model, SyntaxList<AttributeListSyntax> attributes)
        {
            var attrs = attributes.SelectMany(w => w.Attributes)
                                  .Select(w => model.GetSymbolInfo(w))
                                  .Select(w => w.Symbol)
                                  .OfType<IMethodSymbol>();

            return attrs.Any(w => PrettyTypeName(w) == "UdonSharp.UdonSyncedAttribute");
        }

        public static bool IsUdonSyncMode(SemanticModel model, SyntaxList<AttributeListSyntax> attributes, string mode)
        {
            var attr = attributes.SelectMany(w => w.Attributes)
                                 .Select(w => (Attribute: w, SymbolInfo: model.GetSymbolInfo(w)))
                                 .Where(w => w.SymbolInfo.Symbol is IMethodSymbol)
                                 .FirstOrDefault(w => PrettyTypeName(w.SymbolInfo.Symbol) == "UdonSharp.UdonSyncedAttribute");

            if (attr.Equals(default))
                return false;

            if (attr.Attribute.ArgumentList == null)
                return mode == "None";

            return attr.Attribute.ArgumentList.Arguments.Select(w => w.Expression)
                       .Any(w =>
                       {
                           var info = model.GetSymbolInfo(w);
                           if (info.Symbol is not IFieldSymbol field)
                               return mode == "None";
                           return field.Type.ToDisplayString() == "UdonSharp.UdonSyncMode" && field.Name == mode;
                       });
        }

        public static bool HasUdonBehaviourSyncModeAttribute(SemanticModel model, SyntaxNode node)
        {
            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return false;

            var attrs = classDecl.AttributeLists.SelectMany(w => w.Attributes)
                                 .Select(w => model.GetSymbolInfo(w))
                                 .Select(w => w.Symbol)
                                 .OfType<IMethodSymbol>();

            return attrs.Any(w => PrettyTypeName(w) == "UdonSharp.UdonBehaviourSyncModeAttribute");
        }

        public static bool IsUdonBehaviourSyncMode(SemanticModel model, SyntaxNode node, string mode)
        {
            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl == null)
                return false;

            var attr = classDecl.AttributeLists.SelectMany(w => w.Attributes)
                                .Select(w => (Attribute: w, SymbolInfo: model.GetSymbolInfo(w)))
                                .Where(w => w.SymbolInfo.Symbol is IMethodSymbol)
                                .FirstOrDefault(w => PrettyTypeName(w.SymbolInfo.Symbol) == "UdonSharp.UdonBehaviourSyncModeAttribute");

            if (attr.Equals(default))
                return false;

            if (attr.Attribute.ArgumentList == null)
                return mode == "Any";

            return attr.Attribute.ArgumentList.Arguments.Select(w => w.Expression)
                       .Any(w =>
                       {
                           var info = model.GetSymbolInfo(w);
                           if (info.Symbol is not IFieldSymbol field)
                               return mode == "Any";
                           return field.Type.ToDisplayString() == "UdonSharp.BehaviourSyncMode" && field.Name == mode;
                       });
        }

        public static string PrettyTypeName(ISymbol symbol)
        {
            return symbol switch
            {
                IArrayTypeSymbol a => $"{a.ToDisplayString()}",
                IMethodSymbol m => $"{m.ContainingType.ToDisplayString()}",
                INamedTypeSymbol t => $"{t.ToDisplayString()}",
                _ => string.Empty
            };
        }
    }
}