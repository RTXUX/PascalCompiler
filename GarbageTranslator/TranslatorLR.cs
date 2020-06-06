using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.Generator.Utils;
using PascalCompiler.Syntax.TreeNode.Definition;

namespace PascalCompiler.Translator.Garbage
{
    public class TranslatorLR : AbstractVisitor {
        public readonly List<string> Warnings = new List<string>();
        private readonly List<string> assignedSymbols = new List<string>();
        private int tempCount = 0;
        private int labelCount = 0;
        public readonly Dictionary<SyntaxNode, dynamic> properties = new Dictionary<SyntaxNode, dynamic>();
        private static readonly Dictionary<int, string> opMap = new Dictionary<int, string>()
        {
            [1] = "+",
            [2] = "-",
            [3] = "*",
            [4] = "/"
        };

        private string MakeTemp()
        {
            return $"<temp:{Interlocked.Increment(ref tempCount)}>";
        }

        private Label MakeLabel()
        {
            return new Label() { LabelId = Interlocked.Increment(ref labelCount) };
        }

        private CodeEntity MakeLabeledGoto(Label l)
        {
            return new CodeEntity() { Code = $"goto <label:{l.LabelId}>" };
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            return node == null ? null : (SyntaxNode)Visit((dynamic)node);
        }

        private SNode Visit(SNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            props.code = properties[node.CompoundStatement].code;
            return node;
        }

        private CompoundStatementNode Visit(CompoundStatementNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            props.code = properties[node.StatementsNode].code;
            return node;
        }

        private StatementsNodeVar1 Visit(StatementsNodeVar1 node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            props.code = properties[node.StatementNode].code;
            return node;
        }

        private StatementsNodeVar2 Visit(StatementsNodeVar2 node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            code.AddRange(properties[node.StatementsNode].code);
            code.AddRange(properties[node.StatementNode].code);
            return node;
        }

        private AssignStatementNode Visit(AssignStatementNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            code.AddRange(properties[node.ExpressionNode].code);
            code.Add(new CodeEntity() {Code = $"{node.Identifier} = {properties[node.ExpressionNode].addr}"});
            assignedSymbols.Add(node.Identifier);
            return node;
        }

        private StatementNode Visit(StatementNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            props.code = node.Next!=null ? properties[node.Next].code : new List<CodeEntry>();
            return node;
        }

        private WhileStatementNode Visit(WhileStatementNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            var begin = MakeLabel();
            var trueLabel = properties[node.BoolRelationExpression].trueLabel;
            var falseLabel = properties[node.BoolRelationExpression].falseLabel;
            code.Add(begin);
            code.AddRange(properties[node.BoolRelationExpression].code);
            code.Add(trueLabel);
            code.AddRange(properties[node.StatementNode].code);
            code.Add(MakeLabeledGoto(begin));
            code.Add(falseLabel);
            return node;
        }

        private IfStatementNode Visit(IfStatementNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            var trueLabel = properties[node.Condition].trueLabel;
            var falseLabel = properties[node.Condition].falseLabel;
            code.AddRange(properties[node.Condition].code);
            code.Add(trueLabel);
            code.AddRange(properties[node.StatementNode].code);
            code.Add(falseLabel);
            return node;
        }

        private IfElseStatementNode Visit(IfElseStatementNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            var trueLabel = properties[node.Condition].trueLabel;
            var falseLabel = properties[node.Condition].falseLabel;
            var nextLabel = MakeLabel();
            code.AddRange(properties[node.Condition].code);
            code.Add(trueLabel);
            code.AddRange(properties[node.StatementNode1].code);
            code.Add(MakeLabeledGoto(nextLabel));
            code.Add(falseLabel);
            code.AddRange(properties[node.StatementNode2].code);
            code.Add(nextLabel);
            return node;
        }

