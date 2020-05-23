using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PascalCompiler.Lexical.Definition;

namespace PascalCompiler.Syntax.Generator
{
    public class SyntaxNode {
        // Binary Linked List Presentation of Tree
        public List<SyntaxNode> Child { get; set; } = new List<SyntaxNode>();
        public virtual String Tag { get; set; }

        public SyntaxNode() {}

        public SyntaxNode(SyntaxNode[] argNodes) {
            Child.AddRange(argNodes);
        }
    }

    public class TerminalNode : SyntaxNode {
        public LexicalElement Lex { get; }

        public TerminalNode(LexicalElement lex) {
            Lex = lex;
        }
    }

    public abstract class SyntaxPredicate { }

    public sealed class TerminalPredicate : SyntaxPredicate {
        public Predicate<LexicalElement> Predicate { get; }
        public int Precedence { get; set; } = 0;
        public AssociativityType Associativity { get; set; } = AssociativityType.Left;
        public string Name { get; set; }
        public TerminalPredicate(Predicate<LexicalElement> predicate) {
            this.Predicate = predicate;
        }

        public override string ToString() {
            return Name;
        }
    }

    public enum AssociativityType {
        Left, Right
    }

    public sealed class KeyedNonTerminalPredicate : SyntaxPredicate {
        public object Key { get; }

        public KeyedNonTerminalPredicate(object key) {
            this.Key = key;
        }

        private bool Equals(KeyedNonTerminalPredicate other) {
            return Equals(Key, other.Key);
        }

        public override bool Equals(object obj) {
            return ReferenceEquals(this, obj) || obj is KeyedNonTerminalPredicate other && Equals(other);
        }

        public override int GetHashCode() {
            return (Key != null ? Key.GetHashCode() : 0);
        }

        public override string ToString() {
            return Key.ToString();
        }
    }

    public sealed class EpsilonPredicate : SyntaxPredicate {
        private EpsilonPredicate() {}
        private static readonly EpsilonPredicate _instance = new EpsilonPredicate();
        public static EpsilonPredicate Instance => _instance;

        public static IEnumerable<SyntaxPredicate> Enumerator() {
            yield return Instance;
        }

        public override string ToString() {
            return "ε";
        }
    }

    public class ProductionRule {
        // Right Side of Production Rule
        public virtual List<SyntaxPredicate> Predicates { get; set; }
        // Type of Left Side
        public virtual Type LeftType { get; }

        private int? _length;

        public virtual int Length {
            get => _length ?? (from syntaxPredicate in Predicates where !(syntaxPredicate is EpsilonPredicate) select syntaxPredicate).Count();
            set => _length = value;
        }

        public virtual object Key { get; }

        public Func<SyntaxNode[], SyntaxNode> Produce { get; set; }

        public ProductionRule(object key, Type leftType,  List<SyntaxPredicate> predicates) {
            Predicates = predicates;
            Key = key;
            this.LeftType = leftType;
            Produce = nodes => {
                return (SyntaxNode) Activator.CreateInstance(leftType, new object[] {nodes});
            };
        }


        public ProductionRule(object key, Type leftType) : this(key, leftType, new List<SyntaxPredicate>()) { }
    }

    public struct Item {
        public ProductionRule ProductionRule;
        public int Cursor;

        public static Item Advance(in Item item) {
            return new Item() {ProductionRule = item.ProductionRule, Cursor = item.Cursor+1};
        }

        public override string ToString() {
            string[] t = new string[ProductionRule.Length+1];
            int index = 0;
            for (int i = 0; i < Cursor; ++i) {
                t[index++] = ProductionRule.Predicates[i].ToString();
            }
            t[index++] = "·";
            for (int i = Cursor; i < ProductionRule.Length; ++i) {
                t[index++] = ProductionRule.Predicates[i].ToString();
            }
            return String.Join(" ", t);
        }
    }

    public abstract class AnalyzerOperation {}

    public class AnalyzerState {
        public ISet<Item> ItemSet { get; set; }
        public Dictionary<TerminalPredicate, AnalyzerOperation> Action { get; } = new Dictionary<TerminalPredicate, AnalyzerOperation>();
        public Dictionary<object, AnalyzerState> GotoTable { get; } = new Dictionary<object, AnalyzerState>();
    }

    public sealed class ShiftOperation : AnalyzerOperation {
        public AnalyzerState NextState { get; set; }
    }

    public sealed class ReduceOperation : AnalyzerOperation {
        public ProductionRule ReduceBy { get; set; }
    }

    public class Slr1Table {
        public List<ProductionRule> ProductionRules { get; set; }
        public IReadOnlyDictionary<HashSet<Item>, AnalyzerState> States { get; set; }
    }

    public class SyntaxException : Exception {
        public SyntaxException() { }
        public SyntaxException(string message) : base(message) { }
        public SyntaxException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class GeneratorException : Exception {
        public GeneratorException() { }
        public GeneratorException(string message) : base(message) { }
        public GeneratorException(string message, Exception innerException) : base(message, innerException) { }
    }
}
