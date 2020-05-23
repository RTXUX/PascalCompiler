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
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;
using Telerik.Windows.Diagrams.Core;

namespace SyntaxAnalyzerGUI
{
    /// <summary>
    /// ItemSetDiagram.xaml 的交互逻辑
    /// </summary>
    public partial class ItemSetDiagram : Window {
        private Slr1Table slr1Table;
        public ItemSetDiagram(Slr1Table slr1Table) {
            this.slr1Table = slr1Table;
            InitializeComponent();
            InitializeGraph();
        }

        private void InitializeGraph() {
            var stateToShape = new Dictionary<AnalyzerState, RadDiagramShape>();
            foreach (var state in slr1Table.States.Values) {
                var shape = new RadDiagramShape() {
                    Content = String.Join("\n", from item in state.ItemSet select item.ToString())
                };
                shape.Geometry = ShapeFactory.GetShapeGeometry(CommonShapeType.RectangleShape);
                ItemSetGraph.Items.Add(shape);
                stateToShape.Add(state, shape);
            }

            foreach (var state in slr1Table.States.Values) {
                foreach (var kv in state.Action) {
                    if (kv.Value is ShiftOperation so) {
                        var connection = new RadDiagramConnection() {
                            Source = stateToShape[state],
                            Target = stateToShape[so.NextState],
                            Content = new TextBlock() {Text = kv.Key.Name}
                        };
                        ItemSetGraph.Items.Add(connection);
                    }
                }

                foreach (var kv in state.GotoTable) {
                    var connection = new RadDiagramConnection()
                    {
                        Source = stateToShape[state],
                        Target = stateToShape[kv.Value],
                        Content = new TextBlock() { Text = kv.Key.ToString() }
                    };
                    ItemSetGraph.Items.Add(connection);
                }

            }
            ItemSetGraph.AutoLayout = true;
            ItemSetGraph.Layout(LayoutType.Sugiyama, new SugiyamaSettings() {
                HorizontalDistance = 10,
                VerticalDistance = 10,
                Orientation = Telerik.Windows.Diagrams.Core.Orientation.Horizontal,
            });
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            ItemSetGraph.Layout(LayoutType.Sugiyama, new SugiyamaSettings() {
                HorizontalDistance = 10,
                VerticalDistance = 10
            });
        }
    }
}
