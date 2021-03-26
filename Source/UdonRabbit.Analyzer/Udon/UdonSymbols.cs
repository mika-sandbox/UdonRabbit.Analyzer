﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;

namespace UdonRabbit.Analyzer.Udon
{
    public class UdonSymbols
    {
        private static readonly object LockObj = new();
        private readonly Dictionary<string, string> _builtinEvents;
        private readonly Dictionary<Type, Type> _inheritedTypeMap;
        private readonly HashSet<string> _nodeDefinitions;

        public static UdonSymbols Instance { get; private set; }

        private UdonSymbols(UdonEditorManager manager)
        {
            _nodeDefinitions = new HashSet<string>(manager?.GetUdonNodeDefinitions() ?? Array.Empty<string>());
            _builtinEvents = manager?.GetBuiltinEvents() ?? new Dictionary<string, string>();
            _inheritedTypeMap = manager?.GetVrcInheritedTypeMaps() ?? new Dictionary<Type, Type>();
        }

        public static void Initialize(Compilation compilation)
        {
            lock (LockObj)
            {
                if (Instance != null)
                    return;

                var manager = LoadSdkAssemblies(compilation);
                Instance = new UdonSymbols(manager);
            }
        }

        private static UdonEditorManager LoadSdkAssemblies(Compilation compilation)
        {
            var reference = compilation.ExternalReferences.FirstOrDefault(w => w.Display.Contains("VRC.Udon.Common.dll"));
            if (reference == null)
                return null;

            static string FindUnityAssetsDirectory(string path)
            {
                return path.Substring(0, path.IndexOf("Assets", StringComparison.InvariantCulture));
            }

            var assemblies = Path.GetFullPath(Path.Combine(FindUnityAssetsDirectory(reference.Display), "Library", "ScriptAssemblies"));
            var editor = Path.Combine(assemblies, "VRC.Udon.Editor.dll");
            if (!File.Exists(editor))
                return null; // Invalid Path;

            Assembly ResolveDynamicLoadingAssemblies(object _, ResolveEventArgs args)
            {
                var dll = $"{args.Name.Split(',').First()}.dll";
                if (compilation.ExternalReferences.Any(w => w.Display.Contains(dll)))
                    return Assembly.LoadFrom(compilation.ExternalReferences.First(w => w.Display.Contains(dll)).Display);
                return Assembly.LoadFrom(Path.GetFullPath(Path.Combine(assemblies, dll)));
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDynamicLoadingAssemblies;
            var assembly = Assembly.LoadFrom(editor);

            var manager = new UdonEditorManager(assembly);
            return manager.HasInstance ? manager : null;
        }

        public bool FindUdonMethodName(SemanticModel model, IMethodSymbol symbol)
        {
            var receiver = symbol.ReceiverType;
            if (receiver.BaseType.Equals(model.Compilation.GetTypeByMetadataName("UdonSharp.UdonSharpBehaviour"), SymbolEqualityComparer.Default))
                return true; // User-Defined Method, Skip

            return false;
        }

        public bool FindUdonVariableName(IFieldSymbol symbol)
        {
            return false;
        }
    }
}