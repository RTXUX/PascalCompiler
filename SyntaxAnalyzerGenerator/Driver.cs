using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax.Generator.Utils;

namespace PascalCompiler.Syntax.Generator
{
    public class Slr1Driver {
        public Slr1Table Slr1Table { get; }
        public MultiValueDictionary<object, ProductionRule> ProductionDictionary { get; }

        public Slr1Driver(Slr1Table slr1Table) {
            this.Slr1Table = slr1Table;
            ProductionDictionary = CommonUtils.MakeProductionRuleDictionary(slr1Table.ProductionRules);
        }

        public SyntaxNode Parse(Queue<LexicalElement> input, HashSet<Item> startPoint, Type acceptNode) {
            Stack<SyntaxNode> nodeStack = new Stack<SyntaxNode>();
            Stack<AnalyzerState> stateStack = new Stack<AnalyzerState>();
            stateStack.Push(Slr1Table.States[startPoint]);
            while (true) {
                if (input.Count == 0) {
                   if (nodeStack.Count > 0 && nodeStack.Peek().GetType() == acceptNode) break;
                   var reducibleRules = new List<ProductionRule>(from item in stateStack.Peek().ItemSet
                       where item.Cursor == item.ProductionRule.Length
                       select item.ProductionRule);
                   if (reducibleRules.Count != 1) {
                       throw new SyntaxException();
                   }

                   var reducibleRule = reducibleRules[0];
                   var argNodes = new SyntaxNode[reducibleRule.Length];
                   for (int i = argNodes.Length-1; i >= 0; --i) {
                       stateStack.Pop();
                       argNodes[i] = nodeStack.Pop();
                   }
                   nodeStack.Push(reducibleRule.Produce(argNodes));
                   stateStack.Push(stateStack.Peek().GotoTable[reducibleRule.Key]);
                } else {
                    var cur = input.Peek();
                    bool matched = false;
                    foreach (var action in stateStack.Peek().Action) {
                        if (action.Key.Predicate(cur)) {
                            matched = true;
                            var operation = action.Value;
                            switch (operation) {
                                case ShiftOperation so:
                                    input.Dequeue();
                                    nodeStack.Push(new TerminalNode(cur));
                                    stateStack.Push(so.NextState);
                                    break;
                                case ReduceOperation ro:
                                    var reducibleRule = ro.ReduceBy;
                                    var argNodes = new SyntaxNode[reducibleRule.Length];
                                    for (int i = argNodes.Length - 1; i >= 0; --i) {
                                        stateStack.Pop();
                                        argNodes[i] = nodeStack.Pop();
                                    }
                                    nodeStack.Push(reducibleRule.Produce(argNodes));
                                    stateStack.Push(stateStack.Peek().GotoTable[reducibleRule.Key]);
                                    break;
                                default:
                                    throw new SyntaxException();
                            }
                        }
                    }
                    if (!matched) throw new SyntaxException($"Unknown Token {cur.StringValue} at {cur.LineNumber}:[{cur.StartIndex}, {cur.EndIndex})");
                }
            }
            return nodeStack.Pop();
        }
    }
}
