
namespace CSharp_Library.Utility {
    public class StringScanner {
        public int pos = 0;
        public readonly string source;
        public StringScanner(string source) {
            this.source = source;
        }

        public bool GoToString(string target) {
            int newIndex = source.IndexOf(target, pos);
            if (newIndex == -1)
                return false;
            pos = newIndex;
            return true;
        }

        public bool GoToStringReverse(string target) {
            int newIndex = source.LastIndexOf(target, pos);
            if (newIndex == -1)
                return false;
            pos = newIndex;
            return true;
        }

        public bool GoToEndOfString(string target) {
            int originalPos = pos;
            if (GoToString(target) == false)
                return false;
            if (Skip(target.Length) == false) {
                pos = originalPos;
                return false;
            }
            return true;
        }

        public bool Skip(int length) {
            if (pos + length >= source.Length)
                return false;
            pos += length;
            return true;
        }

        public string ReadToString(string endString) {
            int endIndex = source.IndexOf(endString, pos);
            if (endIndex == -1)
                return null;

            int contentLength = endIndex - pos;
            string content = source.Substring(pos, contentLength);
            pos = endIndex;

            return content;
        }

        public string ReadNextBetweenStrings(string begin, string end, string skipTo = null) {
            int originalPos = pos;
            if (skipTo != null && GoToEndOfString(skipTo) == false)
                return null;

            if (GoToEndOfString(begin) == false) {
                pos = originalPos;
                return null;
            }

            string content = ReadToString(end);
            if (content == null) {
                pos = originalPos;
                return null;
            }

            if (Skip(end.Length) == false) {
                pos = originalPos;
                return null;
            }

            return content;
        }
    }
}
