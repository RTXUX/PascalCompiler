using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Lexical {

    enum State {
        TransitionNeeded, IdentifierOrKeyword, Operator, WhiteCharacters, CR, LF, String, Number, Comment
    }

    public class LexerStateMachine {
        private static readonly Regex RegexBinary = new Regex(@"^[0-1]+$");
        private static readonly Regex RegexOctal = new Regex(@"^[0-7]+$");
        private static readonly Regex RegexDecimal = new Regex(@"^[0-9]+$");
        private static readonly Regex RegexHexadecimal = new Regex(@"^[0-9a-fA-F]+$");
        private State _state = State.TransitionNeeded;
        private char lastChar = '\0';
        private int currentLine = 0;
        private int currentCursor = 0;
        private int currentOffset = 0;

        public StreamReader input { get; set; }

        public LexerStateMachine(StreamReader input) {
            this.input = input;
        }

        public char AdvanceChar() {
            lastChar = input.EndOfStream ? '\0' : Convert.ToChar(input.Read());
            currentCursor++;
            currentOffset++;
            return lastChar;
        }

        public LexicalElement NextToken() {
            LexicalElement result = null;
            Span<char> buffer = stackalloc char[129];
            int bufferFill = 0;
            bool run = true;
            if (input.EndOfStream) return null;
            int startIndex = currentCursor-1;
            while (result == null && run) {
                switch (_state) {
                    case State.TransitionNeeded:
                        bufferFill = 0;
                        startIndex = currentCursor - 1;
                        if (lastChar == '\r') {
                            _state = State.CR;
                        } else if (lastChar == '\n') {
                            _state = State.LF;
                        } else if (Mappings.CanBeOperator(lastChar)) {
                            _state = State.Operator;
                        } else if (Mappings.CanBeIdentifier(lastChar, true)) {
                            _state = State.IdentifierOrKeyword;
                        } else if (lastChar == '\"') {
                            _state = State.String;
                        } else if (char.IsDigit(lastChar)) {
                            _state = State.Number;
                        } else if (char.IsWhiteSpace(lastChar)) {
                            _state = State.WhiteCharacters;
                        } else if (lastChar == '{') {
                            _state = State.Comment;
                        } else if (lastChar == '\0') {
                            return null;
                        } else {
                            throw new LexicalException(currentLine, currentCursor-1, currentCursor, $"Unknown character \'{lastChar}\'");
                        }
                        continue;
                    case State.CR:
                        AdvanceChar();
                        if (lastChar == '\n') {
                            _state = State.TransitionNeeded;
                            result = new LineFeedElement() {LineNumber = currentLine, StartIndex = currentCursor-2, EndIndex = currentCursor, StringValue = "\r\n"};
                            currentCursor = 0;
                            currentLine++;
                            AdvanceChar();
                            return result;
                        } else {
                            _state = State.TransitionNeeded;
                            result = new LineFeedElement() {LineNumber = currentLine, StartIndex = currentCursor-2, EndIndex = currentCursor-1, StringValue = "\r"};
                            currentLine++;
                            currentCursor = 0;
                            AdvanceChar();
                            return result;
                        }
                    case State.LF:
                        _state = State.TransitionNeeded;
                        result = new LineFeedElement() { LineNumber = currentLine, StartIndex = currentCursor - 2, EndIndex = currentCursor - 1, StringValue = "\r" };
                        currentLine++;
                        currentCursor = 0;
                        AdvanceChar();
                        return result;
                    case State.WhiteCharacters:
                        if (!char.IsWhiteSpace(AdvanceChar())) {
                            _state = State.TransitionNeeded;
                        }
                        continue;
                    case State.Operator:
                        buffer[bufferFill++] = lastChar;
                        if (!Mappings.CanBeOperator(AdvanceChar())) {
                            _state = State.TransitionNeeded;
                            string s = buffer.Slice(0, bufferFill).ToString();
                            OperatorType ot;
                            if (Mappings.StringToOperatorMap.TryGetValue(s, out ot)) {
                                result = new OperatorElement() {LineNumber = currentLine, StartIndex = currentCursor-1-bufferFill, EndIndex = currentCursor-1, Type = ot, StringValue = s};
                                return result;
                            } else {
                                throw new LexicalException(currentLine, currentCursor-1-bufferFill, currentCursor-1, $"Unknown operator: \"{s}\"");
                            }
                        } 
                        break;
                    case State.IdentifierOrKeyword:
                        buffer[bufferFill++] = lastChar;
                        if (!Mappings.CanBeIdentifier(AdvanceChar())) {
                            _state = State.TransitionNeeded;
                            string s = buffer.Slice(0, bufferFill).ToString();
                            KeywordType kt;
                            if (Mappings.StringToKeywordMap.TryGetValue(s, out kt)) {
                                result = new KeywordElement() {LineNumber = currentLine, StartIndex = currentCursor - 1 - bufferFill, EndIndex = currentCursor - 1, Type = kt, StringValue = s};
                                return result;
                            }
                            result = new IdentifierElement(s) { LineNumber = currentLine, StartIndex = currentCursor - 1 - bufferFill, EndIndex = currentCursor - 1, StringValue = s };
                        }
                        break;
                    case State.String:
                        if (char.IsControl(lastChar)) throw new LexicalException(currentLine, currentCursor-1-bufferFill, currentCursor-1, $"Malformed string");
                        if (AdvanceChar() != '\"') {
                            buffer[bufferFill++] = lastChar;
                            if (lastChar == '\\') {
                                int ob = bufferFill-1;
                                buffer[bufferFill] = AdvanceChar();
                                try {
                                    string es = Regex.Unescape(buffer.Slice(ob, 2).ToString());
                                    bufferFill = ob;
                                    buffer[bufferFill++] = es[0];
                                } catch {
                                    throw new LexicalException(currentLine, currentCursor-2, currentCursor, $"Unknown string escape sequence \"{buffer.Slice(ob, 2).ToString()}\"");
                                }
                            }
                            
                        } else {
                            _state = State.TransitionNeeded;
                            result = new StringLiteral() {LineNumber = currentLine, StartIndex = startIndex, EndIndex = currentCursor-1, Value = buffer.Slice(0, bufferFill).ToString()};
                            AdvanceChar();
                            return result;
                        }

                        break;
                    case State.Number:
                        buffer[bufferFill++] = lastChar;
                        AdvanceChar();
                        if (!char.IsLetterOrDigit(lastChar) && lastChar != '.') {
                            _state = State.TransitionNeeded;
                            string s = buffer.Slice(0, bufferFill).ToString();
                            if (s.Contains('.')) {
                                try {
                                    double value = double.Parse(s);
                                    result = new RealLiteral() {
                                        LineNumber = currentLine, StartIndex = startIndex, EndIndex = currentCursor - 1,
                                        Value = value, StringValue = s
                                    };
                                    return result;
                                } catch {
                                    throw new LexicalException(currentLine, startIndex, currentCursor - 1,
                                        $"Malformed real literal: \"{s}\"");
                                }
                            } else {
                                int value;
                                try {
                                    if (s.StartsWith("00")) {
                                        // Autodetect
                                        string substr = s.Substring(2);
                                        int baseNumber = 10;
                                        if (RegexBinary.IsMatch(substr)) {
                                            // Binary
                                            baseNumber = 2;
                                        } else if (RegexOctal.IsMatch(substr)) {
                                            // Octal
                                            baseNumber = 8;
                                        } else if (RegexDecimal.IsMatch(substr)) {
                                            baseNumber = 10;
                                        } else if (RegexHexadecimal.IsMatch(substr)) {
                                            baseNumber = 16;
                                        } else {
                                            throw new LexicalException(currentLine, startIndex, currentCursor - 1,
                                                $"Malformed integer literal \"{s}\"");
                                        }

                                        value = Convert.ToInt32(substr, baseNumber);
                                    } else if (s.StartsWith("0x")) {
                                        // Hexadecimal
                                        value = Convert.ToInt32(s.Substring(2), 16);
                                    } else if (s.StartsWith("0")) {
                                        // Octal
                                        value = Convert.ToInt32(s.Substring(1), 8);
                                    } else {
                                        // Decimal
                                        value = Convert.ToInt32(s);
                                    }
                                } catch (OverflowException) {
                                    throw new LexicalException(currentLine, startIndex, currentCursor - 1, $"Integer overflow \"{s}\"");
                                } catch(Exception e) {
                                    if (e is LexicalException) throw;
                                    throw new LexicalException(currentLine, startIndex, currentCursor-1, $"Malformed integer interal \"{s}\"");
                                }
                                

                                result = new IntegerLiteral() {
                                    LineNumber = currentLine, StartIndex = startIndex, EndIndex = currentCursor - 1,
                                    Value = value, StringValue = s
                                };
                                return result;
                            }
                        } else { }

                        break;
                    default:
                        throw new LexicalException(currentLine, startIndex, currentCursor, $"Illegal state");
                    case State.Comment:
                        AdvanceChar();
                        if (lastChar == '}') {
                            // End of comment
                            _state = State.TransitionNeeded;
                        }
                        break;
                }
            }
            return result;
        }
    }
}
