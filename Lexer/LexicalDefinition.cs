using System;
using System.Collections.Generic;
using System.Linq;

namespace PascalCompiler.Lexical.Definition {
    public class LexicalElement {
        public string StringValue { get; set; }

        public int LineNumber { get; set; }

        // startIndex is inclusive, endIndex is exclusive. i.e. [startIndex, endIndex)
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }

    public class IdentifierElement : LexicalElement {
        public string Value { get; set; }

        public IdentifierElement(string value) {
            this.Value = value;
        }
    }

    public enum KeywordType {
        And,
        File,
        Mod,
        Repeat,
        Array,
        For,
        Nil,
        Set,
        Begin,
        Forward,
        Not,
        Then,
        Case,
        Function,
        Of,
        To,
        Const,
        Goto,
        Or,
        Type,
        Div,
        If,
        Packed,
        Until,
        Do,
        In,
        Procedure,
        Var,
        Downto,
        Label,
        Program,
        While,
        Else,
        Main,
        Record,
        End,
        With /*, False, True, Integer, Boolean, Real,
        Char*/
    }

    public class KeywordElement : LexicalElement {
        public KeywordType Type { get; set; }
    }

    public enum NonwordType {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Equal,
        Less,
        Greater,
        LeftSquareBracket,
        RightSquareBracket,
        Dot,
        Comma,
        Assign,
        Colon,
        Semicolon,
        LeftParentheses,
        RightParentheses,
        NotEqual,
        LE,
        GE,
        DoubleDot,
        Caret
    }

    public class NonwordElement : LexicalElement {
        public NonwordType Type { get; set; }
    }

    public class IntegerLiteral : LexicalElement {
        public int Value { get; set; }
    }

    public class RealLiteral : LexicalElement {
        public double Value { get; set; }
    }

    public class LineFeedElement : LexicalElement {
        public LineFeedElement() {
            StringValue = "\n";
        }
    }

    public class StringLiteral : LexicalElement {
        public string Value { get; set; }
    }

    public class Mappings {
        private static readonly Dictionary<NonwordType, string> _NonwordToStringMap =
            new Dictionary<NonwordType, string>() {
                [NonwordType.Addition] = "+",
                [NonwordType.Subtraction] = "-",
                [NonwordType.Multiplication] = "*",
                [NonwordType.Division] = "/",
                [NonwordType.Equal] = "=",
                [NonwordType.Less] = "<",
                [NonwordType.Greater] = ">",
                [NonwordType.LeftSquareBracket] = "[",
                [NonwordType.RightSquareBracket] = "]",
                [NonwordType.Dot] = ".",
                [NonwordType.Comma] = ",",
                [NonwordType.Assign] = ":=",
                [NonwordType.Colon] = ":",
                [NonwordType.Semicolon] = ";",
                [NonwordType.LeftParentheses] = "(",
                [NonwordType.RightParentheses] = ")",
                [NonwordType.NotEqual] = "<>",
                [NonwordType.LE] = "<=",
                [NonwordType.GE] = ">=",
                [NonwordType.DoubleDot] = "..",
                [NonwordType.Caret] = "^"
            };

        public static readonly IReadOnlyDictionary<NonwordType, string> NonwordToStringMap = _NonwordToStringMap;
        private static Dictionary<string, NonwordType> _StringToNonwordMap = new Dictionary<string, NonwordType>(comparer: StringComparer.OrdinalIgnoreCase);
        public static readonly IReadOnlyDictionary<string, NonwordType> StringToNonwordMap = _StringToNonwordMap;

        private static readonly HashSet<char> NonwordChars = new HashSet<char>();

        private static readonly Dictionary<KeywordType, string> _KeywordToStringMap =
            new Dictionary<KeywordType, string>();

        public static readonly IReadOnlyDictionary<KeywordType, string> KeywordToStringMap = _KeywordToStringMap;

        private static readonly Dictionary<string, KeywordType> _StringToKeywordMap =
            new Dictionary<string, KeywordType>();

        public static readonly IReadOnlyDictionary<string, KeywordType> StringToKeywordMap = _StringToKeywordMap;

        private const string IdentifierCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$_";
        private const string LegalIdentifierBeginCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ$_";

        static Mappings() {
            InitializeNonwordMapping();
            InitializeKeywordMap();
        }

        private static void InitializeKeywordMap() {
            foreach (KeywordType keyword in Enum.GetValues(typeof(KeywordType))) {
                string str = keyword.ToString().ToLower();
                _KeywordToStringMap.Add(keyword, str);
                _StringToKeywordMap.Add(str, keyword);
            }
        }

        private static void InitializeNonwordMapping() {
            foreach (var item in NonwordToStringMap) {
                _StringToNonwordMap.Add(item.Value, item.Key);
                foreach (char c in item.Value) {
                    NonwordChars.Add(c);
                }
            }
        }

        public static bool CanBeNonword(char c) {
            return NonwordChars.Contains(c);
        }

        public static bool CanBeIdentifier(char c, bool begin = false) {
            if (begin) return LegalIdentifierBeginCharacters.Contains(c);
            else return IdentifierCharacters.Contains(c);
        }

        public static bool CanBePairedNonword(char c) {
            return ":<>=.".Contains(c);
        }
    }

    public class LexicalException : Exception {
        public int line, begin, end;
        public string reason;

        public LexicalException(int line, int begin, int end, string reason)
            : base(String.Format("{0}:[{1},{2}): {3}", line, begin, end, reason)) {
            this.line = line;
            this.begin = begin;
            this.end = end;
            this.reason = reason;
        }
    }
}