using CSharp_Library.Extensions;

namespace CSharp_Library.Utility {
    public class StringScanner {
        public int pos = 0;
        public readonly string source;
        public StringScanner(string source) {
            this.source = source;
        }

        public bool IsComplete() {
            return pos >= source.Length;
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

        //Assumes 
        public string SkipUntil(string[] listToken) {
            foreach (string item in listToken) {
                if (item.Length == 0) {
                    throw new System.Exception("Invalid Input; String with 0 length");
                }
            }
            var listFirstChar = listToken.Map(token => token[0]);
            while (!IsComplete()) {
                int index = source.IndexOfAny(listFirstChar, pos);
                if (index == -1) {
                    pos = source.Length;
                    return null;
                }
                foreach (string token in listToken) {
                    if (ContainsString(token, index)) {
                        pos = index;
                        return token;
                    }
                }
                pos = index + 1;
            }
            return null;
        }

        public char? SkipUntil(char[] listToken) {
            int index = source.IndexOfAny(listToken, pos);
            if (index == -1) {
                pos = source.Length;
                return null;
            }
            pos = index;
            return source[pos];
        }

        public void SkipUntil(string token) {
            if (token.Length == 0) {
                throw new System.Exception("Invalid Input; Token with 0 length");
            }
            int endIndex = source.IndexOf(token, pos);
            if (endIndex == -1) {
                pos = source.Length;
            } else {
                pos = endIndex;
            }
        }

        public void SkipUntil(char token) {
            int endIndex = source.IndexOf(token, pos);
            if (endIndex == -1) {
                pos = source.Length;
            } else {
                pos = endIndex;
            }
        }

        public bool ContainsString(string value, int index) {
            if (index + value.Length > source.Length) {
                return false;
            }
            for (int i = 0; i < value.Length; i+=1) {
                if (value[i] != source[index + i]) {
                    return false;
                }
            }
            return true;
        }

        public bool ContainsStringUnsafe(string value, int index) {
            for (int i = 0; i < value.Length; i+=1) {
                if (value[i] != source[index + i]) {
                    return false;
                }
            }
            return true;
        }

        //Hello\n  //Hello\n, 0, 6
        public bool ContainsStringInRange(string value, int index, int length) {
            var maxIndex = length - value.Length + 1;
            if (index + length > source.Length) {
                return false;
            }
            if (maxIndex <= 0) {
                return false;
            }
            for (int i = 0; i < maxIndex; i+=1) {
                if (ContainsStringUnsafe(value, index + i)) {
                    return true;
                }
            }
            return false;
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

        //Reads an escaped string
        public string ReadEscapedString(char type, char escapeChar = '\\') {
            int originalPos = pos;

            if (source[pos] != type) {
                return null;
            }
            pos += 1;
            char[] possibleChars = {type, escapeChar};
            do {
                var foundChar = SkipUntil(possibleChars);
                if (foundChar == null) {
                    pos = originalPos;
                    return null;
                }
                if (foundChar == type) {
                    pos += 1;
                    break;
                }
                pos += 2;
            } while(true);
            
            return source.Substring(originalPos, pos - originalPos);
        }

        public bool CurrentLineContains(string value, bool readEntireLine = true) {
            var startPos = pos - 1;
            while (startPos >= 0 && source[startPos] != '\n') {
                startPos -= 1;
            }
            startPos += 1;
            
            var endPos = pos;
            if (readEntireLine) {
                while (endPos < source.Length && source[endPos] != '\n') {
                    endPos += 1;
                }
                endPos += 1;
            }
            var length = endPos - startPos;
            var result = ContainsStringInRange(value, startPos, length);
            return result;
        }
    }
}
