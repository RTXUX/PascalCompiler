using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PascalCompiler.Lexical;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax.Generator.Utils;
using PascalCompiler.Syntax;
using PascalCompiler.Syntax.TreeNode.Definition;

namespace SyntaxAnalyzerTest
{
    [TestClass]
    public class UtilsTest
    {
        [TestMethod]
        public void FirstAndFollowSetCalculation()
        {
            var aTerminal = new TerminalPredicate(makeIdentifierPredicate("a"));
            var bTerminal = new TerminalPredicate(makeIdentifierPredicate("b"));
            var dTerminal = new TerminalPredicate(makeIdentifierPredicate("d"));
            var gTerminal = new TerminalPredicate(makeIdentifierPredicate("g"));
            var hTerminal = new TerminalPredicate(makeIdentifierPredicate("h"));
            var nt_S = new KeyedNonTerminalPredicate("S");
            var nt_A = new KeyedNonTerminalPredicate("A");
            var nt_B = new KeyedNonTerminalPredicate("B");
            var nt_C = new KeyedNonTerminalPredicate("C");
            var productionRules = new List<ProductionRule>();
            productionRules.Add(new ProductionRule("S", typeof(SyntaxNode), new List<SyntaxPredicate>() {
                nt_A, nt_C, nt_B
            }));
            productionRules.Add(new ProductionRule("S", typeof(SyntaxNode), new List<SyntaxPredicate>() {
                nt_C, bTerminal, bTerminal
            }));
            productionRules.Add(new ProductionRule("S", typeof(SyntaxNode), new List<SyntaxPredicate>() {
                nt_B, aTerminal
            }));
            productionRules.Add(new ProductionRule("A", typeof(SyntaxNode), new List<SyntaxPredicate>() {dTerminal, aTerminal}));
            productionRules.Add(new ProductionRule("A", typeof(SyntaxNode), new List<SyntaxPredicate>() {nt_B, nt_C }));
            productionRules.Add(new ProductionRule("B", typeof(SyntaxNode), new List<SyntaxPredicate>(){gTerminal}));
            productionRules.Add(new ProductionRule("B", typeof(SyntaxNode), new List<SyntaxPredicate>() { EpsilonPredicate.Instance }));
            productionRules.Add(new ProductionRule("C", typeof(SyntaxNode), new List<SyntaxPredicate>() { hTerminal }));
            productionRules.Add(new ProductionRule("C", typeof(SyntaxNode), new List<SyntaxPredicate>() { EpsilonPredicate.Instance }));
            var first = CommonUtils.CalculateFirstSet(productionRules);
            Assert.AreEqual(4, first.Count);
            Assert.AreEqual(true, first["S"].SetEquals(new List<SyntaxPredicate>() { aTerminal, bTerminal, dTerminal, gTerminal, hTerminal, EpsilonPredicate.Instance }));
            Assert.AreEqual(true, first["A"].SetEquals(new List<SyntaxPredicate>() { dTerminal, gTerminal, hTerminal, EpsilonPredicate.Instance }));
            Assert.AreEqual(true, first["B"].SetEquals(new List<SyntaxPredicate>() { gTerminal, EpsilonPredicate.Instance }));
            Assert.AreEqual(true, first["C"].SetEquals(new List<SyntaxPredicate>() { hTerminal, EpsilonPredicate.Instance }));
            var follow = CommonUtils.CalculateFollowSet(productionRules, first);
            Assert.AreEqual(4, follow.Count);
            Assert.AreEqual(true, follow["S"].SetEquals(new List<SyntaxPredicate>()));
            Assert.AreEqual(true, follow["A"].SetEquals(new List<SyntaxPredicate>(){hTerminal, gTerminal}));
            Assert.AreEqual(true, follow["B"].SetEquals(new List<SyntaxPredicate>(){aTerminal, hTerminal, gTerminal}));
            Assert.AreEqual(true, follow["C"].SetEquals(new List<SyntaxPredicate>(){bTerminal, hTerminal, gTerminal}));
        }

