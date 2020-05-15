using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PascalCompiler.Lexical;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Lexical.CUI
{
    class Program
    {
        static void Main(string[] args) {
            if (args.Length != 1) {
                Console.WriteLine("Need exactly one argument: File path to analyze");
                return;
            }

            string filename = args[0];
            LinkedList<LexicalElement> result = new LinkedList<LexicalElement>();
            LinkedList<KeyValuePair<string, IdentifierElement>> symbolTable = new LinkedList<KeyValuePair<string, IdentifierElement>>();
            try {
                using (var fs = new FileStream(filename, FileMode.Open)) {
                    using (var ss = new StreamReader(fs)) {
                        LexerStateMachine l = new LexerStateMachine(ss);
                        l.AdvanceChar();
                        LexicalElement le;
                        while ((le = l.NextToken()) != null) {
                            result.AddLast(le);
                        }
                    }
                }
            } catch(Exception e) {
                Console.WriteLine("Error:");
                Console.WriteLine(e.Message);
                return;
            }
            BuildSymbolTable(symbolTable, result);
            PrintLexeme(result);
            Console.WriteLine();
            PrintSymbolTable(symbolTable);
            return;
        }

        static void BuildSymbolTable(LinkedList<KeyValuePair<string, IdentifierElement>> symbolTable,
            LinkedList<LexicalElement> result) {
            foreach (var le in result)
            {
                if (le is IdentifierElement ie)
                {
                    bool found = false;
                    foreach (var sym in symbolTable)
                    {
                        if (sym.Key == ie.Value)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        symbolTable.AddLast(new KeyValuePair<string, IdentifierElement>(ie.Value, ie));
                    }
                }
            }
        }

        static void PrintLexeme(LinkedList<LexicalElement> result) {
            Console.WriteLine("Lexemes:");
            foreach (var le in result) {
                switch (le) {
                    case IdentifierElement ie:
                        Console.Write($"<id:{ie.Value}> ");
                        break;
                    case StringLiteral sl:
                        Console.Write($"<str:\"{sl.StringValue}\" ");
                        break;
                    case IntegerLiteral il:
                        Console.Write($"<num:{il.Value}> ");
                        break;
                    case RealLiteral rl:
                        Console.Write($"<real:{rl.Value}> ");
                        break;
                    case LineFeedElement lfe:
                        string s;
                        switch (lfe.StringValue) {
                            case "\r\n":
                                s = "CRLF";
                                break;
                            case "\r":
                                s = "CR";
                                break;
                            default:
                                s = "LF";
                                break;
                        }
                        Console.Write($"<{s}> ");
                        break;
                    default:
                        Console.Write($"<{le.StringValue}> ");
                        break;
                }
            }
            Console.WriteLine();
        }

        static void PrintSymbolTable(LinkedList<KeyValuePair<string, IdentifierElement>> symbolTable) {
            Console.WriteLine("Symbol Table:");
            foreach (var sym in symbolTable) {
                Console.WriteLine($"Identifier: \"{sym.Key}\", first appear at Line {sym.Value.LineNumber}, [{sym.Value.StartIndex}, {sym.Value.EndIndex})");
            }
            Console.WriteLine();
        }
    }
}
