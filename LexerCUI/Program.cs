using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PascalCompiler.Lexical;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Lexical.CUI
{
    class Program
    {
        static void Main(string[] args) {
            LinkedList<LexicalElement> result = new LinkedList<LexicalElement>();
            LinkedList<KeyValuePair<string, IdentifierElement>> symbolTable = new LinkedList<KeyValuePair<string, IdentifierElement>>();
            if (args.Length != 1) {
                var ms = new MemoryStream();
                var writer = new StreamWriter(ms);
                int ch;
                while ((ch = Console.Read()) != -1) {
                    writer.Write(Convert.ToChar(ch));
                }
                writer.Flush();
                ms.Seek(0, SeekOrigin.Begin);
            } else {
                string filename = args[0];
                try
                {
                    using (var fs = new FileStream(filename, FileMode.Open))
                    {
                        using (var ss = new StreamReader(fs))
                        {
                            LexerStateMachine l = new LexerStateMachine(ss);
                            l.AdvanceChar();
                            LexicalElement le;
                            while ((le = l.NextToken()) != null)
                            {
                                result.AddLast(le);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(e.Message);
                    return;
                }
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
                        Console.Write($"<id, {ie.Value}> ");
                        break;
                    case StringLiteral sl:
                        Console.Write($"<string, \"{sl.StringValue}\" ");
                        break;
                    case IntegerLiteral il:
                        Console.Write($"<num, {il.Value}> ");
                        break;
                    case RealLiteral rl:
                        Console.Write($"<real, {rl.Value}> ");
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
