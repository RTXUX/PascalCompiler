using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
using PascalCompiler.Lexical.Lexer;
using PascalCompiler.Lexical.Definition;

namespace LexerGUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        private string _FilePath = null;
        private bool _analyzed = false;
        private bool _UpdatingSelection = false;
        private List<LexicalElement> lexicalElements;
        private List<IdentifierElement> symbols;
        public bool Analyzed {
            get => _analyzed;
            set {
                _analyzed = value;
                Editor.Document.IsReadOnly = value;
                AnalyzeButton.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                ClearAnalysisButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                if (!value) {
                    PropGrid.DataObject = null;
                    ListView.ItemsSource = null;
                    lexicalElements = null;
                    symbols = null;
                    SymbolList.ItemsSource = null;
                }

                
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadLanguage();
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.New, OnNewFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OnOpenFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, OnSaveFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, OnSaveFileAs));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, OnClose));
            ListView.SelectionMode = SelectionMode.Single;
            Editor.InactiveSelectedTextBackground = new SolidColorBrush(Colors.Aqua);
            Editor.SelectedTextBackground = new SolidColorBrush(Colors.Aqua);
        }

        private void LoadLanguage() {
            Editor.Document.Language = new PascalSyntaxLanguage();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (!Analyzed) {
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
                        List<LexicalElement> result = new List<LexicalElement>();
                        while ((t = l.NextToken()) != null) {
                            result.Add(t);
                        }

                        lexicalElements = result;
                        ListView.ItemsSource = result;
                        symbols = new List<IdentifierElement>();
                        foreach (var le in result) {
                            if (le is IdentifierElement ie) {
                                bool found = false;
                                foreach (var ids in symbols) {
                                    if (ie.Value == ids.Value) {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) symbols.Add(ie);
                            }
                        }

                        SymbolList.ItemsSource = symbols;
                    }

                    Analyzed = true;
                } catch (LexicalException ex) {
                    MessageBox.Show(ex.Message, "词法错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    Editor.ActiveView.Selection.StartPosition = new TextPosition(ex.line, ex.begin);
                    Editor.ActiveView.Selection.EndPosition = new TextPosition(ex.line, ex.end);
                } catch (Exception ex) {
                    MessageBox.Show(ex.Message, "未知异常", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                Analyzed = false;
            }
        }

        private void OnNewFile(object sender, ExecutedRoutedEventArgs e) {
            Editor.Document.SetText("");
            _FilePath = null;
            Analyzed = false;
        }

        private void OnSaveFile(object sender, ExecutedRoutedEventArgs e) {
            if (_FilePath != null) {
                Editor.Document.SaveFile(_FilePath, LineTerminator.Newline);
            } else {
                OnSaveFileAs(sender, e);
            }
        }

        private void OnSaveFileAs(object sender, ExecutedRoutedEventArgs e) {
            var dialog = new SaveFileDialog {OverwritePrompt = true};
            if (dialog.ShowDialog() == true) {
                using (var stream = dialog.OpenFile()) {
                    Editor.Document.SaveFile(stream, Encoding.UTF8, LineTerminator.Newline);
                }

                _FilePath = dialog.FileName;
            }
        }

        private void OnOpenFile(object sender, ExecutedRoutedEventArgs e) {
            var dialog = new OpenFileDialog {Multiselect = false, Filter = "所有文件 (*.*)|*.*"};
            if (dialog.ShowDialog() == true) {
                using (var stream = dialog.OpenFile()) {
                    Editor.Document.LoadFile(stream, Encoding.UTF8);
                }

                _FilePath = dialog.FileName;
            }

            Analyzed = false;
        }

        private void OnClose(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            PropGrid.DataObject = ListView.SelectedItem;
            if (_UpdatingSelection) return;
            _UpdatingSelection = true;
            
            if (ListView.SelectedItem is LexicalElement le) {
                Editor.ActiveView.Selection.StartPosition = new TextPosition(le.LineNumber, le.StartIndex);
                Editor.ActiveView.Selection.EndPosition = new TextPosition(le.LineNumber, le.EndIndex);
            }

            _UpdatingSelection = false;
        }

        private void PropGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            PropGrid.IsReadOnly = true;
        }

        private void PropGrid_ItemSelecting(object sender, ActiproSoftware.Windows.Controls.Grids.TreeListBoxItemEventArgs e)
        {
            PropGrid.IsReadOnly = true;
        }

        private void Editor_ViewSelectionChanged(object sender, ActiproSoftware.Windows.Controls.SyntaxEditor.EditorViewSelectionEventArgs e) {
            if (lexicalElements == null) return;
            if (_UpdatingSelection) return;
            _UpdatingSelection = true;
            var cp = e.CaretPosition;
            int index = 0;
            foreach (var le in lexicalElements) {
                if (cp.Line == le.LineNumber) {
                    if (cp.Character >= le.StartIndex && cp.Character < le.EndIndex) {
                        ListView.SelectedIndex = index;
                        ListView.ScrollIntoView(ListView.SelectedItem);
                        break;
                    }
                }

                index++;
            }

            _UpdatingSelection = false;
        }

        private void SymbolList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement) e.OriginalSource).DataContext is IdentifierElement ie) {
                ListView.SelectedItem = ie;
                ListView.ScrollIntoView(ie);
            }
        }
    }

    [ValueConversion(typeof(LexicalElement), typeof(string))]
    public class NameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value.GetType().Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
