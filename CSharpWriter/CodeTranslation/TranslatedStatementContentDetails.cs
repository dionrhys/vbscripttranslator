﻿using VBScriptTranslator.CSharpWriter.Lists;
using System;
using VBScriptTranslator.LegacyParser.Tokens.Basic;

namespace VBScriptTranslator.CSharpWriter.CodeTranslation
{
    public class TranslatedStatementContentDetails
    {
        public TranslatedStatementContentDetails(string translatedContent, NonNullImmutableList<NameToken> variablesAccessed)
        {
            if (string.IsNullOrWhiteSpace(translatedContent))
                throw new ArgumentException("Null/blank translatedContent specified");
            if (variablesAccessed == null)
                throw new ArgumentNullException("variablesAccessed");

            TranslatedContent = translatedContent;
            VariablesAccessed = variablesAccessed;
        }

        /// <summary>
        /// This will never return null or blank
        /// </summary>
        public string TranslatedContent { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public NonNullImmutableList<NameToken> VariablesAccessed { get; private set; }
    }
}
