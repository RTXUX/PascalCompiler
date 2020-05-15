using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Lexical {
    public class Lexer {
        private static readonly Regex RegexOctal = new Regex("^[0-7]+$");
        private static readonly Regex RegexHexical = new Regex("^[0-9a-f]+$");
        public LinkedList<LexicalElement> Lex(StreamReader input) {
            LinkedList<LexicalElement> tokens = new LinkedList<LexicalElement>();
            int currentLine = 0;
            int currentCursor = 0;
            int beginOfToken;
            int endOfToken;
            char peekChar;
            Span<char> buffer = stackalloc char[9];
            while (!input.EndOfStream) {
                peekChar = Convert.ToChar(input.Peek());
                if (peekChar == '\n') {
                    // Line Feed
                    tokens.AddLast(new LineFeedElement()
                        {LineNumber = currentLine, StartIndex = currentCursor, EndIndex = currentCursor + 1});
                    currentLine += 1;
                    currentCursor = 0;
                    input.Read();
                } else if (Char.IsWhiteSpace(peekChar)) {
                    // WhiteSpace, Ignore and advance
                    input.Read();
                    currentCursor += 1;
                } else if (Mappings.CanBeOperator(peekChar)) {
                    // Operators
                    beginOfToken = currentCursor;
                    int length = 0;
                    while (!input.EndOfStream && Mappings.CanBeOperator(peekChar = Convert.ToChar(input.Peek()))) {
                        buffer[length++] = Convert.ToChar(input.Read());
                        currentCursor++;
                    }

                    endOfToken = currentCursor;
                    string s = buffer.Slice(0, length).ToString();
                    OperatorType ot;
                    if (Mappings.StringToOperatorMap.TryGetValue(s, out ot)) {
                        tokens.AddLast(new OperatorElement() {
                            LineNumber = currentLine,
                            StartIndex = beginOfToken,
                            EndIndex = endOfToken,
                            Type = ot,
                            StringValue = s
                        });
                    } else {
                        throw new LexicalException(currentLine, beginOfToken, endOfToken, $"Unknown token {s}");
                    }
                } else if (Mappings.CanBeIdentifier(peekChar, true)) {
                    // Identifier or keyword
                    beginOfToken = currentCursor;
                    StringBuilder sb = new StringBuilder();
                    while (!input.EndOfStream && Mappings.CanBeIdentifier(peekChar = Convert.ToChar(input.Peek()))) {
                        sb.Append(Convert.ToChar(input.Read()));
                        currentCursor++;
                    }

                    endOfToken = currentCursor;
                    string s = sb.ToString();
                    KeywordType kt;
                    if (Mappings.StringToKeywordMap.TryGetValue(s, out kt)) {
                        // Keyword
                        tokens.AddLast(new KeywordElement()
                            {LineNumber = currentLine, StartIndex = beginOfToken, EndIndex = endOfToken, Type = kt});
                    } else {
                        // Identifier
                        tokens.AddLast(new IdentifierElement(s)
                            {LineNumber = currentLine, StartIndex = beginOfToken, EndIndex = endOfToken});
                    }
                } else if (peekChar == '\"') {
                    // string
                    beginOfToken = currentCursor;
                    input.Read();
                    currentCursor++;
                    StringBuilder sb = new StringBuilder();
                    while (!input.EndOfStream && (peekChar = Convert.ToChar(input.Peek())) != '\"') {
                        if (peekChar == '\\') {
                            buffer[0] = Convert.ToChar(input.Peek());
                            buffer[1] = Convert.ToChar(input.Peek());
                            currentCursor += 2;
                            string s = buffer.Slice(0, 2).ToString();
                            try {
                                sb.Append(Regex.Unescape(s));
                            }
                            catch (ArgumentException) {
                                throw new LexicalException(currentLine, currentCursor - 2, currentCursor,
                                    String.Format("Unknown Escape Sequence: \"{0}\"", s));
                            }
                        } else {
                            sb.Append(Convert.ToChar(input.Read()));
                            currentCursor++;
                        }
                    }

                    input.Read();
                    currentCursor++;
                    endOfToken = currentCursor;
                    string str = sb.ToString();
                    tokens.AddLast(new StringLiteral()
                        {LineNumber = currentLine, StartIndex = beginOfToken, EndIndex = endOfToken, Value = str});
                } else if (Char.IsDigit(peekChar)) {
                    // Numerical
                    StringBuilder sb = new StringBuilder();
                    /*if (peekChar == '0') {
                        // Hex or oct
                        sb.Append(Convert.ToChar(input.Read()));
                        currentCursor++;
                        peekChar = Convert.ToChar(input.Peek());
                        if (peekChar == '0') {
                            // Autodetect
                        }
                        else if (peekChar=='x') {
                            //Hex
                        }
                        else if (!Char.IsDigit(peekChar)) {
                            // Zero
                            
                        }
                        else {
                            // Oct
                        }
                    }
                    else {
                        // Dec
                    }*/
                    beginOfToken = currentCursor;
                    while (!input.EndOfStream && char.IsLetterOrDigit(peekChar = Convert.ToChar(input.Peek()))) {
                        sb.Append(Convert.ToChar(input.Read()));
                        currentCursor++;
                    }

                    endOfToken = currentCursor;
                    bool floating = false;
                    string s = sb.ToString();
                    int integerValue = 0;
                    try {
                        if (s.Contains('.')) {
                            floating = true;
                            var value = double.Parse(s);
                            tokens.AddLast(new RealLiteral() {
                                LineNumber = currentLine,
                                StartIndex = beginOfToken,
                                EndIndex = endOfToken,
                                Value = value
                            });
                        } else if (s.StartsWith("00")) {
                            // Autodetect
                            if (RegexOctal.IsMatch(s)) {
                                integerValue = Convert.ToInt32(s.Substring(2), 8);
                            } else if (RegexHexical.IsMatch(s)) {
                                integerValue = Convert.ToInt32(s.Substring(2), 16);
                            } else {
                                throw new LexicalException(currentLine, beginOfToken, endOfToken, String.Format("Malformed numerical literal \"{0}\"", s));
                            }
                        } else if (s.StartsWith("0x")) {
                            // Hex
                            integerValue = Convert.ToInt32(s.Substring(2), 16);
                        } else if (s.StartsWith("0")) {
                            // Oct
                            integerValue = Convert.ToInt32(s.Substring(1), 8);
                        } else {
                            // Dec
                            integerValue = Convert.ToInt32(s);
                        }

                        if (!floating) {
                            tokens.AddLast(new IntegerLiteral() {
                                LineNumber = currentLine,
                                StartIndex = beginOfToken,
                                EndIndex = endOfToken,
                                Value = integerValue,
                                StringValue = s
                            });
                        }
                    } catch (Exception e) {
                        throw new LexicalException(currentLine, beginOfToken, endOfToken, String.Format("Malformed numerical literal \"{0}\"", s));
                    }
                }
            }

            return tokens;
        }
    }
}