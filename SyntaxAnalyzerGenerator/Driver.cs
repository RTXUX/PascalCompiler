using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        public SyntaxNode Parse(Queue<LexicalElement> input, HashSet<Item> startPoint, Type acceptNode, List<ParserConfiguration> history = null, List<SyntaxException> exceptions = null) {
            Stack<SyntaxNode> nodeStack = new Stack<SyntaxNode>();
            Stack<AnalyzerState> stateStack = new Stack<AnalyzerState>();
            stateStack.Push(Slr1Table.States[startPoint]);
            while (true) {
                try {
                    if (input.Count == 0) {
                        if (nodeStack.Count > 0 && nodeStack.Peek().GetType() == acceptNode) {
                            history?.Add(new ParserConfiguration(nodeStack, stateStack, input, null));
                            break;
                        }
                        var reducibleRules = new List<ProductionRule>(from item in stateStack.Peek().ItemSet
                            where item.Cursor == item.ProductionRule.Length
                            select item.ProductionRule);
                        if (reducibleRules.Count != 1) {
                            var l = FindRightMostTerminal(nodeStack.Peek());
                            throw new SyntaxException($"Multiple or No Reducible Rule, last token \"{l.StringValue}\" at {l.LineNumber}:[{l.StartIndex}, {l.EndIndex}). Expected {String.Join(", ", from pred in stateStack.Peek().Action.Keys select pred.Name)}");
                        }

                        var reducibleRule = reducibleRules[0];
                        history?.Add(new ParserConfiguration(nodeStack, stateStack, input, new ReduceOperation() { ReduceBy = reducibleRule }));
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
                                        history?.Add(new ParserConfiguration(nodeStack, stateStack, input, so));
                                        input.Dequeue();
                                        nodeStack.Push(new TerminalNode(cur));
                                        stateStack.Push(so.NextState);
                                    
                                        break;
                                    case ReduceOperation ro:
                                        var reducibleRule = ro.ReduceBy;
                                        history?.Add(new ParserConfiguration(nodeStack, stateStack, input, ro));
                                        var argNodes = new SyntaxNode[reducibleRule.Length];
                                        for (int i = argNodes.Length - 1; i >= 0; --i) {
                                            stateStack.Pop();
                                            argNodes[i] = nodeStack.Pop();
                                        }
                                        nodeStack.Push(reducibleRule.Produce(argNodes));
                                        stateStack.Push(stateStack.Peek().GotoTable[reducibleRule.Key]);
                                    
                                        break;
                                    default:
                                        throw new SyntaxException("Internal Error: Unknown Operation");
                                }
                            }
                        }
                        if (!matched) throw new SyntaxException($"Unknown Token \"{cur.StringValue}\" at {cur.LineNumber}:[{cur.StartIndex}, {cur.EndIndex}), no matching operation. Expected {String.Join(", ", from pred in stateStack.Peek().Action.Keys select pred.Name)}");
                    }
                } catch (SyntaxException e) {
                    exceptions?.Add(e);
                    var ero = new ErrorRecoveryOperation();
                    history?.Add(new ParserConfiguration(nodeStack, stateStack, input, ero));
                    if (input.Count == 0) {
                        var ex = new SyntaxException("Error Recovery Failed: Unable to Reduce");
                        ero.Success = false;
                        exceptions.Add(ex);
                        throw ex;
                    }
                    List<object> intersection = new List<object>();
                    intersection.AddRange(Slr1Table.AllowedErrorRecoveryKey.Keys.Intersect(stateStack.Peek().GotoTable.Keys));
                    while (intersection.Count == 0 && stateStack.Count>0) {
                        stateStack.Pop();
                        nodeStack.Pop();
                        intersection.AddRange(Slr1Table.AllowedErrorRecoveryKey.Keys.Intersect(stateStack.Peek().GotoTable.Keys));
                    }

                    if (intersection.Count == 0) {
                        var ex = new SyntaxException("Error Recovery Failed: No Recoverable State");
                        ero.Success = false;
                        exceptions.Add(ex);
                        throw ex;
                    }

                    var nextKey = intersection.First();
                    bool matched = false;
                    while (!matched && input.Count>0) {
                        foreach (var follow in Slr1Table.FollowSets[nextKey]) {
                            if (follow is TerminalPredicate tp && tp.Predicate(input.Peek())) {
                                matched = true;
                                break;
                            }

                            input.Dequeue();
                        }
                    }

                    if (!matched) {
                        var ex = new SyntaxException("Error Recovery Failed: No Followable Input");
                        ero.Success = false;
                        exceptions.Add(ex);
                        throw ex;
                    }
                    var nextState = stateStack.Peek().GotoTable[nextKey];
                    var nextNode = Slr1Table.AllowedErrorRecoveryKey[nextKey]();
                    stateStack.Push(nextState);
                    nodeStack.Push(nextNode);
                    ero.Success = true;
                }
            }
            return nodeStack.Pop();
        }

        private LexicalElement FindRightMostTerminal(SyntaxNode node) {
            var t = node;
            while (!(t is TerminalNode)) {
                t = node.Child[node.Child.Count - 1];
            }
            return ((TerminalNode) t).Lex;
        }


        
    }
    public class ParserConfiguration
    {
        public List<SyntaxNode> nodeStack;
        public List<AnalyzerState> stateStack;
        public List<LexicalElement> input;
        public AnalyzerOperation operation;

        public ParserConfiguration(Stack<SyntaxNode> nodeStack, Stack<AnalyzerState> stateStack, Queue<LexicalElement> input, AnalyzerOperation operation)
        {
            var nsa = new SyntaxNode[nodeStack.Count];
            nodeStack.CopyTo(nsa, 0);
            Array.Reverse(nsa);
            this.nodeStack = new List<SyntaxNode>(nsa);
            var ssa = new AnalyzerState[stateStack.Count];
            stateStack.CopyTo(ssa, 0);
            Array.Reverse(ssa);
            this.stateStack = new List<AnalyzerState>(ssa);
            var ia = new LexicalElement[input.Count];
            input.CopyTo(ia, 0);
            this.input = new List<LexicalElement>(ia);
            this.operation = operation;
        }
    }
}