        private ForStatementUpNode Visit(ForStatementUpNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            var beginLabel = MakeLabel();
            var trueLabel = MakeLabel();
            var nextLabel = MakeLabel();
            var temp = MakeTemp();
            code.AddRange(properties[node.expr1].code);
            code.Add(new CodeEntity() {Code = $"{node.Identifier} = {properties[node.expr1].addr}"});
            code.Add(beginLabel);
            code.AddRange(properties[node.expr2].code);
            code.Add(new CodeEntity(){Code = $"if {node.Identifier} < {properties[node.expr2].addr} goto <label:{trueLabel.LabelId}>"});
            code.Add(MakeLabeledGoto(nextLabel));
            code.Add(trueLabel);
            code.AddRange(properties[node.StatementNode].code);
            code.Add(new CodeEntity() {Code = $"{temp} = {node.Identifier} + 1"});
            code.Add(new CodeEntity() { Code = $"{node.Identifier} = {temp}" });
            code.Add(MakeLabeledGoto(beginLabel));
            code.Add(nextLabel);
            return node;
        }

        private ForStatementDownNode Visit(ForStatementDownNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            var beginLabel = MakeLabel();
            var trueLabel = MakeLabel();
            var nextLabel = MakeLabel();
            var temp = MakeTemp();
            code.AddRange(properties[node.expr1].code);
            code.Add(new CodeEntity() { Code = $"{node.Identifier} = {properties[node.expr1].addr}" });
            code.Add(beginLabel);
            code.AddRange(properties[node.expr2].code);
            code.Add(new CodeEntity() { Code = $"if {node.Identifier} >= {properties[node.expr2].addr} goto <label:{trueLabel.LabelId}>" });
            code.Add(MakeLabeledGoto(nextLabel));
            code.Add(trueLabel);
            code.AddRange(properties[node.StatementNode].code);
            code.Add(new CodeEntity() { Code = $"{temp} = {node.Identifier} - 1" });
            code.Add(new CodeEntity() { Code = $"{node.Identifier} = {temp}" });
            code.Add(MakeLabeledGoto(beginLabel));
            code.Add(nextLabel);
            return node;
        }

        private BoolRelationExpressionNode Visit(BoolRelationExpressionNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            var trueLabel = MakeLabel();
            var falseLabel = MakeLabel();
            props.trueLabel = trueLabel;
            props.falseLabel = falseLabel;
            code.AddRange(properties[node.e1].code);
            code.AddRange(properties[node.e2].code);
            var op = node.op == 1 ? "<" : ">";
            code.Add(new CodeEntity() {Code = $"if {properties[node.e1].addr} {op} {properties[node.e2].addr} goto <label:{trueLabel.LabelId}>"});
            code.Add(MakeLabeledGoto(falseLabel));
            return node;
        }

        private ExpressionNode Visit(ExpressionNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            if (node.op == 6) {
                code.AddRange(properties[node.f].code);
                props.addr = properties[node.f].addr;
            } else if (node.op == 5) {
                var t = MakeTemp();
                code.AddRange(properties[node.e1].code);
                code.AddRange(properties[node.f].code);
                code.Add(new CodeEntity(){Code = $"{t} = {properties[node.e1].addr} ^ {properties[node.f].addr}"});
                props.addr = t;
            } else {
                var t = MakeTemp();
                code.AddRange(properties[node.e1].code);
                code.AddRange(properties[node.e2].code);
                code.Add(new CodeEntity() { Code = $"{t} = {properties[node.e1].addr} {opMap[node.op]} {properties[node.e2].addr}" });
                props.addr = t;
            }

            return node;
        }

        private FactorNode Visit(FactorNode node) {
            dynamic props = new ExpandoObject();
            properties.Add(node, props);
            var code = new List<CodeEntry>();
            props.code = code;
            switch (node.type) {
                case 1:
                    if (!assignedSymbols.Contains(node.Identifier)) {
                        var ele = (node.Child[0] as TerminalNode).Lex as IdentifierElement;
                        Warnings.Add($"{ele.LineNumber}:[{ele.StartIndex}, {ele.EndIndex}): Unassigned identifier {node.Identifier}");
                    }
                    props.addr = node.Identifier;
                    break;
                case 2:
                    props.addr = node.value;
                    break;
                case 3:
                    code.AddRange(properties[node.e1].code);
                    props.addr = properties[node.e1].addr;
                    break;
            }

            return node;
        }
    }
}
