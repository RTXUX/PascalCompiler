using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using PascalCompiler.Syntax.Generator;
using PascalCompiler.Translator.Garbage;

namespace SyntaxAnalyzerGUI
{
    /// <summary>
    /// GarbageTranslatorView.xaml 的交互逻辑
    /// </summary>
    public partial class GarbageTranslatorView : Window {
        private readonly List<CodeEntity> _codes;
        public GarbageTranslatorView(List<CodeEntity> codes)
        {
            InitializeComponent();
            _codes = codes;
            InitializeListView();
        }

        private void InitializeListView() {
            int index = 0;
            var items = new List<ViewItem>();
            foreach (var code in _codes) {
                items.Add(new ViewItem(index++, code.Code));
            }

            ListView.ItemsSource = items;
        }

        private sealed class ViewItem {
            public int Address { get; }
            public string Code { get; }

            public ViewItem(int address, string code) {
                Address = address;
                Code = code;
            }
        }
    }
}
