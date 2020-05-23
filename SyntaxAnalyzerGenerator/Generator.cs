using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using PascalCompiler.Syntax.Generator.Utils;

namespace PascalCompiler.Syntax.Generator
{
    public class Generator {
        public readonly MultiValueDictionary<object, ProductionRule> ProductionDict;
        public readonly Dictionary<object, HashSet<SyntaxPredicate>> FirstSets;
        public readonly Dictionary<object, HashSet<SyntaxPredicate>> FollowSets;
        public readonly Dictionary<HashSet<Item>, AnalyzerState> ItemSets;
        
        // Grammar must be already augmented.
        public Generator(List<ProductionRule> productionRules) {
            ProductionDict = CommonUtils.MakeProductionRuleDictionary(productionRules);
            FirstSets = CommonUtils.CalculateFirstSet(productionRules);
            FollowSets = CommonUtils.CalculateFollowSet(productionRules, FirstSets);
            ItemSets = new Dictionary<HashSet<Item>, AnalyzerState>(HashSet<Item>.CreateSetComparer());
        }

        public void GenerateInternal(AnalyzerState state, int priority, AssociativityType associativity) {
            var shifts = new MultiValueDictionary<TerminalPredicate, Item>();
            var reduces = new Dictionary<TerminalPredicate, ProductionRule>();
            var gotos = new MultiValueDictionary<object, Item>();
            foreach (var item in state.ItemSet) {
                if (item.Cursor < item.ProductionRule.Length) {
                    switch (item.ProductionRule.Predicates[item.Cursor]) {
                        case TerminalPredicate tp:
                            shifts.Add(tp, Item.Advance(item));
                            break;
                        case KeyedNonTerminalPredicate kp:
                            gotos.Add(kp.Key, Item.Advance(item));
                            break;
                        default:
                            throw new GeneratorException();
                    }
                } else {
                    var follow = FollowSets[item.ProductionRule.Key];
                    foreach (var syntaxPredicate in follow.OfType<TerminalPredicate>()) {
                        if (reduces.ContainsKey(syntaxPredicate)) {
                            throw new GeneratorException("Reduce-reduce conflict");
                        }
                        reduces.Add(syntaxPredicate, item.ProductionRule);
                    }
                }
            }
            var s_r_conflict = new List<TerminalPredicate>(shifts.Keys.Intersect(reduces.Keys));
            foreach (var terminalPredicate in s_r_conflict) {
                if (terminalPredicate.Precedence > priority) {
                    state.Action.Add(terminalPredicate, new ShiftOperation() {NextState = ResolveAnalyzerState(CommonUtils.Closure(new HashSet<Item>(shifts[terminalPredicate]), ProductionDict), terminalPredicate.Precedence, terminalPredicate.Associativity) });
                    continue;
                } else if (terminalPredicate.Precedence == priority) {
                    if (terminalPredicate.Associativity == AssociativityType.Right) {
                        state.Action.Add(terminalPredicate, new ShiftOperation() { NextState = ResolveAnalyzerState(CommonUtils.Closure(new HashSet<Item>(shifts[terminalPredicate]), ProductionDict), terminalPredicate.Precedence, terminalPredicate.Associativity) });
                        continue;
                    }
                }
                state.Action.Add(terminalPredicate, new ReduceOperation() {ReduceBy = reduces[terminalPredicate]});
            }

            foreach (var shift in shifts.Keys.Except(s_r_conflict)) {
                state.Action.Add(shift, new ShiftOperation() { NextState = ResolveAnalyzerState(CommonUtils.Closure(new HashSet<Item>(shifts[shift]), ProductionDict), shift.Precedence, shift.Associativity)});
            }

            foreach (var predicate in reduces.Keys.Except(s_r_conflict)) {
                state.Action.Add(predicate, new ReduceOperation() {ReduceBy = reduces[predicate]});
            }

            foreach (var key in gotos) {
                state.GotoTable.Add(key.Key, ResolveAnalyzerState(CommonUtils.Closure(new HashSet<Item>(key.Value), ProductionDict), priority, associativity));
            }

        }

        public Slr1Table Generate(object startKey) {
            var table = new Slr1Table();
            var itemSet = new HashSet<Item>();
            foreach (var productionRule in ProductionDict[startKey]) {
                itemSet.Add(new Item() {ProductionRule = productionRule, Cursor = 0});
            }
            itemSet = CommonUtils.Closure(itemSet, ProductionDict);
            var state = new AnalyzerState() {ItemSet = itemSet};
            ItemSets.Add(itemSet, state);
            GenerateInternal(state, 0, AssociativityType.Left);
            table.ProductionRules = new List<ProductionRule>();
            foreach (var pl in ProductionDict) {
                foreach (var productionRule in pl.Value) {
                    table.ProductionRules.Add(productionRule);
                }
            }
            table.States = ItemSets;
            return table;
        }

        private AnalyzerState ResolveAnalyzerState(HashSet<Item> itemSet, int precedence, AssociativityType associativity) {
            if (ItemSets.TryGetValue(itemSet, out var state)) {
                return state;
            }
            state = new AnalyzerState() {ItemSet = itemSet};
            ItemSets.Add(itemSet, state);
            GenerateInternal(state, precedence, associativity);
            return state;
        }

    }
}
