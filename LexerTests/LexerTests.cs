using Microsoft.VisualStudio.TestTools.UnitTesting;
using PascalCompiler.Lexical;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Lexical.Tests
{
    [TestClass()]
    public class LexerTests
    {
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
            
            Assert.IsTrue(true);
        }
    }
}