using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax.Generator;

namespace PascalCompiler.Syntax.TreeNode.Definition
{
    internal class Util {
        internal static SyntaxException MakeTypeSyntaxException(Type type1, int pos) {
            return new SyntaxException($"Type Error: {type1.Name}, position {pos}");
        }
    }

    public class SNode : SyntaxNode {
        public readonly CompoundStatementNode CompoundStatement;

        public SNode(SyntaxNode[] argNodes) : base(argNodes) {
            if (argNodes[3] is CompoundStatementNode n) {
                CompoundStatement = n;
                return;
            }
            throw Util.MakeTypeSyntaxException(GetType(), 3);
        }
    }

    public class CompoundStatementNode : SyntaxNode {
        public readonly SyntaxNode StatementsNode;

        public CompoundStatementNode(SyntaxNode[] argNodes) : base(argNodes) {
            if (argNodes[1] is StatementsNodeVar1 || argNodes[1] is StatementsNodeVar2) {
                StatementsNode = argNodes[1];
                return;
            }
            throw Util.MakeTypeSyntaxException(GetType(), 3);
        }
    }

    public class StatementsNodeVar1 : SyntaxNode {
        public readonly SyntaxNode StatementNode;

        public StatementsNodeVar1(SyntaxNode[] argNodes) : base(argNodes) {
            if (argNodes[0] is StatementNode || argNodes[0] is AssignStatementNode || argNodes[0] is WhileStatementNode) {
                StatementNode = argNodes[0];
                return;
            }

            throw Util.MakeTypeSyntaxException(GetType(), 0);
        }
    }

    public class StatementsNodeVar2 : SyntaxNode {
        public readonly SyntaxNode StatementsNode;
        public readonly SyntaxNode StatementNode;

        public StatementsNodeVar2(SyntaxNode[] argNodes) : base(argNodes) {
            if (!(argNodes[0] is StatementsNodeVar1 || argNodes[0] is StatementsNodeVar2)) {
                throw Util.MakeTypeSyntaxException(GetType(), 0);
            }

            if (!(argNodes[2] is StatementNode || argNodes[2] is AssignStatementNode ||
                  argNodes[2] is WhileStatementNode)) {
                throw Util.MakeTypeSyntaxException(GetType(), 2);
            }

            StatementNode = argNodes[2];
            StatementsNode = argNodes[0];
        }
    }

    public class AssignStatementNode : SyntaxNode {
        public readonly string Identifier;
        public readonly ExpressionNode ExpressionNode;

        public AssignStatementNode(SyntaxNode[] argNodes) : base(argNodes) {
            Identifier = ((IdentifierElement) ((TerminalNode) argNodes[0]).Lex).Value;
            ExpressionNode = (ExpressionNode) argNodes[2];
        }
    }

    // stmt -> compound_stmt | if_stmt | for_stmt | eps
    public class StatementNode : SyntaxNode {
        public readonly SyntaxNode Next;

        public StatementNode(SyntaxNode[] argNodes) : base(argNodes) {
            if (argNodes.Length > 0) {
                Next = argNodes[0];
            }
        }
    }

    public class WhileStatementNode : SyntaxNode {
        public readonly BoolRelationExpressionNode BoolRelationExpression;
        public readonly SyntaxNode StatementNode;
        public WhileStatementNode(SyntaxNode[] argNodes) : base(argNodes) {
            BoolRelationExpression = (BoolRelationExpressionNode) argNodes[1];
            StatementNode = argNodes[3];
        }
    }

    public class IfStatementNode : SyntaxNode {
        public readonly BoolRelationExpressionNode Condition;
        public readonly SyntaxNode StatementNode;

        public IfStatementNode(SyntaxNode[] argNodes) : base(argNodes) {
            Condition = (BoolRelationExpressionNode) argNodes[1];
            StatementNode = argNodes[3];
        }
    } 