        [TestMethod]
        public void ClosureTest() {
            var aTerminal = new TerminalPredicate(makeIdentifierPredicate("a"));
            var nt_S = new KeyedNonTerminalPredicate("S");
            var nt_A = new KeyedNonTerminalPredicate("A");
            var nt_B = new KeyedNonTerminalPredicate("B");
            var nt_C = new KeyedNonTerminalPredicate("C");
            var productionRules = new List<ProductionRule>();
            productionRules.Add(new ProductionRule("S", typeof(SyntaxNode), new List<SyntaxPredicate>() {nt_A, nt_B}));
            productionRules.Add(new ProductionRule("A", typeof(SyntaxNode), new List<SyntaxPredicate>() {nt_S}));
            productionRules.Add(new ProductionRule("A", typeof(SyntaxNode), new List<SyntaxPredicate>() { nt_C }));
            productionRules.Add(new ProductionRule("B", typeof(SyntaxNode), new List<SyntaxPredicate>() {nt_A}));
            productionRules.Add(new ProductionRule("C", typeof(SyntaxNode), new List<SyntaxPredicate>() {nt_S}));
            productionRules.Add(new ProductionRule("C", typeof(SyntaxNode), new List<SyntaxPredicate>() { aTerminal }));
            var d = CommonUtils.MakeProductionRuleDictionary(productionRules);
            var result =
                CommonUtils.Closure(new HashSet<Item>() {new Item() {ProductionRule = productionRules[0], Cursor = 1}},
                    d);
            Assert.AreEqual(7, result.Count);
        }

        [TestMethod]
        public void GeneratorTest() {
            var generator = new Generator(PascalDefinition.ProductionRules);
            var slr1table = generator.Generate(PascalDefinition.NonTerminalKey.Start);
            var driver = new Slr1Driver(slr1table);
            List<LexicalElement> lexicalElements = new List<LexicalElement>();
            using (var file = new FileStream("test_source1.txt", FileMode.Open)) {
                var reader = new StreamReader(file);
                var l = new LexerStateMachine(reader);
                l.AdvanceChar();
                LexicalElement le;
                while ((le = l.NextToken()) != null) {
                    if (le is LineFeedElement) continue;
                    lexicalElements.Add(le);
                }
            }
            var q = new Queue<LexicalElement>(lexicalElements);
            var treeRoot = driver.Parse(q,
                CommonUtils.Closure(
                    new HashSet<Item>() {new Item() {ProductionRule = PascalDefinition.ProductionRules[0], Cursor = 0}},
                    driver.ProductionDictionary), typeof(SNode));
            Assert.IsInstanceOfType(treeRoot, typeof(SNode));
        }

        [TestMethod]
        public void PrintTable() {
            var generator = new Generator(PascalDefinition.ProductionRules);
            var slr1table = generator.Generate(PascalDefinition.NonTerminalKey.Start);
            var stateIndexDict = new Dictionary<AnalyzerState, int>();
            var pdIndexDict = new Dictionary<ProductionRule, int>();
            int index = 0;
            foreach (var kv in slr1table.States) {
                stateIndexDict.Add(kv.Value, ++index);
            }

            index = 0;
            foreach (var rule in PascalDefinition.ProductionRules) {
                pdIndexDict.Add(rule, ++index);
            }
            index = 0;
            var terminalIndexDict = new Dictionary<TerminalPredicate, int>();
            foreach (var state in slr1table.States.Values) {
                foreach (var actionKey in state.Action.Keys) {
                    if (!terminalIndexDict.ContainsKey(actionKey)) {
                        terminalIndexDict.Add(actionKey, ++index);
                    }
                }
            }
            var gotoIndexDict = new Dictionary<object, int>();
            foreach (var state in slr1table.States.Values) {
                foreach (var key in state.GotoTable.Keys) {
                    if (gotoIndexDict.ContainsKey(key)) continue;
                    gotoIndexDict.Add(key, ++index);
                }
            }
            DataTable dt = new DataTable();
            for (int i = 0; i <= index; ++i) {
                dt.Columns.Add(i.ToString());
            }
            var dr = dt.NewRow();
            foreach (var kv in terminalIndexDict) {
                dr[kv.Value.ToString()] = kv.Key.Name;
            }
            foreach (var kv in gotoIndexDict) {
                dr[kv.Value.ToString()] = kv.Key.ToString();
            }

            dr["0"] = 0;
            dt.Rows.Add(dr);
            foreach (var state in slr1table.States.Values) {
                dr = dt.NewRow();
                dr[0] = stateIndexDict[state];
                foreach (var kv in state.Action) {
                    string s = "";
                    if (kv.Value is ShiftOperation so) {
                        s = $"s{stateIndexDict[so.NextState]}";
                    }

                    if (kv.Value is ReduceOperation ro) {
                        s = $"r{pdIndexDict[ro.ReduceBy]}";
                    }

                    dr[terminalIndexDict[kv.Key].ToString()] = s;
                }

                foreach (var kv in state.GotoTable) {
                    dr[gotoIndexDict[kv.Key].ToString()] = stateIndexDict[kv.Value];
                }

                dt.Rows.Add(dr);
            }
            XLWorkbook xb = new XLWorkbook();
            xb.Worksheets.Add(dt, "a");
            xb.SaveAs("Table.xlsx");
        }

        private static Predicate<LexicalElement> makeIdentifierPredicate(string id) {
            return (e) => (e is IdentifierElement ie && ie.Value == id);
        }
    }
}
