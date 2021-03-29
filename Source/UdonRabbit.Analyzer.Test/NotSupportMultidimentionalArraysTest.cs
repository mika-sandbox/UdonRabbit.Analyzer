﻿using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

using UdonRabbit.Analyzer.Test.Infrastructure;

using Xunit;

namespace UdonRabbit.Analyzer.Test
{
    public class NotSupportMultidimentionalArraysTest : DiagnosticVerifier<NotSupportMultidimentionalArrays>
    {
        [Fact]
        public async Task MonoBehaviourMultidimentionalArrayDeclarationHasNoDiagnosticsReport()
        {
            const string source = @"
using UnityEngine;

namespace UdonRabbit
{
    public class TestBehaviour : MonoBehaviour
    {
        private int[,] array1;

        private void Start()
        {
            array1 = new int[2, 4];
        }
    }
}
";

            await VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task MonoBehaviourMultidimentionalArrayVariableHasNoDiagnosticsReport()
        {
            const string source = @"
using UnityEngine;

namespace UdonRabbit
{
    public class TestBehaviour : MonoBehaviour
    {
        private void Start()
        {
            var array1 = new int[2, 4];
        }
    }
}
";

            await VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task UdonSharpBehaviourMultidimentionalArrayClassVariableHasDiagnosticsReport()
        {
            var diagnostic = ExpectDiagnostic(NotSupportMultidimentionalArrays.ComponentId)
                             .WithLocation(8, 24)
                             .WithSeverity(DiagnosticSeverity.Error);

            const string source = @"
using UdonSharp;

namespace UdonRabbit
{
    public class TestBehaviour : UdonSharpBehaviour
    {
        private int[,] array1;

        private void Start()
        {
            array1 = new int[2, 4];
        }
    }
}
";

            await VerifyAnalyzerAsync(source, diagnostic);
        }

        [Fact]
        public async Task UdonSharpBehaviourMultidimentionalArrayLocalVariableHasDiagnosticsReport()
        {
            var diagnostic = ExpectDiagnostic(NotSupportMultidimentionalArrays.ComponentId)
                             .WithLocation(10, 17)
                             .WithSeverity(DiagnosticSeverity.Error);

            const string source = @"
using UdonSharp;

namespace UdonRabbit
{
    public class TestBehaviour : UdonSharpBehaviour
    {
        private void Start()
        {
            var array1 = new int[2, 4];
        }
    }
}
";

            await VerifyAnalyzerAsync(source, diagnostic);
        }

        [Fact]
        public async Task UdonSharpBehaviourMultidimentionalMethodParameterVariableHasDiagnosticsReport()
        {
            var diagnostic = ExpectDiagnostic(NotSupportMultidimentionalArrays.ComponentId)
                             .WithLocation(8, 33)
                             .WithSeverity(DiagnosticSeverity.Error);

            const string source = @"
using UdonSharp;

namespace UdonRabbit
{
    public class TestBehaviour : UdonSharpBehaviour
    {
        private void TestMethod(int[,] array) {}
    }
}
";

            await VerifyAnalyzerAsync(source, diagnostic);
        }

        [Fact]
        public async Task UdonSharpBehaviourSingleArrayDeclarationHasNoDiagnosticsReport()
        {
            const string source = @"
using UdonSharp;

namespace UdonRabbit
{
    public class TestBehaviour : UdonSharpBehaviour
    {
        private int[] array1;

        private void Start()
        {
            array1 = new int[2];
        }
    }
}
";

            await VerifyAnalyzerAsync(source);
        }
    }
}