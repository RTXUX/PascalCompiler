using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using Microsoft.Win32;
using PascalCompiler.Lexical;
using PascalCompiler.Lexical.Definition;
using PascalCompiler.Syntax;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Syntax.Generator.Utils;
using PascalCompiler.Syntax.TreeNode.Definition;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;
using Telerik.Windows.Diagrams.Core;

namespace SyntaxAnalyzerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Slr1Table slr1Table;
        private Generator generator;
        private Slr1Driver slr1Driver;
        private string FilePath;
        public MainWindow()
        {
            FluentPalette.Palette.FontFamily = new FontFamily("Sarasa Mono SC");
            FluentPalette.Palette.FontSize = 16;
            StyleManager.ApplicationTheme = new FluentTheme();
            InitializeComponent();
            InitializeSlr1Table();
            Editor.Document.Language = new PascalCompiler.Lexical.Lexer.PascalSyntaxLanguage();
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, OnNewFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OnOpenFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, OnSaveFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, OnSaveAsFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnExit));
        }

        private void OnNewFile(object sender, ExecutedRoutedEventArgs e) {
            FilePath = null;
            Editor.Document.SetText("");
        }
        private void OnOpenFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = false, Filter = "所有文件 (*.*)|*.*" };
            if (dialog.ShowDialog() == true)
            {
                using (var stream = dialog.OpenFile())
                {
                    Editor.Document.LoadFile(stream, Encoding.UTF8);
                }

                FilePath = dialog.FileName;
            }
        }
        private void OnSaveFile(object sender, ExecutedRoutedEventArgs e)
        {
            if (FilePath != null)
            {
                Editor.Document.SaveFile(FilePath, LineTerminator.Newline);
            }
            else
            {
                OnSaveAsFile(sender, e);
            }
        }
        private void OnSaveAsFile(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { OverwritePrompt = true };
            if (dialog.ShowDialog() == true)
            {
                using (var stream = dialog.OpenFile())
                {
                    Editor.Document.SaveFile(stream, Encoding.UTF8, LineTerminator.Newline);
                }

                FilePath = dialog.FileName;
            }
        }
        private void OnExit(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }



        private void InitializeSlr1Table() {
            this.generator = new Generator(PascalDefinition.ProductionRules);
            slr1Table = generator.Generate(PascalDefinition.NonTerminalKey.Start);
            slr1Table.AllowedErrorRecoveryKey.Add(PascalDefinition.NonTerminalKey.Statement, () => new StatementNode(new SyntaxNode[0]));
            slr1Driver = new Slr1Driver(slr1Table);
        }

        private void OpenDiagram(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            var a = new ItemSetDiagram(slr1Table);
            a.Show();
        }

        private void ToggleAnalyze(object sender, RoutedEventArgs e) {
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
            var history = new List<ParserConfiguration>();
            var exceptions = new List<SyntaxException>();
            SyntaxNode treeRoot;
            try {
                
                treeRoot = slr1Driver.Parse(new Queue<LexicalElement>(lexicals),
                    CommonUtils.Closure(
                        new HashSet<Item>()
                            {new Item() {ProductionRule = PascalDefinition.ProductionRules[0], Cursor = 0}},
                        generator.ProductionDict), typeof(SNode), history, exceptions);
                if (exceptions.Count > 0) {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"语法分析器共检测到{exceptions.Count}个错误\n\n");
                    foreach (var exception in exceptions) {
                        sb.Append(exception.Message);
                        sb.Append('\n');
                    }

                    MessageBox.Show(sb.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                } else {
                    new SyntaxTreeView(treeRoot).Show();
                }
                
                
            } catch (Exception ex) {
                StringBuilder sb = new StringBuilder();
                sb.Append($"语法分析器共检测到{exceptions.Count}个错误，无法恢复\n\n");
                foreach (var exception in exceptions)
                {
                    sb.Append(exception.Message);
                    sb.Append('\n');
                }
                MessageBox.Show(sb.ToString(), $"语法分析错误", MessageBoxButton.OK, MessageBoxImage.Error);
                //return;
            }
            new AnalyzerHistory(history, slr1Driver.Slr1Table).Show();

        }

    }
}
