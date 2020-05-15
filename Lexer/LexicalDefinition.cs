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

        With /*, False, True, Integer, Boolean, Real,
        Char*/
    }

    public class KeywordElement : LexicalElement {
        public KeywordType Type { get; set; }
    }

    public enum OperatorType {
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

    public class OperatorElement : LexicalElement {
        public OperatorType Type { get; set; }
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
        private static readonly Dictionary<OperatorType, string> _OperatorToStringMap =
            new Dictionary<OperatorType, string>() {
                [OperatorType.Addition] = "+",
                [OperatorType.Subtraction] = "-",
                [OperatorType.Multiplication] = "*",
                [OperatorType.Division] = "/",
                [OperatorType.Equal] = "=",
                [OperatorType.Less] = "<",
                [OperatorType.Greater] = ">",
                [OperatorType.LeftSquareBracket] = "[",
                [OperatorType.RightSquareBracket] = "]",
                [OperatorType.Dot] = ".",
                [OperatorType.Comma] = ",",
                [OperatorType.Assign] = ":=",
                [OperatorType.Colon] = ":",
                [OperatorType.Semicolon] = ";",
                [OperatorType.LeftParentheses] = "(",
                [OperatorType.RightParentheses] = ")",
                [OperatorType.NotEqual] = "<>",
                [OperatorType.LE] = "<=",
                [OperatorType.GE] = ">=",
                [OperatorType.DoubleDot] = "..",
                [OperatorType.Caret] = "^"
            };

        public static readonly IReadOnlyDictionary<OperatorType, string> OperatorToStringMap = _OperatorToStringMap;
        private static Dictionary<string, OperatorType> _StringToOperatorMap = new Dictionary<string, OperatorType>(comparer: StringComparer.OrdinalIgnoreCase);
        public static readonly IReadOnlyDictionary<string, OperatorType> StringToOperatorMap = _StringToOperatorMap;

        private static readonly HashSet<char> OperatorChars = new HashSet<char>();

        private static readonly Dictionary<KeywordType, string> _KeywordToStringMap =
            new Dictionary<KeywordType, string>();

        public static readonly IReadOnlyDictionary<KeywordType, string> KeywordToStringMap = _KeywordToStringMap;

        private static readonly Dictionary<string, KeywordType> _StringToKeywordMap =
            new Dictionary<string, KeywordType>();

        public static readonly IReadOnlyDictionary<string, KeywordType> StringToKeywordMap = _StringToKeywordMap;

        private const string IdentifierCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789$_";
        private const string LegalIdentifierBeginCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ$_";

        static Mappings() {
            InitializeOperatorMapping();
            InitializeKeywordMap();
        }

        private static void InitializeKeywordMap() {
            foreach (KeywordType keyword in Enum.GetValues(typeof(KeywordType))) {
                string str = keyword.ToString().ToLower();
                _KeywordToStringMap.Add(keyword, str);
                _StringToKeywordMap.Add(str, keyword);
            }
        }

        private static void InitializeOperatorMapping() {
            foreach (var item in OperatorToStringMap) {
                _StringToOperatorMap.Add(item.Value, item.Key);
                foreach (char c in item.Value) {
                    OperatorChars.Add(c);
                }
            }
        }

        public static bool CanBeOperator(char c) {
            return OperatorChars.Contains(c);
        }

        public static bool CanBeIdentifier(char c, bool begin = false) {
            if (begin) return LegalIdentifierBeginCharacters.Contains(c);
            else return IdentifierCharacters.Contains(c);
        }
    }

    public class LexicalException : Exception {
        private int line, begin, end;
        private string reason;

        public LexicalException(int line, int begin, int end, string reason)
            : base(String.Format("{0}:[{1},{2}): {3}", line, begin, end, reason)) {
            this.line = line;
            this.begin = begin;
            this.end = end;
            this.reason = reason;
        }
    }
}