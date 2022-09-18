using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AeroltChatServer
{
    public class Censor
    {
        /// <summary>
        /// Get or set the censored words list.
        /// </summary>
        /// <value>The censored words list.</value>
        public IList<string> CensoredWords { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Censor"/> class.
        /// </summary>
        /// <param name="censoredWords">Censored words, if null uses default list</param>
        public Censor(IEnumerable<string> censoredWords = null) =>
            CensoredWords = censoredWords == null ? new List<string>() : new List<string>(censoredWords);

        /// <summary>
        /// Censors the text and replaces dirty words with ****
        /// </summary>
        /// <returns>The text that is now censored</returns>
        /// <param name="text">Text to censor</param>
        public string CensorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var censoredText = text;

            foreach (string censoredWord in CensoredWords)
            {
                var regularExpression = ToRegexPattern(censoredWord);
                
                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (censoredText.Contains(censoredWord))
                {
                    censoredText.Replace(censoredWord, censoredText);
                }
                
            }

            
            
            return censoredText;
        }

        /// <summary>
        /// Determines whether the text is dirty (has a bad word in it).
        /// </summary>
        /// <returns><c>true</c> if text is dirty; otherwise, <c>false</c>.</returns>
        /// <param name="text">Text to check for dirty words.</param>
        public bool HasCensoredWord(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var censoredText = text;

            foreach (var censoredWord in CensoredWords)
            {
                var regularExpression = ToRegexPattern(censoredWord);

                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

                if (censoredText != text)
                    return true;
            }

            return false;
        }

        static string StarCensoredMatch(Group m) =>
            new string('*', m.Captures[0].Value.Length);

        static string ToRegexPattern(string wildcardSearch)
        {
            var regexPattern = Regex.Escape(wildcardSearch);

            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");

            if (regexPattern.StartsWith(".*?", StringComparison.Ordinal))
            {
                regexPattern = regexPattern.Substring(3);
                regexPattern = @"(^\b)*?" + regexPattern;
            }

            regexPattern = @"\b" + regexPattern + @"\b";

            return regexPattern;
        }
    }
}