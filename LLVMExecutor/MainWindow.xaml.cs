using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ActiproSoftware.Text;
using LLVMSharp.Interop;
using LLVMTranslator;
using Microsoft.Win32;
using PascalCompiler.Lexical;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.Generator.Utils;
using PascalCompiler.Syntax.TreeNode.Definition;
using Telerik.Windows;
using Telerik.Windows.Controls;

namespace LLVMExecutor {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private Slr1Table slr1Table;
        private Generator generator;
        private Slr1Driver slr1Driver;
        private string FilePath;
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PM();
        public MainWindow() {
            FluentPalette.Palette.FontFamily = new FontFamily("Microsoft YaHei");
            FluentPalette.Palette.FontSize = 14;
            StyleManager.ApplicationTheme = new FluentTheme();
            InitializeComponent();
            InitializeSlr1Table();
            InitializeLLVM();
            Editor.Document.Language = new PascalCompiler.Lexical.Lexer.PascalSyntaxLanguage();
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, OnNewFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OnOpenFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, OnSaveFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, OnSaveAsFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnExit));
        }

        private void InitializeLLVM() {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();
        }

        private void InitializeSlr1Table() {
            this.generator = new Generator(PascalDefinition.ProductionRules);
            slr1Table = generator.Generate(PascalDefinition.NonTerminalKey.Start);
            slr1Table.AllowedErrorRecoveryKey.Add(PascalDefinition.NonTerminalKey.Statement,
                () => new StatementNode(new SyntaxNode[0]));
            slr1Driver = new Slr1Driver(slr1Table);
        }

        private void OnNewFile(object sender, ExecutedRoutedEventArgs e) {
            FilePath = null;
            Editor.Document.SetText("");
        }

        private void OnOpenFile(object sender, ExecutedRoutedEventArgs e) {
            var dialog = new RadOpenFileDialog() {Multiselect = false, Filter = "所有文件 (*.*)|*.*"};
            if (dialog.ShowDialog() == true) {
                using (var stream = dialog.OpenFile()) {
                    Editor.Document.LoadFile(stream, Encoding.UTF8);
                }

                FilePath = dialog.FileName;
            }
        }

        private void OnSaveFile(object sender, ExecutedRoutedEventArgs e) {
            if (FilePath != null) {
                Editor.Document.SaveFile(FilePath, LineTerminator.Newline);
            } else {
                OnSaveAsFile(sender, e);
            }
        }

        private void OnSaveAsFile(object sender, ExecutedRoutedEventArgs e) {
            var dialog = new RadSaveFileDialog();
            if (dialog.ShowDialog() == true) {
                using (var stream = dialog.OpenFile()) {
                    Editor.Document.SaveFile(stream, Encoding.UTF8, LineTerminator.Newline);
                }

                FilePath = dialog.FileName;
            }
        }

        private void OnExit(object sender, ExecutedRoutedEventArgs e) {
            Close();
        }

        private void GridViewDataControl_OnCellValidating(object sender, GridViewCellValidatingEventArgs e) {
            if (e.Cell.Column.UniqueName == "Name") {
                if ((e.NewValue as string)?.Length == 0) {
                    e.IsValid = false;
                    e.ErrorMessage = "Name must not be empty.";
                }

                if ((from syms in (SymbolTable.ItemsSource as ICollection<SymbolEntry>) select syms.Name).Contains(
                    e.NewValue)) {
                    e.IsValid = false;
                    e.ErrorMessage = "Name must be unique.";
                }
            }
        }

        private void RadMenuItem_OnClick(object sender, RadRoutedEventArgs e) {
            ExecuteCode();
        }

        private void ExecuteCode() {
            var globals = new Dictionary<string, GCHandle>();
            LLVMModuleRef mod;
            LLVMExecutionEngineRef engine = null;
            try {
                SymbolTable.IsReadOnly = true;
                mod = LLVMModuleRef.CreateWithName("main");
                var options = LLVMMCJITCompilerOptions.Create();
                engine = mod.CreateMCJITCompiler(ref options);
                var builder = mod.Context.CreateBuilder();
                var globalSource = SymbolTable.ItemsSource as ICollection<SymbolEntry>;
                var globalStore = new Dictionary<string, object>();
                foreach (var k in globalSource) {
                    globalStore.Add(k.Name, k.Value);
                }

                foreach (var kv in globalStore) {
                    globals.Add(kv.Key, GCHandle.Alloc(kv.Value, GCHandleType.Pinned));
                }

                var fpm = mod.CreateFunctionPassManager();
                fpm.AddPromoteMemoryToRegisterPass();
                fpm.AddInstructionCombiningPass();
                fpm.AddReassociatePass();
                fpm.AddGVNPass();
                fpm.AddCFGSimplificationPass();
                fpm.InitializeFunctionPassManager();
                var symT = ProcessGlobals(mod, engine, globals);
                var lexicals = new List<LexicalElement>();
                try {
                    using (var ms = new MemoryStream()) {
                        var writer = new StreamWriter(ms);
                        writer.Write(Editor.Document.CurrentSnapshot.GetText(LineTerminator.Newline));
                        writer.Flush();
                        ms.Seek(0L, SeekOrigin.Begin);
                        var reader = new StreamReader(ms);
                        var l = new LexerStateMachine(reader);
                        l.AdvanceChar();
                        LexicalElement t;
                        while ((t = l.NextToken()) != null) {
                            if (t is LineFeedElement) continue;
                            lexicals.Add(t);
                        }
                    }
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "词法错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var exceptions = new List<SyntaxException>();
                SyntaxNode treeRoot;
                try {
                    treeRoot = slr1Driver.Parse(new Queue<LexicalElement>(lexicals),
                        CommonUtils.Closure(
                            new HashSet<Item>()
                                {new Item() {ProductionRule = PascalDefinition.ProductionRules[0], Cursor = 0}},
                            generator.ProductionDict), typeof(SNode), null, exceptions);
                    if (exceptions.Count > 0) {
                        StringBuilder sb = new StringBuilder();
                        sb.Append($"语法分析器共检测到{exceptions.Count}个错误\n\n");
                        foreach (var exception in exceptions) {
                            sb.Append(exception.Message);
                            sb.Append('\n');
                        }

                        MessageBox.Show(sb.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                } catch (SyntaxException ex) {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"语法分析器共检测到{exceptions.Count}个错误，无法恢复\n\n");
                    foreach (var exception in exceptions) {
                        sb.Append(exception.Message);
                        sb.Append('\n');
                    }

                    MessageBox.Show(sb.ToString(), $"语法分析错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var translator = new LLVMTranslator.LLVMTranslator(mod, engine, builder, symT);
                translator.Visit(treeRoot);
                fpm.RunFunctionPassManager(translator.func);
                IrBox.Text = mod.PrintToString();
                PM main = engine.GetPointerToGlobal<PM>(translator.func);
                main();
                foreach (var symbolEntry in globalSource) {
                    symbolEntry.Value = (int) globalStore[symbolEntry.Name];
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                SymbolTable.IsReadOnly = false;
                if (engine != null) engine.Dispose();
                foreach (var kv in globals) {
                    kv.Value.Free();
                }
            }
        }

        private LLVMSymbolTable ProcessGlobals(LLVMModuleRef mod, LLVMExecutionEngineRef engine,
            Dictionary<string, GCHandle> globals) {
            var res = new LLVMSymbolTable(null);
            foreach (var kv in globals) {
                var gs = mod.AddGlobal(LLVMTypeRef.Int32, kv.Key);
                engine.AddGlobalMapping(gs, kv.Value.AddrOfPinnedObject());
                res.Add(kv.Key, gs);
            }

            return res;
        }
    }

    public class SymbolViewModel : ViewModelBase {
        public ObservableCollection<SymbolEntry> Symbols { get; } = new ObservableCollection<SymbolEntry>();
    }

    public class SymbolEntry : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _name;
        private int _value;

        public string Name {
            get => _name;
            set {
                if (value != _name) {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public int Value {
            get => _value;
            set {
                if (value != _value) {
                    _value = value;
                    OnPropertyChanged("Value");
                }
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) {
            PropertyChanged?.Invoke(this, args);
        }

        private void OnPropertyChanged(string propertyName) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}