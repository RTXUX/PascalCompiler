using PascalCompiler.Syntax.Generator;
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
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;
using Telerik.Windows.Diagrams.Core;

namespace SyntaxAnalyzerGUI
{
    /// <summary>
    /// SyntaxTreeView.xaml 的交互逻辑
    /// </summary>
    
    
    public partial class SyntaxTreeView : Window
    {
        private SyntaxNode treeRoot;
        public SyntaxTreeView(SyntaxNode treeRoot) {
            this.treeRoot = treeRoot;
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e) {
            var shape = new RadDiagramShape()
            {
                Content = treeRoot.GetType().Name
            };
            Diagram.Items.Add(shape);
            addNodes(treeRoot, shape);
            Diagram.RoutingService.Router = new OrgTreeRouter()
            {
                TreeLayoutType = TreeLayoutType.TreeDown
            };
            Task.Delay(1000).ContinueWith(_ => {
                Dispatcher.Invoke(() => {
                    Diagram.Layout(LayoutType.Tree, new TreeLayoutSettings()
                    {
                        TreeLayoutType = TreeLayoutType.TreeDown,
                        HorizontalSeparation = 10.0,
                        VerticalSeparation = 40.0,
                        Roots = { shape },
                        AnimateTransitions = true
                    });
                });
            });
        }

        private void addNodes(SyntaxNode node, RadDiagramShape parentShape)
        {
            if (node.Child == null) return;
            foreach (var syntaxNode in node.Child)
            {
                string content = "";
                if (syntaxNode is TerminalNode tn)
                {
                    content = tn.Lex.StringValue;
                }
                else
                {
                    content = syntaxNode.GetType().Name;
                }
                var shape = new RadDiagramShape()
                {
                    Content = content
                };
                shape.Geometry = ShapeFactory.GetShapeGeometry(CommonShapeType.EllipseShape);
                var connection = new RadDiagramConnection()
                {
                    Source = parentShape,
                    Target = shape,
                    SourceConnectorPosition = ConnectorPosition.Bottom,
                    TargetConnectorPosition = ConnectorPosition.Left
                };
                Diagram.Items.Add(shape);
                Diagram.Items.Add(connection);
                addNodes(syntaxNode, shape);
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            new GarbageTranslatorView(treeRoot).Show();
        }
    }
}
