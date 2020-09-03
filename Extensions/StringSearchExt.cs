using System;
using System.Collections.Generic;

namespace CSharp_Library.Extensions {

    public static class StringSearchExt
    {
        static public bool ContainsText (this string target, string text)
        {
            return (target.IndexOf(text) != -1);
        }
        
        static public bool ContainsTextIgnoreCase (this string target, string text)
        {
            return target.ToLower().ContainsText(text.ToLower());
        }

        static public string GetStringAfterFirst(this string target, string prefix)
        {
            int index = target.IndexOf(prefix);
            if (index == -1)
                return null;
            return target.Substring(index + prefix.Length);
        }

        static public string GetStringAfterLast(this string target, string prefix)
        {
            int index = target.LastIndexOf(prefix);
            if (index == -1)
                return null;
            return target.Substring(index + prefix.Length);
        }

        static public string GetStringBeforeFirst(this string target, string suffix)
        {
            int index = target.IndexOf(suffix);
            if (index == -1)
                return null;
            return target.Substring(0, index);
        }

        static public string GetStringBeforeLast(this string target, string suffix)
        {
            int index = target.LastIndexOf(suffix);
            if (index == -1)
                return null;
            return target.Substring(0, index);
        }

        static public string GetStringInbetween(this string target, string prefix, string suffix)
        {
            string firstPiece = target.GetStringAfterFirst(prefix);
            if (firstPiece == null)
                firstPiece = target;

            string finalPiece = firstPiece.GetStringBeforeFirst(suffix);
            if (finalPiece == null)
                finalPiece = firstPiece;

            return finalPiece;
        }

        static public string GetStringInbetweenStrict(this string target, string prefix, string suffix)
        {
            string firstPiece = target.GetStringAfterFirst(prefix);
            if (firstPiece == null)
                return null;

            string finalPiece = firstPiece.GetStringBeforeFirst(suffix);
            return finalPiece;
        }

        static public string[] FindAllStringsInBetween(this string target, string prefix, string suffix, int skip = 0)
        {
            List<string> matched = new List<string>();
            int progress = skip;
            while (true)
            {
                int indexStart = target.IndexOf(prefix, progress);
                if (indexStart == -1)
                    break;
                indexStart += prefix.Length;
                int indexEnd = target.IndexOf(suffix, indexStart);
                if (indexEnd == -1)
                    break;

                string foundMatch = target.Substring(indexStart, indexEnd - indexStart);
                matched.Add(foundMatch);
                progress = indexEnd + suffix.Length;
            }

            return matched.ToArray();
        }
    }
}