    public class IfElseStatementNode : SyntaxNode {
        public readonly BoolRelationExpressionNode Condition;
        public readonly SyntaxNode StatementNode1;
        public readonly SyntaxNode StatementNode2;
        public IfElseStatementNode(SyntaxNode[] argNodes) : base(argNodes) {
            Condition = (BoolRelationExpressionNode)argNodes[1];
            StatementNode1 = argNodes[3];
            StatementNode2 = argNodes[5];
        }
    }

    public class ForStatementUpNode : SyntaxNode {
        public readonly string Identifier;
        public readonly ExpressionNode expr1;
        public readonly ExpressionNode expr2;
        public readonly SyntaxNode StatementNode;
        public ForStatementUpNode(SyntaxNode[] argNodes) : base(argNodes) {
            Identifier = ((IdentifierElement) ((TerminalNode) argNodes[1]).Lex).Value;
            expr1 = (ExpressionNode) argNodes[3];
            expr2 = (ExpressionNode) argNodes[5];
            StatementNode = argNodes[7];
        }
    }

    public class ForStatementDownNode : SyntaxNode {
        public readonly string Identifier;
        public readonly ExpressionNode expr1;
        public readonly ExpressionNode expr2;
        public readonly SyntaxNode StatementNode;

        public ForStatementDownNode(SyntaxNode[] argNodes) : base(argNodes) {
            Identifier = ((IdentifierElement)((TerminalNode)argNodes[1]).Lex).Value;
            expr1 = (ExpressionNode)argNodes[3];
            expr2 = (ExpressionNode)argNodes[5];
            StatementNode = argNodes[7];
        }
    }

    public class BoolRelationExpressionNode : SyntaxNode {
        public readonly int op;
        public readonly ExpressionNode e1;
        public readonly ExpressionNode e2;
        public BoolRelationExpressionNode(SyntaxNode[] argNodes) : base(argNodes) {
            switch (((NonwordElement)((TerminalNode)argNodes[1]).Lex).Type) {
                case NonwordType.Less:
                    op = 1;
                    break;
                case NonwordType.Greater:
                    op = 2;
                    break;
            }
            e1 = (ExpressionNode)argNodes[0];
            e2 = (ExpressionNode) argNodes[2];
        }
    }

    public class ExpressionNode : SyntaxNode {
        public readonly int op;
        public readonly ExpressionNode e1;
        public readonly ExpressionNode e2;
        public readonly FactorNode f;
        public ExpressionNode(SyntaxNode[] argNodes) : base(argNodes) {
            if (argNodes.Length == 1)
            {
                op = 6;
                f = (FactorNode)argNodes[0];
            }
            else
            {
                Dictionary<NonwordType, int> map = new Dictionary<NonwordType, int>()
                {
                    [NonwordType.Addition] = 1,
                    [NonwordType.Subtraction] = 2,
                    [NonwordType.Multiplication] = 3,
                    [NonwordType.Division] = 4,
                    [NonwordType.Caret] = 5
                };
                op = map[((NonwordElement)((TerminalNode)argNodes[1]).Lex).Type];
                e1 = (ExpressionNode)argNodes[0];
                if (op <= 4)
                {
                    e2 = (ExpressionNode)argNodes[2];
                }
                else
                {
                    f = (FactorNode)argNodes[2];
                }
            }

        }
    }

    public class FactorNode : SyntaxNode {
        public readonly int type = 0;
        public readonly int value;
        public readonly string Identifier;
        public readonly ExpressionNode e1;
        public FactorNode(SyntaxNode[] argNodes) : base(argNodes) {
            if (argNodes.Length == 3) {
                type = 3;
                e1 = (ExpressionNode) argNodes[1];
            } else {
                LexicalElement l = ((TerminalNode) argNodes[0]).Lex;
                if (l is IdentifierElement ie) {
                    type = 1;
                    Identifier = ie.Value;
                } else if (l is IntegerLiteral il) {
                    type = 2;
                    value = il.Value;
                }
            }
        }
    }

}
