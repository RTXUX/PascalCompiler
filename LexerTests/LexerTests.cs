using Microsoft.VisualStudio.TestTools.UnitTesting;
using PascalCompiler.Lexical;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Lexical.Tests
{
    [TestClass()]
    public class LexerTests
    {
        private static Stream StringToMemoryStream(string s) {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        } 

        [TestMethod()]
        public void LexTest()
        {
            /*var l = new Lexer();
            LinkedList<LexicalElement> result;
            using (var fileStream = new FileStream("source.txt", FileMode.Open)) {
                result = l.Lex(new StreamReader(fileStream));
            }*/
            LinkedList<LexicalElement> res = new LinkedList<LexicalElement>();
            using (var fs = new FileStream("source.txt", FileMode.Open)) {
                using (var ss = new StreamReader(fs)) {
                    var l = new LexerStateMachine(ss);
                    l.AdvanceChar();
                    LexicalElement le;
                    while ((le = l.NextToken()) != null) {
                        res.AddLast(le);
                    }
                }
            }
            Assert.AreEqual(44, res.Count);
        }

        [TestMethod()]
        public void StringEscapeTest1() {
            LexicalElement a;
            using (var ms = StringToMemoryStream("\"abc\\nab\\\"\"")) {
                using (var reader = new StreamReader(ms)) {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    a = l.NextToken();
                }
            }
            Assert.IsInstanceOfType(a, typeof(StringLiteral));
            Assert.AreEqual("abc\nab\"", (a as StringLiteral).Value);
        }

        [TestMethod()]
        [ExpectedException(typeof(LexicalException))]
        public void StringMalformedTest1()
        {
            LexicalElement a;
            using (var ms = StringToMemoryStream("\"abc\\nab\\\""))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    a = l.NextToken();
                }
            }
        }

        [TestMethod()]
        public void NumberTest() {
            LexicalElement a;
            List<LexicalElement> res = new List<LexicalElement>();
            using (var ms = StringToMemoryStream("123 0123 00101 00125 00768 002f1 0x123 1.234"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    while ((a = l.NextToken()) != null) {
                        res.Add(a);
                    }
                }
            }
            Assert.AreEqual(8, res.Count);
            Assert.AreEqual(123, (res[0] as IntegerLiteral).Value);
            Assert.AreEqual(83, (res[1] as IntegerLiteral).Value);
            Assert.AreEqual(0b101, (res[2] as IntegerLiteral).Value);
            Assert.AreEqual(85, (res[3] as IntegerLiteral).Value);
            Assert.AreEqual(768, (res[4] as IntegerLiteral).Value);
            Assert.AreEqual(0x2f1, (res[5] as IntegerLiteral).Value);
            Assert.AreEqual(0x123, (res[6] as IntegerLiteral).Value);
            Assert.AreEqual(1.234, (res[7] as RealLiteral).Value);
        }

        [TestMethod()]
        public void MalformedNumberTest() {
            LexicalElement a;
            List<LexicalElement> res = new List<LexicalElement>();
            using (var ms = StringToMemoryStream("0012k"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.ThrowsException<LexicalException>(() => {
                        l.NextToken();
                    });
                }
            }
            using (var ms = StringToMemoryStream("0123f"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.ThrowsException<LexicalException>(() => {
                        l.NextToken();
                    });
                }
            }
            using (var ms = StringToMemoryStream("0x12k"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.ThrowsException<LexicalException>(() => {
                        l.NextToken();
                    });
                }
            }
            using (var ms = StringToMemoryStream("0.213.213"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.ThrowsException<LexicalException>(() => {
                        l.NextToken();
                    });
                }
            }
            using (var ms = StringToMemoryStream("28743685636583658698"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.ThrowsException<LexicalException>(() => {
                        l.NextToken();
                    });
                }
            }
        }
        
        [TestMethod()]
        public void CommentTest() {
            using (var ms = StringToMemoryStream("{ Fuck }"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.IsNull(l.NextToken());
                }
            }
            using (var ms = StringToMemoryStream("{ Fuck \n Fuck }"))
            {
                using (var reader = new StreamReader(ms))
                {
                    var l = new LexerStateMachine(reader);
                    l.AdvanceChar();
                    Assert.IsNull(l.NextToken());
                }
            }
        }
    }
}