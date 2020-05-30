using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.TreeNode.Definition;

namespace PascalCompiler.Translator.Garbage
{
    public abstract class CodeEntry {}

    public sealed class CodeEntity : CodeEntry {
        public string Code { get; set; }
    }

    public sealed class Label : CodeEntry {
        public int LabelId { get; set; }
    }

    public class Translator : AbstractVisitor {

        private int tempCount = 0;
        private int labelCount = 0;
        public readonly List<CodeEntry> generatedCode = new List<CodeEntry>();
        private readonly Dictionary<SyntaxNode, dynamic> properties = new Dictionary<SyntaxNode, dynamic>();
        private static readonly Dictionary<int, string> opMap = new Dictionary<int, string>() {
            [1]="+",
            [2]="-",
            [3]="*",
            [4]="/"
        };

        public override SyntaxNode Visit(SyntaxNode node) {
            return node == null ? null : (SyntaxNode) Visit((dynamic) node);
        }

        private string MakeTemp() {
            return $"<temp:{Interlocked.Increment(ref tempCount)}>";
        }

        private Label MakeLabel() {
            return new Label() {LabelId = Interlocked.Increment(ref labelCount)};
        }

        private CodeEntity MakeLabeledGoto(Label l) {
            return new CodeEntity() {Code = $"goto <label:{l.LabelId}>"};
        }

        private SNode Visit(SNode node) {
            node.CompoundStatement.Visit(this);
            return node;
        }

        private CompoundStatementNode Visit(CompoundStatementNode node) {
            node.StatementsNode.Visit(this);
            return node;
        }

        private StatementsNodeVar1 Visit(StatementsNodeVar1 node) {
            node.StatementNode.Visit(this);
            return node;
        }

        private StatementsNodeVar2 Visit(StatementsNodeVar2 node) {
            node.StatementNode.Visit(this);
            node.StatementsNode.Visit(this);
            return node;
        }

        private AssignStatementNode Visit(AssignStatementNode node) {
            node.ExpressionNode.Visit(this);
            generatedCode.Add(new CodeEntity() {Code = $"{node.Identifier} = {properties[node.ExpressionNode].addr}"});
            return node;
        }

        private StatementNode Visit(StatementNode node) {
            node.Next?.Visit(this);
            return node;
        }

        private WhileStatementNode Visit(WhileStatementNode node) {
            var begin = MakeLabel();
            var body = MakeLabel();
            var next = MakeLabel();
            dynamic BoolProp = new ExpandoObject();
            properties.Add(node.BoolRelationExpression, BoolProp);
            BoolProp.trueLabel = body;
            BoolProp.falseLabel = next;
            generatedCode.Add(begin);
            node.BoolRelationExpression.Visit(this);
            generatedCode.Add(body);
            node.StatementNode.Visit(this);
            generatedCode.Add(MakeLabeledGoto(begin));
            generatedCode.Add(next);
            return node;
        }

        private IfStatementNode Visit(IfStatementNode node) {
            var trueLabel = MakeLabel();
            var nextLabel = MakeLabel();
            dynamic BoolProp = new ExpandoObject();
            properties.Add(node.Condition, BoolProp);
            BoolProp.trueLabel = trueLabel;
            BoolProp.falseLabel = nextLabel;
            node.Condition.Visit(this);
            generatedCode.Add(trueLabel);
            node.StatementNode.Visit(this);
            generatedCode.Add(nextLabel);
            return node;
        }

        private IfElseStatementNode Visit(IfElseStatementNode node) {
            var trueLabel = MakeLabel();
            var falseLabel = MakeLabel();
            var nextLabel = MakeLabel();
            dynamic BoolProp = new ExpandoObject();
            properties.Add(node.Condition, BoolProp);
            BoolProp.trueLabel = trueLabel;
            BoolProp.falseLabel = falseLabel;
            node.Condition.Visit(this);
            generatedCode.Add(trueLabel);
            node.StatementNode1.Visit(this);
            generatedCode.Add(MakeLabeledGoto(nextLabel));
            generatedCode.Add(falseLabel);
            node.StatementNode2.Visit(this);
            generatedCode.Add(nextLabel);
            return node;
        }

