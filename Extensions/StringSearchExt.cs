using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductScraper
{
    public class StringScanner
    {
        public int pos = 0;
        public readonly string source;
        public StringScanner(string source)
        {
            this.source = source;
        }

        public bool GoToString(string target)
        {
            int newIndex = source.IndexOf(target, pos);
            if (newIndex == -1)
                return false;
            pos = newIndex;
            return true;
        }

        public bool GoToStringReverse(string target)
        {
            int newIndex = source.LastIndexOf(target, pos);
            if (newIndex == -1)
                return false;
            pos = newIndex;
            return true;
        }

        public bool GoToEndOfString(string target)
        {
            int originalPos = pos;
            if (GoToString(target) == false)
                return false;
            if (Skip(target.Length) == false) {
                pos = originalPos;
                return false;
            }
            return true;
        }
        
        public bool Skip(int length)
        {
            if (pos + length >= source.Length)
                return false;
            pos += length;
            return true;
        }

        public string ReadToString(string endString)
        {
            int endIndex = source.IndexOf(endString, pos);
            if (endIndex == -1)
                return null;

            int contentLength = endIndex - pos;
            string content = source.Substring(pos, contentLength);
            pos = endIndex;

            return content;
        }

        public string ReadNextBetweenStrings(string begin, string end, string skipTo = null)
        {
            int originalPos = pos;
            if (skipTo != null && GoToEndOfString(skipTo) == false)
                return null;

            if (GoToEndOfString(begin) == false)
            {
                pos = originalPos;
                return null;
            }

            string content = ReadToString(end);
            if (content == null)
            {
                pos = originalPos;
                return null;
            }

            if (Skip(end.Length) == false)
            {
                pos = originalPos;
                return null;
            }

            return content;
        }
    }

    static class LevenshteinDistance
    {
        public static int Compute(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t))
            {
                return s.Length;
            }

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }
            return d[n, m];
        }
    }

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

        static public string[] FindAllStringsInBetween(this string target, string prefix, string suffix)
        {
            List<string> matched = new List<string>();
            int progress = 0;
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
