using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.TreeNode.Definition;

namespace PascalCompiler.Syntax
{
    public class PascalDefinition {
        public static Dictionary<Enum, TerminalPredicate> TerminalPredicates = new Dictionary<Enum, TerminalPredicate>();

        public static TerminalPredicate IdentifierPredicate = new TerminalPredicate(e => e is IdentifierElement) {Name = "id"};

        public static TerminalPredicate IntegerPredicate = new TerminalPredicate(e => e is IntegerLiteral) {Name = "num"};

        public static TerminalPredicate RealPredicate = new TerminalPredicate(e => e is RealLiteral);

        public static TerminalPredicate StringPredicate = new TerminalPredicate(e => e is StringLiteral);

        public static List<ProductionRule> ProductionRules = new List<ProductionRule>();

        public static Dictionary<NonTerminalKey, KeyedNonTerminalPredicate> NonTerminalPredicates =
            new Dictionary<NonTerminalKey, KeyedNonTerminalPredicate>();

        public enum NonTerminalKey {
            Start, S, CompoundStatement, Statements, Statement, IfStatement, ForStatement, BoolExpression, Expression, Factor
        }

        static PascalDefinition() {
            foreach (NonwordType nt in Enum.GetValues(typeof(NonwordType))) {
                TerminalPredicates.Add(nt, MakeOperatorPredicate(nt));
            }
            foreach (KeywordType kt in Enum.GetValues(typeof(KeywordType)))
            {
                TerminalPredicates.Add(kt, MakeKeywordPredicate(kt));
            }

            TerminalPredicates[KeywordType.Else].Associativity = AssociativityType.Right;
            TerminalPredicates[NonwordType.Multiplication].Precedence = 1;
            TerminalPredicates[NonwordType.Division].Precedence = 1;
            TerminalPredicates[NonwordType.Caret].Precedence = 2;
            foreach (NonTerminalKey key in Enum.GetValues(typeof(NonTerminalKey))) {
                NonTerminalPredicates.Add(key, new KeyedNonTerminalPredicate(key));
            }
            InitializeProductionRules();
        }

        private static void InitializeProductionRules() {
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Start, typeof(SyntaxNode), new List<SyntaxPredicate>() {NonTerminalPredicates[NonTerminalKey.S]}));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.S, typeof(SNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.Program], IdentifierPredicate, TerminalPredicates[NonwordType.Semicolon], NonTerminalPredicates[NonTerminalKey.CompoundStatement], TerminalPredicates[NonwordType.Dot]
            }) {Produce = (nodes => new SNode(nodes))});
            ProductionRules.Add(new ProductionRule(NonTerminalKey.CompoundStatement, typeof(CompoundStatementNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.Begin], NonTerminalPredicates[NonTerminalKey.Statements], TerminalPredicates[KeywordType.End]
            }) {Produce = nodes => new CompoundStatementNode(nodes)});
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statements, typeof(StatementsNodeVar1), new List<SyntaxPredicate>() { NonTerminalPredicates[NonTerminalKey.Statement] }) {Produce = nodes => new StatementsNodeVar1(nodes)});
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statements, typeof(StatementsNodeVar2), new List<SyntaxPredicate>() { NonTerminalPredicates[NonTerminalKey.Statements], TerminalPredicates[NonwordType.Semicolon], NonTerminalPredicates[NonTerminalKey.Statement] }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statement, typeof(AssignStatementNode), new List<SyntaxPredicate>() {
                IdentifierPredicate, TerminalPredicates[NonwordType.Assign], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statement, typeof(StatementNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.CompoundStatement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statement, typeof(StatementNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.IfStatement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statement, typeof(StatementNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.ForStatement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statement, typeof(WhileStatementNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.While], NonTerminalPredicates[NonTerminalKey.BoolExpression], TerminalPredicates[KeywordType.Do], NonTerminalPredicates[NonTerminalKey.Statement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Statement, typeof(StatementNode), new List<SyntaxPredicate>() { EpsilonPredicate.Instance }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.IfStatement, typeof(IfStatementNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.If], NonTerminalPredicates[NonTerminalKey.BoolExpression], TerminalPredicates[KeywordType.Then], NonTerminalPredicates[NonTerminalKey.Statement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.IfStatement, typeof(IfElseStatementNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.If], NonTerminalPredicates[NonTerminalKey.BoolExpression], TerminalPredicates[KeywordType.Then], NonTerminalPredicates[NonTerminalKey.Statement],
                TerminalPredicates[KeywordType.Else], NonTerminalPredicates[NonTerminalKey.Statement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.ForStatement, typeof(ForStatementUpNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.For], IdentifierPredicate, TerminalPredicates[NonwordType.Assign], NonTerminalPredicates[NonTerminalKey.Expression],
                TerminalPredicates[KeywordType.To], NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[KeywordType.Do], NonTerminalPredicates[NonTerminalKey.Statement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.ForStatement, typeof(ForStatementDownNode), new List<SyntaxPredicate>() {
                TerminalPredicates[KeywordType.For], IdentifierPredicate, TerminalPredicates[NonwordType.Assign], NonTerminalPredicates[NonTerminalKey.Expression],
                TerminalPredicates[KeywordType.Downto], NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[KeywordType.Do], NonTerminalPredicates[NonTerminalKey.Statement]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.BoolExpression, typeof(BoolRelationExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Less], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.BoolExpression, typeof(BoolRelationExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Greater], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Expression, typeof(ExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Addition], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Expression, typeof(ExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Subtraction], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Expression, typeof(ExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Multiplication], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Expression, typeof(ExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Division], NonTerminalPredicates[NonTerminalKey.Expression]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Expression, typeof(ExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.Caret], NonTerminalPredicates[NonTerminalKey.Factor]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Expression, typeof(ExpressionNode), new List<SyntaxPredicate>() {
                NonTerminalPredicates[NonTerminalKey.Factor]
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Factor, typeof(FactorNode), new List<SyntaxPredicate>() {
                IdentifierPredicate
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Factor, typeof(FactorNode), new List<SyntaxPredicate>() {
                IntegerPredicate
            }));
            ProductionRules.Add(new ProductionRule(NonTerminalKey.Factor, typeof(FactorNode), new List<SyntaxPredicate>() {
                TerminalPredicates[NonwordType.LeftParentheses], NonTerminalPredicates[NonTerminalKey.Expression], TerminalPredicates[NonwordType.RightParentheses]
            }));
        }


        public static TerminalPredicate MakeOperatorPredicate(NonwordType nt) {
            return new TerminalPredicate((e) => e is NonwordElement ne && ne.Type==nt) {Name = Mappings.NonwordToStringMap[nt]};
        }

        public static TerminalPredicate MakeKeywordPredicate(KeywordType kt) {
            return new TerminalPredicate((e) => e is KeywordElement ke && ke.Type==kt) {Name = kt.ToString()};
        }
    }
}
