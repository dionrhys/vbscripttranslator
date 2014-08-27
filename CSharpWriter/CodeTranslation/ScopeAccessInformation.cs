﻿using CSharpWriter.Lists;
using System;
using VBScriptTranslator.LegacyParser.CodeBlocks.Basic;
using VBScriptTranslator.LegacyParser.Tokens.Basic;

namespace CSharpWriter.CodeTranslation
{
    public class ScopeAccessInformation
    {
        public ScopeAccessInformation(
			IHaveNestedContent parent,
			IDefineScope scopeDefiningParent,
            CSharpName parentReturnValueNameIfAny,
            CSharpName errorRegistrationTokenIfAny,
            NonNullImmutableList<NameToken> externalDependencies,
            NonNullImmutableList<ScopedNameToken> classes,
            NonNullImmutableList<ScopedNameToken> functions,
            NonNullImmutableList<ScopedNameToken> properties,
            NonNullImmutableList<ScopedNameToken> variables)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");
            if (scopeDefiningParent == null)
                throw new ArgumentNullException("scopeDefiningParent");
            if (externalDependencies == null)
                throw new ArgumentNullException("externalDependencies");
            if (classes == null)
                throw new ArgumentNullException("classes");
            if (functions == null)
                throw new ArgumentNullException("functions");
            if (properties == null)
                throw new ArgumentNullException("properties");
            if (variables == null)
                throw new ArgumentNullException("variables");

            Parent = parent;
			ScopeDefiningParent = scopeDefiningParent;
            ErrorRegistrationTokenIfAny = errorRegistrationTokenIfAny;
            ParentReturnValueNameIfAny = parentReturnValueNameIfAny;
            ExternalDependencies = externalDependencies;
            Classes = classes;
            Functions = functions;
            Properties = properties;
            Variables = variables;
        }

        public static ScopeAccessInformation FromOutermostScope(IDefineScope outermostScope, NonNullImmutableList<NameToken> externalDependencies)
        {
            if (outermostScope == null)
                throw new ArgumentNullException("outermostScope");

            return new ScopeAccessInformation(
                outermostScope, // parent
                outermostScope, // scope-defining parent
                null, // parentReturnValueNameIfAny
                null, // errorRegistrationTokenIfAny
                externalDependencies,
                new NonNullImmutableList<ScopedNameToken>(), // classes
                new NonNullImmutableList<ScopedNameToken>(), // functions,
                new NonNullImmutableList<ScopedNameToken>(), // properties,
                new NonNullImmutableList<ScopedNameToken>() // variables
            );
        }

        /// <summary>
        /// This will never be null - if this is a statement within the outermost scope, there should be a construct to identify this (the OuterMostScope
        /// class is intended to be used for this purposes)
        /// </summary>
		public IHaveNestedContent Parent { get; private set; }

		/// <summary>
        /// This will never be null - if this is a statement within the outermost scope, there should be a construct to identify this (the OuterMostScope
        /// class is intended to be used for this purposes). This may be the same reference as Parent or it may be a different one - if, for example this
        /// is for a statement within an IF block then the Parent will be the IF block and and ScopeDefiningParent will be the Function / Property or
        /// OuterMostScope containing the IF.
		/// </summary>
		public IDefineScope ScopeDefiningParent { get; private set; }

        /// <summary>
        /// This will be null if the ScopeDefining is not a structure that returns a value. If ScopeDefiningParent IS a structure that returns a value
        /// (ie. FUNCTION or PROPERTY) then this will be non-null.
        /// </summary>
        public CSharpName ParentReturnValueNameIfAny { get; private set; }

        /// <summary>
        /// This must not be null if error-trapping is to be supported by the current scope. If it is null then error-trapping can be never be applied
        /// to translated statements (it being non-null does not mean that error-trapping will be applied to ALL statements, it depends upon where and
        /// how error-trapping is enabled by the code being translated).
        /// </summary>
        public CSharpName ErrorRegistrationTokenIfAny { get; private set; }

        /// <summary>
        /// These are references that are declared as being a compulsory and expected part of the Environment References - eg. if a command line
        /// script is being translated then WScript may be an expected External Dependency and warnings should not be emitted about accessing
        /// it, even though there is nothing to indicate its presence in the source. This will never be null.
        /// </summary>
        public NonNullImmutableList<NameToken> ExternalDependencies { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public NonNullImmutableList<ScopedNameToken> Classes { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public NonNullImmutableList<ScopedNameToken> Functions { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public NonNullImmutableList<ScopedNameToken> Properties { get; private set; }

        /// <summary>
        /// This will never be null
        /// </summary>
        public NonNullImmutableList<ScopedNameToken> Variables { get; private set; }

        // TODO: We can get rid of this now that ScopeDefiningParent may never be null
        public ScopeLocationOptions ScopeLocation
        {
            get { return ScopeDefiningParent.Scope; }
        }
    }
}
