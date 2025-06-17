using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GraphAnalyzer
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, Ellipse> nodes = new();
        private Dictionary<string, List<string>> adjacencyList = new();
        private int nodeCount = 0;
        private Ellipse selectedNode = null;
        private bool isAddingVertex = false;

        private DispatcherTimer dfsTimer;
        private Stack<string> dfsStack;
        private HashSet<string> dfsVisited;

        private DispatcherTimer bfsTimer;
        private Queue<string> bfsQueue;
        private HashSet<string> bfsVisited;

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void AddVertex_Click(object sender, RoutedEventArgs e)
        {
            isAddingVertex = true;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isAddingVertex) return;

            Point position = e.GetPosition(GraphCanvas);
            string nodeName = $"V{nodeCount++}";

            var node = new Ellipse
            {
                Width = 30,
                Height = 30,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetZIndex(node, 1);

            var label = new TextBlock
            {
                Text = nodeName,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(node, position.X - 15);
            Canvas.SetTop(node, position.Y - 15);
            Canvas.SetLeft(label, position.X - 10);
            Canvas.SetTop(label, position.Y - 10);

            node.MouseLeftButtonDown += Node_MouseLeftButtonDown;

            GraphCanvas.Children.Add(node);
            GraphCanvas.Children.Add(label);

            nodes[nodeName] = node;
            adjacencyList[nodeName] = new List<string>();
            isAddingVertex = false;
        }

        private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedNode = sender as Ellipse;
            string clickedNodeName = nodes.FirstOrDefault(x => x.Value == clickedNode).Key;

            if (selectedNode != null)
            {
                string selectedNodeName = nodes.FirstOrDefault(x => x.Value == selectedNode).Key;
                ConnectNodes(selectedNodeName, clickedNodeName);
                selectedNode = null;
            }
            else
            {
                selectedNode = clickedNode;
            }
        }

        private void ConnectNodes(string from, string to)
        {
            if (!adjacencyList.ContainsKey(from) || !adjacencyList.ContainsKey(to)) return;

            if (!adjacencyList[from].Contains(to))
                adjacencyList[from].Add(to);
            if (!adjacencyList[to].Contains(from))
                adjacencyList[to].Add(from);

            if (FindEdge(from, to) != null) return;

            var edge = new Line
            {
                X1 = Canvas.GetLeft(nodes[from]) + 15,
                Y1 = Canvas.GetTop(nodes[from]) + 15,
                X2 = Canvas.GetLeft(nodes[to]) + 15,
                Y2 = Canvas.GetTop(nodes[to]) + 15,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            GraphCanvas.Children.Add(edge);
        }

        private Line FindEdge(string from, string to)
        {
            foreach (var child in GraphCanvas.Children)
            {
                if (child is Line line)
                {
                    double x1 = Canvas.GetLeft(nodes[from]) + 15;
                    double y1 = Canvas.GetTop(nodes[from]) + 15;
                    double x2 = Canvas.GetLeft(nodes[to]) + 15;
                    double y2 = Canvas.GetTop(nodes[to]) + 15;

                    if ((line.X1 == x1 && line.Y1 == y1 && line.X2 == x2 && line.Y2 == y2) ||
                        (line.X1 == x2 && line.Y1 == y2 && line.X2 == x1 && line.Y2 == y1))
                    {
                        return line;
                    }
                }
            }
            return null;
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            GraphCanvas.Children.Clear();
            nodes.Clear();
            adjacencyList.Clear();
        }

        private void CheckCycles_Click(object sender, RoutedEventArgs e)
        {
            bool hasCycle = false;
            var visited = new HashSet<string>();

            foreach (var node in nodes.Keys)
            {
                if (!visited.Contains(node) && HasCycle(node, null, visited))
                {
                    hasCycle = true;
                    break;
                }
            }

            MessageBox.Show(hasCycle ? "В графі є цикли." : "В графі немає циклів.");
        }

        private bool HasCycle(string node, string parent, HashSet<string> visited)
        {
            visited.Add(node);
            foreach (var neighbor in adjacencyList[node])
            {
                if (!visited.Contains(neighbor))
                {
                    if (HasCycle(neighbor, node, visited)) return true;
                }
                else if (neighbor != parent)
                {
                    return true;
                }
            }
            return false;
        }

        private void DFS_Click(object sender, RoutedEventArgs e)
        {
            if (nodes.Count == 0)
            {
                MessageBox.Show("Граф порожній.");
                return;
            }

            ResetNodeColors();
            string startNode = nodes.Keys.First();
            dfsStack = new Stack<string>();
            dfsVisited = new HashSet<string>();
            dfsStack.Push(startNode);

            dfsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            dfsTimer.Tick += (s, args) =>
            {
                if (dfsStack.Count > 0)
                {
                    string node = dfsStack.Pop();
                    if (dfsVisited.Contains(node)) return;

                    nodes[node].Fill = Brushes.Yellow;
                    dfsVisited.Add(node);

                    foreach (var neighbor in adjacencyList[node])
                    {
                        if (!dfsVisited.Contains(neighbor))
                            dfsStack.Push(neighbor);
                    }
                }
                else
                {
                    dfsTimer.Stop();
                    MessageBox.Show("DFS обхід завершено.");
                    ResetNodeColors();
                }
            };
            dfsTimer.Start();
        }

        private void BFS_Click(object sender, RoutedEventArgs e)
        {
            if (nodes.Count == 0)
            {
                MessageBox.Show("Граф порожній.");
                return;
            }

            ResetNodeColors();
            string startNode = nodes.Keys.First();
            bfsQueue = new Queue<string>();
            bfsVisited = new HashSet<string>();
            bfsQueue.Enqueue(startNode);

            bfsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            bfsTimer.Tick += (s, args) =>
            {
                if (bfsQueue.Count > 0)
                {
                    string node = bfsQueue.Dequeue();
                    if (bfsVisited.Contains(node)) return;

                    nodes[node].Fill = Brushes.Green;
                    bfsVisited.Add(node);

                    foreach (var neighbor in adjacencyList[node])
                    {
                        if (!bfsVisited.Contains(neighbor))
                            bfsQueue.Enqueue(neighbor);
                    }
                }
                else
                {
                    bfsTimer.Stop();
                    MessageBox.Show("BFS обхід завершено.");
                    ResetNodeColors();
                }
            };
            bfsTimer.Start();
        }

        private void ResetNodeColors()
        {
            foreach (var node in nodes.Values)
                node.Fill = Brushes.LightBlue;
        }
    }
}
