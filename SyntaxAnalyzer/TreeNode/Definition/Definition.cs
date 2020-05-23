using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PascalCompiler.Syntax.Generator;

namespace PascalCompiler.Syntax.TreeNode.Definition
{
    public class SNode : SyntaxNode {
        public SNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class CompoundStatementNode : SyntaxNode {
        public CompoundStatementNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class StatementsNodeVar1 : SyntaxNode {
        public StatementsNodeVar1(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class StatementsNodeVar2 : SyntaxNode {
        public StatementsNodeVar2(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class AssignStatementNode : SyntaxNode {
        public AssignStatementNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    // stmt -> compound_stmt | if_stmt | for_stmt | eps
    public class StatementNode : SyntaxNode {
        public StatementNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class WhileStatementNode : SyntaxNode {
        public WhileStatementNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class IfStatementNode : SyntaxNode {
        public IfStatementNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class IfElseStatementNode : SyntaxNode {
        public IfElseStatementNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class ForStatementUpNode : SyntaxNode {
        public ForStatementUpNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class ForStatementDownNode : SyntaxNode {
        public ForStatementDownNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class BoolRelationExpressionNode : SyntaxNode {
        public BoolRelationExpressionNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class ExpressionNode : SyntaxNode {
        public ExpressionNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

    public class FactorNode : SyntaxNode {
        public FactorNode(SyntaxNode[] argNodes) : base(argNodes) { }
    }

}
