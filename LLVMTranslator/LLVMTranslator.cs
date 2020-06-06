using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using LLVMSharp.Interop;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.TreeNode.Definition;

namespace LLVMTranslator
{
    public class LLVMTranslator : AbstractVisitor {
        private LLVMModuleRef mod;
        private readonly LLVMExecutionEngineRef engine;
        private LLVMBuilderRef builder;
        private readonly List<LLVMBasicBlockRef> blockStack;
        public LLVMValueRef func { get; private set; }
        private LLVMBasicBlockRef entry;
        private readonly List<LLVMSymbolTable> symbolTables;
        private readonly Dictionary<SyntaxNode, dynamic> properties = new Dictionary<SyntaxNode, dynamic>();
        private int count = 0;
        public LLVMTranslator(LLVMModuleRef mod, LLVMExecutionEngineRef engine, LLVMBuilderRef builder, LLVMSymbolTable rootSymbol) {
            this.mod = mod;
            blockStack = new List<LLVMBasicBlockRef>();
            symbolTables = new List<LLVMSymbolTable>();
            symbolTables.Add(rootSymbol);
            this.engine = engine;
            this.builder = builder;
        }

        private dynamic GetProperty(SyntaxNode node) {
            if (properties.TryGetValue(node, out var val)) {
                return val;
            }
            val = new ExpandoObject();
            properties.Add(node, val);
            return val;
        }

        public override SyntaxNode Visit(SyntaxNode node) {
            return node == null ? null : (SyntaxNode) Visit((dynamic) node);
        }

        private LLVMValueRef CreateAllocEntry(string name) {
            var tmpB = mod.Context.CreateBuilder();
            tmpB.PositionAtEnd(func.EntryBasicBlock);
            var v = tmpB.BuildAlloca(LLVMTypeRef.Int32, name);
            tmpB.Dispose();
            return v;
        }

        private LLVMValueRef LookupSymbolOrAlloc(string name) {
            try {
                return symbolTables.Last().Lookup(name);
            } catch (KeyNotFoundException) {
                var v = CreateAllocEntry(name);
                symbolTables.Last().Add(name, v);
                return v;
            }
        }

        private SNode Visit(SNode node) {
            LLVMTypeRef[] paramType = { };
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, paramType);
            func = mod.AddFunction("main", funcType);
            entry = func.AppendBasicBlock("entry");
            var body = func.AppendBasicBlock("body");
            blockStack.Add(entry);
            blockStack.Add(body);
            symbolTables.Add(new LLVMSymbolTable(symbolTables.Last()));
            builder.PositionAtEnd(body);
            Visit(node.CompoundStatement);
            builder.BuildRetVoid();
            builder.PositionAtEnd(entry);
            builder.BuildBr(body);
            return node;
        }

        private CompoundStatementNode Visit(CompoundStatementNode node) {
            Visit(node.StatementsNode);
            return node;
        }

        private StatementsNodeVar1 Visit(StatementsNodeVar1 node) {
            Visit(node.StatementNode);
            return node;
        }

        private StatementsNodeVar2 Visit(StatementsNodeVar2 node) {
            Visit(node.StatementsNode);
            Visit(node.StatementNode);
            return node;
        }

        private AssignStatementNode Visit(AssignStatementNode node) {
            Visit(node.ExpressionNode);
            var v = LookupSymbolOrAlloc(node.Identifier);
            builder.BuildStore(GetProperty(node.ExpressionNode).addr, v);
            return node;
        }

        private StatementNode Visit(StatementNode node) {
            if (node.Next == null) return null;
            Visit(node.Next);
            return node;
        }

        private WhileStatementNode Visit(WhileStatementNode node) {
            
            var condBB = func.AppendBasicBlock($"cond_{++count}");
            var bodyBB = func.AppendBasicBlock($"body_{++count}");
            var contBB = func.AppendBasicBlock($"next_{++count}");
            builder.BuildBr(condBB);
            builder.PositionAtEnd(condBB);
            Visit(node.BoolRelationExpression);
            builder.BuildCondBr(GetProperty(node.BoolRelationExpression).addr, bodyBB, contBB);
            builder.PositionAtEnd(bodyBB);
            Visit(node.StatementNode);
            builder.BuildBr(condBB);
            builder.PositionAtEnd(contBB);
            return node;
        }

        private IfStatementNode Visit(IfStatementNode node) {
            var condBB = func.AppendBasicBlock($"cond_{++count}");
            var bodyBB = func.AppendBasicBlock($"body_{++count}");
            var contBB = func.AppendBasicBlock($"next_{++count}");
            builder.BuildBr(condBB);
            builder.PositionAtEnd(condBB);
            Visit(node.Condition);
            builder.BuildCondBr(GetProperty(node.Condition).addr, bodyBB, contBB);
            builder.PositionAtEnd(bodyBB);
            Visit(node.StatementNode);
            builder.BuildBr(contBB);
            builder.PositionAtEnd(contBB);
            return node;
        }

