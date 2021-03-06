﻿using System;
using System.Collections.Generic;
using System.Linq;
using VBScriptTranslator.LegacyParser.Tokens;
using VBScriptTranslator.LegacyParser.Tokens.Basic;

namespace VBScriptTranslator.LegacyParser.ContentBreaking
{
    public static class TokenBreaker
    {
        private const string TokenBreakChars = ",.*&+-/\\=!(){}[]<>:;\n";

        /// <summary>
        /// Break down an UnprocessedContentToken into a combination of AtomToken and AbstractEndOfStatementToken references. This will never return null nor a set
        /// containing any null references.
        /// </summary>
        public static IEnumerable<IToken> BreakUnprocessedToken(UnprocessedContentToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            var lineIndex = token.LineIndex;
            var buffer = "";
            var content = token.Content;
            var tokens = new List<IToken>();
            for (var index = 0; index < content.Length; index++)
            {
                var chr = content.Substring(index, 1);
                if (char.IsWhiteSpace(chr, 0) && (chr != "\n"))
                {
                    // If we've found a (non-line-return) whitespace character, push content retrieved from the token so far (if any), into a fresh token on the
                    // list and clear the buffer to accept following data.
                    if (buffer != "")
                        tokens.Add(AtomToken.GetNewToken(buffer, lineIndex));
                    buffer = "";
                }
                else
                {
                    bool characterIsTokenBreaker;
                    if (TokenBreakChars.IndexOf(chr) != -1)
                        characterIsTokenBreaker = true;
                    else if (chr == "_")
                    {
                        // An underscore is a line return continuation character if it follows whitespace, but it must be part of a variable name if it is not
                        // preceded by whitespace (and line return continuation is a token-breaker, as opposed to an underscore that is part of the current
                        // token)
                        characterIsTokenBreaker = (index > 0) && char.IsWhiteSpace(content, index - 1);
                    }
                    else
                        characterIsTokenBreaker = false;
                    if (characterIsTokenBreaker)
                    {
                        // If the current character is a "&" then it may be a string concatenation or it may be the start of a hex number (eg. "&h001"), if it's
                        // the latter then we want to represent the content as a single token "&h001" not break the "&" out.
                        if ((chr == "&") && (index <= (content.Length - 3)))
                        {
                            var chrNext = content.Substring(index + 1, 1);
                            var chrNextNext = content.Substring(index + 2, 1);
                            if (chrNext.Equals("H", StringComparison.InvariantCultureIgnoreCase) && ("0123456789".IndexOf(chrNextNext) != -1))
                            {
                                buffer += chr;
                                continue;
                            }
                        }

                        // If we've found another "break" character (which means a token split is identified, but that we want to keep the break character itself,
                        // unlike with whitespace breaks), then do similar to above.
                        if (buffer != "")
                            tokens.Add(AtomToken.GetNewToken(buffer, lineIndex));
                        tokens.Add(AtomToken.GetNewToken(chr, lineIndex));
                        buffer = "";
                    }
                    else
                        buffer += chr;
                }
                if (chr == "\n")
                    lineIndex++;
            }
            if (buffer != "")
                tokens.Add(AtomToken.GetNewToken(buffer, lineIndex));

            // Handle ignore-line-return / end-of-statement combinations
            tokens = handleLineReturnCancels(tokens);

            return tokens;
        }

        /// <summary>
        /// Look for any "_" character AtomTokens and ensure they are followed by a line return - if so, drop both (if not, raise exception - invalid VBScript)
        /// </summary>
        private static List<IToken> handleLineReturnCancels(List<IToken> tokens)
        {
            var tokensOut = new List<IToken>();
            for (int index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                if ((token is AtomToken) && (token.Content == "_"))
                {
                    // Ensure followed by line return, then ignore both tokens
                    if (index == (tokens.Count - 1))
                        throw new Exception("Encountered line-return cancellation that isn't followed by a line return - invalid");
                    var tokenNext = tokens[index + 1];
                    if (!(tokenNext is EndOfStatementNewLineToken))
                        throw new Exception("Encountered line-return cancellation that isn't followed by a line return - invalid");
                    index++;
                }
                else
                    tokensOut.Add(token);
            }
            return tokensOut;
        }
    }
}
