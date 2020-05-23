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

namespace SyntaxAnalyzerGUI
{
    /// <summary>
    /// AnalyzerHistory.xaml 的交互逻辑
    /// </summary>
    public partial class AnalyzerHistory : Window {
        private List<ParserConfiguration> history;
        private Slr1Table slr1Table;
        public AnalyzerHistory(List<ParserConfiguration> history, Slr1Table slr1Table) {
            this.history = history;
            this.slr1Table = slr1Table;
            InitializeComponent();
        }

        private void HistoryList_Initialized(object sender, EventArgs e) {
            var stateIds = new Dictionary<AnalyzerState, int>();
            var typeKey = new Dictionary<Type, string>();
            int index = 0;
            foreach (var state in slr1Table.States.Values) {
                stateIds.Add(state, ++index);
            }
            foreach (var rule in slr1Table.ProductionRules) {
                if (!typeKey.ContainsKey(rule.LeftType)) {
                    typeKey.Add(rule.LeftType, rule.Key.ToString());
                }
            }
            var dsl = new List<DisplayState>();
            foreach (var hisItem in history) {
                var ns = new List<string>();
                foreach (var node in hisItem.nodeStack) {
                    if (node is TerminalNode tn) {
                        ns.Add(tn.Lex.StringValue);
                    } else {
                        ns.Add(typeKey[node.GetType()]);
                    }
                }
                var ss = new List<string>();
                foreach (var state in hisItem.stateStack) {
                    ss.Add(stateIds[state].ToString());
                }
                var input = new List<string>();
                foreach (var le in hisItem.input) {
                    input.Add(le.StringValue);
                }
                input.Add("$");
                string ops = "";
                switch (hisItem.operation) {
                    case ShiftOperation so:
                        ops = $"Shift to {stateIds[so.NextState]}";
                        break;
                    case ReduceOperation ro:
                        ops = $"Reduce by {ro.ReduceBy.ToString()}";
                        break;
                }
                dsl.Add(new DisplayState() {
                    nodes = String.Join(" ", ns),
                    states = String.Join(" ", ss),
                    inputs = String.Join(" ", input),
                    operation = ops
                });
            }

            HistoryList.ItemsSource = dsl;
        }
    }

    public class DisplayState {
        public string nodes { get; set; }
        public string states { get; set; }
        public string inputs { get; set; }
        public string operation { get; set; }
    }
}