        private IfElseStatementNode Visit(IfElseStatementNode node) {
            var condBB = func.AppendBasicBlock($"cond_{++count}");
            var bodyBB = func.AppendBasicBlock($"body_{++count}");
            var elseBB = func.AppendBasicBlock($"else_{++count}");
            var contBB = func.AppendBasicBlock($"cont_{++count}");
            builder.BuildBr(condBB);
            builder.PositionAtEnd(condBB);
            Visit(node.Condition);
            builder.BuildCondBr(GetProperty(node.Condition).addr, bodyBB, elseBB);
            builder.PositionAtEnd(bodyBB);
            Visit(node.StatementNode1);
            builder.BuildBr(contBB);
            builder.PositionAtEnd(elseBB);
            Visit(node.StatementNode2);
            builder.BuildBr(contBB);
            builder.PositionAtEnd(contBB);
            return node;
        }

        private ForStatementUpNode Visit(ForStatementUpNode node) {
            Visit(node.expr1);
            builder.BuildStore(GetProperty(node.expr1).addr, LookupSymbolOrAlloc(node.Identifier));
            var condBB = func.AppendBasicBlock($"cond_{++count}");
            var bodyBB = func.AppendBasicBlock($"body_{++count}");
            var contBB = func.AppendBasicBlock($"cont_{++count}");
            builder.BuildBr(condBB);
            builder.PositionAtEnd(condBB);
            Visit(node.expr2);
            var t = builder.BuildLoad(LookupSymbolOrAlloc(node.Identifier));
            var b = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, t, GetProperty(node.expr2).addr);
            builder.BuildCondBr(b, bodyBB, contBB);
            builder.PositionAtEnd(bodyBB);
            Visit(node.StatementNode);
            t = builder.BuildLoad(LookupSymbolOrAlloc(node.Identifier));
            var r = builder.BuildAdd(t, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
            builder.BuildStore(r, LookupSymbolOrAlloc(node.Identifier));
            builder.BuildBr(condBB);
            builder.PositionAtEnd(contBB);
            return node;
        }

        private ForStatementDownNode Visit(ForStatementDownNode node) {
            Visit(node.expr1);
            builder.BuildStore(GetProperty(node.expr1).addr, LookupSymbolOrAlloc(node.Identifier));
            var condBB = func.AppendBasicBlock($"cond_{++count}");
            var bodyBB = func.AppendBasicBlock($"body_{++count}");
            var contBB = func.AppendBasicBlock($"cont_{++count}");
            builder.BuildBr(condBB);
            builder.PositionAtEnd(condBB);
            Visit(node.expr2);
            var t = builder.BuildLoad(LookupSymbolOrAlloc(node.Identifier));
            var b = builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, t, GetProperty(node.expr2).addr);
            builder.BuildCondBr(b, bodyBB, contBB);
            builder.PositionAtEnd(bodyBB);
            Visit(node.StatementNode);
            t = builder.BuildLoad(LookupSymbolOrAlloc(node.Identifier));
            var r = builder.BuildSub(t, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1));
            builder.BuildStore(r, LookupSymbolOrAlloc(node.Identifier));
            builder.BuildBr(condBB);
            builder.PositionAtEnd(contBB);
            return node;
        }

        private BoolRelationExpressionNode Visit(BoolRelationExpressionNode node) {
            Visit(node.e1);
            Visit(node.e2);
            LLVMValueRef v;
            if (node.op == 1) {
                v = builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, GetProperty(node.e1).addr,
                    GetProperty(node.e2).addr);
            } else {
                v = builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, GetProperty(node.e1).addr,
                    GetProperty(node.e2).addr);
            }

            GetProperty(node).addr = v;
            return node;
        }

        private ExpressionNode Visit(ExpressionNode node) {
            LLVMValueRef v = null;
            if (node.op==6) {
                Visit(node.f);
                v = GetProperty(node.f).addr;
            } else if (node.op == 5) {
                throw new NotImplementedException();
            } else {
                Visit(node.e1);
                Visit(node.e2);
                if (node.op == 1) {
                    v = builder.BuildAdd(GetProperty(node.e1).addr, GetProperty(node.e2).addr);
                } else if (node.op == 2) {
                    v = builder.BuildSub(GetProperty(node.e1).addr, GetProperty(node.e2).addr);
                } else if (node.op == 3) {
                    v = builder.BuildMul((LLVMValueRef)GetProperty(node.e1).addr, (LLVMValueRef)GetProperty(node.e2).addr, "".AsSpan());
                } else if (node.op == 4) {
                    v = builder.BuildSDiv((LLVMValueRef)GetProperty(node.e1).addr, (LLVMValueRef)GetProperty(node.e2).addr, "".AsSpan());
                } 
            }
            GetProperty(node).addr = v;
            return node;
        }

        private FactorNode Visit(FactorNode node) {
            if (node.type == 3) {
                Visit(node.e1);
                GetProperty(node).addr = GetProperty(node.e1).addr;
            } else if (node.type == 2) {
                GetProperty(node).addr = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)node.value);
            } else {
                var y = builder.BuildLoad(LookupSymbolOrAlloc(node.Identifier));
                GetProperty(node).addr = y;
            }
            return node;
        }
    }
}
