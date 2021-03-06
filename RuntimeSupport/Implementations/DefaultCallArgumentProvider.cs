﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace VBScriptTranslator.RuntimeSupport.Implementations
{
	public class DefaultCallArgumentProvider : IBuildCallArgumentProviders
    {
        private readonly List<Tuple<object, Action<object>>> _valuesWithUpdatesWhereRequired;
		private readonly IAccessValuesUsingVBScriptRules _vbscriptValueAccessor;
        private bool _useBracketsWhereZeroArguments;
        public DefaultCallArgumentProvider(IAccessValuesUsingVBScriptRules vbscriptValueAccessor)
        {
			if (vbscriptValueAccessor == null)
				throw new ArgumentNullException("vbscriptValueAccessor");

			_vbscriptValueAccessor = vbscriptValueAccessor;
			_valuesWithUpdatesWhereRequired = new List<Tuple<object, Action<object>>>();
            _useBracketsWhereZeroArguments = false;
        }

        /// <summary>
        /// Add an argument to the set that will be passed by-val, regardless of whether the target call site expects a by-ref or by-val argument.
        /// This should return a reference to itself to enable chaining when building up argument sets.
        /// </summary>
        public IBuildCallArgumentProviders Val(object value)
        {
            _valuesWithUpdatesWhereRequired.Add(Tuple.Create(value, (Action<object>)null));
            return this;
        }

        /// <summary>
        /// Add an argument to the set that will be passed by-ref if the target call site expects a by-ref argument (otherwise it will be treated as
        /// by-val). This should return a reference to itself to enable chaining when building up argument sets.
        /// </summary>
        public IBuildCallArgumentProviders Ref(object value, Action<object> valueUpdater)
        {
            if (valueUpdater == null)
                throw new ArgumentNullException("valueUpdater");

            _valuesWithUpdatesWhereRequired.Add(Tuple.Create(value, valueUpdater));
            return this;
        }

        /// <summary>
        /// Add an argument to the set that will be passed by-ref if the target call site expects a by-ref argument and if the value is an array
        /// (otherwise it will be treated as by-val). This should return a reference to itself to enable chaining when building up argument sets.
        /// </summary>
        public IBuildCallArgumentProviders RefIfArray(object target, IEnumerable<IProvideCallArguments> argumentProviders)
        {
            if (target == null)
                throw new ArgumentNullException("target");
			if (argumentProviders == null)
				throw new ArgumentNullException("argumentProviders");

			var argumentProvidersArray = argumentProviders.ToArray();
			if (argumentProvidersArray.Length == 0)
				throw new ArgumentException("There must be at least one argument provider");
			if (argumentProvidersArray.Any(p => p.NumberOfArguments == 0))
				throw new ArgumentException("There may not be any argument providers with zero arguments");

			// We need to pass a context reference to CALL and SET but we know that we're only interested in public members (which includes the
			// case of an array indexer, which is public) so we can set context as null since we don't need to set it to anything more interesting
			// (we don't need to have a reference to the caller in case any private members may be accessed because we're not going to access any
			// private members)
			object context = null;

			// Process all but the last set of argument providers, updating target with each call. If at any point target is not an array
			// then the final value will be passed ByVal (since there must be a function or property access involved, the result of which
			// is never passed ByRef).
			var passByVal = false;
			for (var index = 0; index < argumentProvidersArray.Length - 1; index++)
			{
				if (!target.GetType().IsArray)
					passByVal = true;
				target = _vbscriptValueAccessor.CALL(context, target, new string[0], argumentProvidersArray[index]);
			}
			if (!target.GetType().IsArray)
				passByVal = true;

			// Process the final arguments to get the value that should actually be passed as the argument. If we've determined that this
			// value should be passed ByVal then hand straight off to the Val method.
			var lastArgumentProvider = argumentProvidersArray.Last();
            var valueForArgument = _vbscriptValueAccessor.CALL(context, target, new string[0], lastArgumentProvider);
			if (passByVal)
				return Val(valueForArgument);

			// If the value must be passed ByRef then we pass in the same valueForArgument as above but in the valueUpdater callback we
			// have to call SET on the target (which is the array that valueForArgument was taken from) to push the ByRef value back
			// into the array. (The ByRef support here is only used if the method being called has ByRef arguments, otherwise it's
			// not required - but that is the responsibility of the CALL implementation to write the values back, the job of this
			// interface is to allow that to occur where necessary).
			return Ref(
				valueForArgument,
				v => _vbscriptValueAccessor.SET(v, context, target, null, lastArgumentProvider)
			);
        }

        /// <summary>
        /// Specify that brackets were specified, even if there were zero arguments - this may affect the available call mechanisms (eg. it will
        /// only access methods, not properties, on IDispatch targets). This information is only of use if there are zero arguments (though it
        /// is not invalid to indicate this even where there are arguments present). This should return a reference to itself to enable chaining
        /// when building up argument sets.
        /// </summary>
        public IBuildCallArgumentProviders ForceBrackets()
        {
            _useBracketsWhereZeroArguments = true;
            return this;
        }

        /// <summary>
        /// This will never return null
        /// </summary>
        public IProvideCallArguments GetArgs()
        {
            return new ArgumentProvider(_valuesWithUpdatesWhereRequired, _useBracketsWhereZeroArguments);
        }

        private class ArgumentProvider : IProvideCallArguments
        {
            private readonly List<Tuple<object, Action<object>>> _valuesWithUpdatesWhereRequired;
            public ArgumentProvider(List<Tuple<object, Action<object>>> valuesWithUpdatesWhereRequired, bool useBracketsWhereZeroArguments)
            {
                if (valuesWithUpdatesWhereRequired == null)
                    throw new ArgumentNullException("valuesWithUpdatesWhereRequired");

                _valuesWithUpdatesWhereRequired = valuesWithUpdatesWhereRequired;
                UseBracketsWhereZeroArguments = useBracketsWhereZeroArguments;
            }

            /// <summary>
            /// This will always be zero or greater
            /// </summary>
            public int NumberOfArguments { get { return _valuesWithUpdatesWhereRequired.Count; } }

            /// <summary>
            /// The presence of brackets in the source following a member access with zero arguments may affect the available calling mechanisms on
            /// the target, so this is important information to record and expose
            /// </summary>
            public bool UseBracketsWhereZeroArguments { get; private set; }

            /// <summary>
            /// This will always return a set with NumberOfArguments items in it
            /// </summary>
            public IEnumerable<object> GetInitialValues()
            {
                return _valuesWithUpdatesWhereRequired.Select(entry => entry.Item1);
            }

            /// <summary>
            /// The index must be zero or greater and less than NumberOfArguments. If the argument at that index may not be overrwritten then the
            /// function call will have no effect.
            /// </summary>
            public void OverwriteValueIfByRef(int index, object value)
            {
                if ((index < 0) || (index >= _valuesWithUpdatesWhereRequired.Count))
                    throw new ArgumentOutOfRangeException("index");

                var valueUpdater = _valuesWithUpdatesWhereRequired[index].Item2;
                if (valueUpdater != null)
                    valueUpdater(value);
            }
        }
    }
}
