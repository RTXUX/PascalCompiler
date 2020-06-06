using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LLVMSharp;
using LLVMSharp.Interop;
using LLVMTranslator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PascalCompiler.Lexical;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.Generator.Utils;
using PascalCompiler.Syntax.TreeNode.Definition;

namespace LLVMTest {
    [TestClass]
    public class UnitTest1 {

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Add(int a);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GetG(IntPtr addr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PM();

        [TestMethod]
        public void TestMethod1() {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
            var mod = LLVMModuleRef.CreateWithName("Test1");
            var options = LLVMMCJITCompilerOptions.Create();
            options.NoFramePointerElim = 1;
            var engine = mod.CreateMCJITCompiler(ref options);
            LLVMTypeRef[] paramsType = {LLVMTypeRef.Int32};
            var funcType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, paramsType, false);
            var acc = mod.AddFunction("acc", funcType);
            var gs = mod.AddGlobal(LLVMTypeRef.Int32, "gs");
            var entry = acc.AppendBasicBlock("entry");
            var builder = mod.Context.CreateBuilder();
            builder.PositionAtEnd(entry);
            var tmp = builder.BuildLoad(gs);
            var tmp2 = builder.BuildMul(tmp, acc.GetParam(0), "".AsSpan());
            builder.BuildStore(tmp2, gs);
            builder.BuildRet(tmp2);
            if (!mod.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var error)) {
                Trace.WriteLine(error);
            }
            builder.Dispose();
            object gsActual = 1;
            var handle = GCHandle.Alloc(gsActual, GCHandleType.Pinned);
            engine.AddGlobalMapping(gs, handle.AddrOfPinnedObject());
            var accMethod = engine.GetPointerToGlobal<Add>(acc);
            var gsAddr = engine.GetPointerToGlobal(gs);
            Assert.AreEqual(2, accMethod(2));
            Assert.AreEqual(8, accMethod(4));
            Assert.AreEqual(8, gsActual);
            Trace.WriteLine(mod.PrintToString());
            engine.Dispose();
            handle.Free();
        }

        [TestMethod]
        public void LLVMTest2() {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
            var mod = LLVMModuleRef.CreateWithName("Test1");
            var options = LLVMMCJITCompilerOptions.Create();
            options.NoFramePointerElim = 1;
            var engine = mod.CreateMCJITCompiler(ref options);
            var builder = mod.Context.CreateBuilder();
            var lexicals = new List<LexicalElement>();
            var globals = new LLVMSymbolTable(null);
            globals.Add("z", mod.AddGlobal(LLVMTypeRef.Int32, "z"));
            object z = 0;
            var handle = GCHandle.Alloc(z, GCHandleType.Pinned);
            engine.AddGlobalMapping(globals["z"], handle.AddrOfPinnedObject());
            using (var fs = new FileStream("D:\\Repos\\PascalCompiler\\SyntaxAnalyzerTest\\test_source2.txt", FileMode.Open)) {
                using (var ss = new StreamReader(fs)) {
                    var l = new LexerStateMachine(ss);
                    l.AdvanceChar();
                    LexicalElement t;
                    while ((t = l.NextToken()) != null)
                    {
                        if (t is LineFeedElement) continue;
                        lexicals.Add(t);
                    }
                }
            }
            var history = new List<ParserConfiguration>();
            var exceptions = new List<SyntaxException>();
            SyntaxNode treeRoot;
            var generator = new Generator(PascalDefinition.ProductionRules);
            var slr1Table = generator.Generate(PascalDefinition.NonTerminalKey.Start);
            slr1Table.AllowedErrorRecoveryKey.Add(PascalDefinition.NonTerminalKey.Statement, () => new StatementNode(new SyntaxNode[0]));
            var slr1Driver = new Slr1Driver(slr1Table);
            treeRoot = slr1Driver.Parse(new Queue<LexicalElement>(lexicals),
                CommonUtils.Closure(
                    new HashSet<Item>()
                        {new Item() {ProductionRule = PascalDefinition.ProductionRules[0], Cursor = 0}},
                    generator.ProductionDict), typeof(SNode), history, exceptions);
            var translator =
                new LLVMTranslator.LLVMTranslator(mod, engine, builder, globals);
            translator.Visit(treeRoot);
            if (!mod.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var error)) {
                Trace.WriteLine(error);
            }
            Trace.WriteLine(mod.PrintToString());
            PM a = engine.GetPointerToGlobal<PM>(translator.func);
            a();
            builder.Dispose();
            engine.Dispose();
            handle.Free();
        }
    }
}