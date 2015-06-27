﻿using System;
using System.Linq;
using CSharpWriter.CodeTranslation;
using Xunit;

namespace VBScriptTranslator.UnitTests.CSharpWriter.CodeTranslation.IntegrationTests
{
    public class EndToEndConstTranslationTests
    {
        // No Name Redefined: CONST then REDIM (will be an illegal assignment at runtime, not a compile / translation error)
        //  ' Illegal assignment (runtime error)
        //  Const cn998 = 1
        //  ReDim cn998(12)

        [Fact]
        public void RepeatedConstNameInSameScopeResultsInNameRedefinedError()
        {
            var source = @"
                CONST a = 1
                CONST a = 2
            ";
            Assert.Throws<NameRedefinedException>(() =>
                WithoutScaffoldingTranslator.GetTranslatedStatements(source, WithoutScaffoldingTranslator.DefaultConsoleExternalDependencies)
            );
        }

        [Fact]
        public void ConstThenDimForSameNameInSameScopeResultsInNameRedefinedError()
        {
            var source = @"
                CONST a = 1
                DIM a
            ";
            Assert.Throws<NameRedefinedException>(() =>
                WithoutScaffoldingTranslator.GetTranslatedStatements(source, WithoutScaffoldingTranslator.DefaultConsoleExternalDependencies)
            );
        }

        [Fact]
        public void DimThenConstForSameNameInSameScopeResultsInNameRedefinedError()
        {
            var source = @"
                DIM a
                CONST a = 1
            ";
            Assert.Throws<NameRedefinedException>(() =>
                WithoutScaffoldingTranslator.GetTranslatedStatements(source, WithoutScaffoldingTranslator.DefaultConsoleExternalDependencies)
            );
        }

        /// <summary>
        /// If there is a CONST then REDIM for the same variable then there will be a runtime error when the readonly value is to be altered by the
        /// REDIM, but there will not be a name-redefined compile error (REDIM is only treated as an explicit variable declaration if there is no
        /// variable already declared for it to target). However, a REDIM and THEN a CONST for the same variable IS a name-redefined compile
        /// error since the REDIM will have been treated as an explicit variable declaration (since it came first).
        /// </summary>
        [Fact]
        public void ReDimBeforeConstForSameNameInSameScopeResultsInNameRedefinedError()
        {
            var source = @"
                ReDim a(1)
                CONST a = 1
            ";
            Assert.Throws<NameRedefinedException>(() =>
                WithoutScaffoldingTranslator.GetTranslatedStatements(source, WithoutScaffoldingTranslator.DefaultConsoleExternalDependencies)
            );
        }

        /// <summary>
        /// If a REDIM follows a CONST then there is no name-refined problem (the REDIM accepts the CONST as the explicit variable declaration and
        /// so doesn't try to create one of its own) but it will result in a runtime illegal assignment error. This happens after any evaluation
        /// is performed regarding dimension sizes, so each argument of the REDIM must be processed and the error raised afterward.
        /// </summary>
        [Fact]
        public void ReDimAfterConstForSameNameInSameScopeResultsInIllegalAssignmentRuntimeError()
        {
            var source = @"
                CONST a = 1
                ReDim a(1)
            ";
            var expected = @"
                _outer.a = 1;
                _.NEWARRAY(new object[] { (Int16)1 });
                _.RAISEERROR(new IllegalAssignmentException(""'a'""));";
            Assert.Equal(
                expected.Replace(Environment.NewLine, "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray(),
                WithoutScaffoldingTranslator.GetTranslatedStatements(source, WithoutScaffoldingTranslator.DefaultConsoleExternalDependencies)
            );
        }

        [Fact]
        public void ConstsForTheSameVariableAreAllowedIfTheyAreInSeparateScopes()
        {
            var source = @"
                Const a = 1
                Function F1()
                    Const a = 1
                End Function
            ";
            var expected = @"
                _outer.a = 1;

                public object f1()
                {
                    object retVal1 = null;
                    object a = null;
                    a = 1;
                    return retVal1;
                }";
            Assert.Equal(
                expected.Replace(Environment.NewLine, "\n").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray(),
                WithoutScaffoldingTranslator.GetTranslatedStatements(source, WithoutScaffoldingTranslator.DefaultConsoleExternalDependencies)
            );
        }
    }
}
