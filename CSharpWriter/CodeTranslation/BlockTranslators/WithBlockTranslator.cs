﻿using System;
using System.Linq;
using VBScriptTranslator.CSharpWriter.CodeTranslation.Extensions;
using VBScriptTranslator.CSharpWriter.CodeTranslation.StatementTranslation;
using VBScriptTranslator.CSharpWriter.Lists;
using VBScriptTranslator.CSharpWriter.Logging;
using VBScriptTranslator.LegacyParser.CodeBlocks;
using VBScriptTranslator.LegacyParser.CodeBlocks.Basic;

namespace VBScriptTranslator.CSharpWriter.CodeTranslation.BlockTranslators
{
	public class WithBlockTranslator : CodeBlockTranslator
	{
		private readonly ITranslateIndividualStatements _statementTranslator;
		private readonly ILogInformation _logger;
		public WithBlockTranslator(
			CSharpName supportRefName,
			CSharpName envClassName,
			CSharpName envRefName,
			CSharpName outerClassName,
			CSharpName outerRefName,
			VBScriptNameRewriter nameRewriter,
			TempValueNameGenerator tempNameGenerator,
			ITranslateIndividualStatements statementTranslator,
			ITranslateValueSettingsStatements valueSettingStatementTranslator,
			ILogInformation logger)
			: base(supportRefName, envClassName, envRefName, outerClassName, outerRefName, nameRewriter, tempNameGenerator, statementTranslator, valueSettingStatementTranslator, logger)
		{
			if (statementTranslator == null)
				throw new ArgumentNullException("statementTranslator");
			if (logger == null)
				throw new ArgumentNullException("logger");

			_statementTranslator = statementTranslator;
			_logger = logger;
		}

		public TranslationResult Translate(WithBlock withBlock, ScopeAccessInformation scopeAccessInformation, int indentationDepth)
		{
			if (withBlock == null)
				throw new ArgumentNullException("withBlock");
			if (scopeAccessInformation == null)
				throw new ArgumentNullException("scopeAccessInformation");
			if (indentationDepth < 0)
				throw new ArgumentOutOfRangeException("indentationDepth", "must be zero or greater");

			var translatedTargetReference = _statementTranslator.Translate(withBlock.Target, scopeAccessInformation, ExpressionReturnTypeOptions.Reference, _logger.Warning);
			var undeclaredVariables = translatedTargetReference.VariablesAccessed
				.Where(v => !scopeAccessInformation.IsDeclaredReference(v, _nameRewriter));
			foreach (var undeclaredVariable in undeclaredVariables)
				_logger.Warning("Undeclared variable: \"" + undeclaredVariable.Content + "\" (line " + (undeclaredVariable.LineIndex + 1) + ")");

			var targetName = base._tempNameGenerator(new CSharpName("with"), scopeAccessInformation);
			var withBlockContentTranslationResult = Translate(
				withBlock.Content.ToNonNullImmutableList(),
				new ScopeAccessInformation(
					withBlock,
					scopeAccessInformation.ScopeDefiningParent,
					scopeAccessInformation.ParentReturnValueNameIfAny,
					scopeAccessInformation.ErrorRegistrationTokenIfAny,
					new ScopeAccessInformation.DirectedWithReferenceDetails(
						targetName,
						withBlock.Target.Tokens.First().LineIndex
					),
					scopeAccessInformation.ExternalDependencies,
					scopeAccessInformation.Classes,
					scopeAccessInformation.Functions,
					scopeAccessInformation.Properties,
					scopeAccessInformation.Constants,
					scopeAccessInformation.Variables,
				scopeAccessInformation.StructureExitPoints
				),
				indentationDepth
			);
			return new TranslationResult(
				withBlockContentTranslationResult.TranslatedStatements
					.Insert(
						new TranslatedStatement(
							string.Format(
								"var {0} = {1};",
								targetName.Name,
								translatedTargetReference.TranslatedContent
							),
							indentationDepth,
							withBlock.Target.Tokens.First().LineIndex
						),
						0
					),
				withBlockContentTranslationResult.ExplicitVariableDeclarations,
				withBlockContentTranslationResult.UndeclaredVariablesAccessed.AddRange(undeclaredVariables)
			);
		}

		private TranslationResult Translate(NonNullImmutableList<ICodeBlock> blocks, ScopeAccessInformation scopeAccessInformation, int indentationDepth)
		{
			if (blocks == null)
				throw new ArgumentNullException("block");
			if (scopeAccessInformation == null)
				throw new ArgumentNullException("scopeAccessInformation");
			if (indentationDepth < 0)
				throw new ArgumentOutOfRangeException("indentationDepth", "must be zero or greater");

			return base.TranslateCommon(
				base.GetWithinFunctionBlockTranslators(),
				blocks,
				scopeAccessInformation,
				indentationDepth
			);
		}
	}
}