        private ForStatementUpNode Visit(ForStatementUpNode node) {
            var beginLabel = MakeLabel();
            var trueLabel = MakeLabel();
            var nextLabel = MakeLabel();
            node.expr1.Visit(this);
            generatedCode.Add(new CodeEntity() {Code = $"{node.Identifier} = {properties[node.expr1].addr}"});
            generatedCode.Add(beginLabel);
            node.expr2.Visit(this);
            generatedCode.Add(new CodeEntity() {Code = $"if {node.Identifier} < {properties[node.expr2].addr} goto <label:{trueLabel.LabelId}>"});
            generatedCode.Add(MakeLabeledGoto(nextLabel));
            generatedCode.Add(trueLabel);
            node.StatementNode.Visit(this);
            var temp = MakeTemp();
            generatedCode.Add(new CodeEntity() {Code = $"{temp} = {node.Identifier} + 1"});
            generatedCode.Add(new CodeEntity() {Code = $"{node.Identifier} = {temp}"});
            generatedCode.Add(MakeLabeledGoto(beginLabel));
            generatedCode.Add(nextLabel);
            return node;
        }

        private ForStatementDownNode Visit(ForStatementDownNode node) {
            var beginLabel = MakeLabel();
            var trueLabel = MakeLabel();
            var nextLabel = MakeLabel();
            node.expr1.Visit(this);
            generatedCode.Add(new CodeEntity() { Code = $"{node.Identifier} = {properties[node.expr1].addr}" });
            generatedCode.Add(beginLabel);
            node.expr2.Visit(this);
            generatedCode.Add(new CodeEntity() { Code = $"if {node.Identifier} >= {properties[node.expr2].addr} goto <label:{trueLabel.LabelId}>" });
            generatedCode.Add(MakeLabeledGoto(nextLabel));
            generatedCode.Add(trueLabel);
            node.StatementNode.Visit(this);
            var temp = MakeTemp();
            generatedCode.Add(new CodeEntity() { Code = $"{temp} = {node.Identifier} - 1" });
            generatedCode.Add(new CodeEntity() { Code = $"{node.Identifier} = {temp}" });
            generatedCode.Add(MakeLabeledGoto(beginLabel));
            generatedCode.Add(nextLabel);
            return node;
        }

        private BoolRelationExpressionNode Visit(BoolRelationExpressionNode node) {
            var props = properties[node];
            node.e1.Visit(this);
            node.e2.Visit(this);
            var op = node.op == 1 ? "<" : ">";
            generatedCode.Add(new CodeEntity() {Code = $"if {properties[node.e1].addr} {op} {properties[node.e2].addr} goto <label:{props.trueLabel.LabelId}>"});
            generatedCode.Add(MakeLabeledGoto(props.falseLabel));
            return node;
        }

        private ExpressionNode Visit(ExpressionNode node) {
            dynamic props = new ExpandoObject();
            if (node.op == 6) {
                node.f.Visit(this);
                props.addr = properties[node.f].addr;
            } else if (node.op == 5) {
                var temp = MakeTemp();
                node.e1.Visit(this);
                node.f.Visit(this);
                generatedCode.Add(new CodeEntity(){Code = $"{temp} = {properties[node.e1].addr} ^ {properties[node.f].addr}"});
                props.addr = temp;
            } else {
                var temp = MakeTemp();
                node.e1.Visit(this);
                node.e2.Visit(this);
                generatedCode.Add(new CodeEntity() {Code = $"{temp} = {properties[node.e1].addr} {opMap[node.op]} {properties[node.e2].addr}"});
                props.addr = temp;
            }
            properties.Add(node, props);
            return node;
        }

        private FactorNode Visit(FactorNode node) {
            dynamic props = new ExpandoObject();
            switch (node.type) {
                case 1:
                    props.addr = node.Identifier;
                    break;
                case 2:
                    props.addr = node.value.ToString();
                    break;
                case 3:
                    node.e1.Visit(this);
                    props.addr = properties[node.e1].addr;
                    break;
            }
            properties.Add(node, props);
            return node;
        }

        public static List<CodeEntity> ResolveLabels(List<CodeEntry> cl) {
            List<CodeEntity> res = new List<CodeEntity>();
            Dictionary<Label, int> addressMap = new Dictionary<Label, int>();
            foreach (var entry in cl) {
                if (entry is Label l) {
                    addressMap.Add(l, res.Count);
                }

                if (entry is CodeEntity ce) {
                    res.Add(ce);
                }
            }

            foreach (var kv in addressMap) {
                foreach (var entity in res) {
                    entity.Code = entity.Code.Replace($"<label:{kv.Key.LabelId}>", kv.Value.ToString());
                }
            }
            res.Add(new CodeEntity());
            return res;
        }
    }
}
