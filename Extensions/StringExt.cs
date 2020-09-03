using System;
using System.Linq;

namespace CSharp_Library.Extensions {
    public static class StringExt {
        public static bool IsAlphaNumeric(this string str) {
            if (string.IsNullOrEmpty(str))
                return false;

            return (str.ToCharArray().All(c => Char.IsLetter(c) || Char.IsNumber(c)));
        }

        public static string RemoveWhitespace(this string input) {
            return new string(input.ToCharArray()
                              .Where(c => !Char.IsWhiteSpace(c))
                              .ToArray());
        }

        public static char First(this string input, char altValue) {
            if (input.Length == 0) {
                return altValue;
            }
            return input[0];
        }
    }
}