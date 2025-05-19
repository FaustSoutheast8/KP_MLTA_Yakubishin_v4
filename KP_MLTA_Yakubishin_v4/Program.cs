using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace KP_MLTA_Yakubishin
{
    public class MyPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public MyPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", X, Y);
        }
    }

    public class Polygon
    {
        public List<MyPoint> Vertices { get; set; }
        public Polygon(List<MyPoint> vertices)
        {
            Vertices = vertices;
        }

        public double GetArea()
        {
            double area = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                var a = Vertices[i];
                var b = Vertices[(i + 1) % Vertices.Count];
                area += a.X * b.Y - b.X * a.Y;
            }
            return Math.Abs(area / 2);
        }
    }

    public static class GreedyConvexHull
    {
        public static Polygon FindMaxGreedyPolygon(List<MyPoint> points, int size)
        {
            if (points.Count < size) return null;

            double maxArea = 0;
            Polygon bestPolygon = null;

            foreach (var start in points)
            {
                List<MyPoint> currentPoly = new List<MyPoint> { start };

                while (currentPoly.Count < size)
                {
                    MyPoint bestNext = null;
                    double bestArea = -1;

                    foreach (var p in points)
                    {
                        if (currentPoly.Contains(p)) continue;

                        var candidate = new List<MyPoint>(currentPoly) { p };

                        if (!IsConvex(candidate)) continue;

                        double area = new Polygon(candidate).GetArea();

                        if (area > bestArea)
                        {
                            bestArea = area;
                            bestNext = p;
                        }
                    }

                    if (bestNext != null)
                    {
                        currentPoly.Add(bestNext);
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentPoly.Count == size)
                {
                    var poly = new Polygon(currentPoly);
                    double area = poly.GetArea();
                    if (area > maxArea)
                    {
                        maxArea = area;
                        bestPolygon = poly;
                    }
                }
            }

            return bestPolygon;
        }

        private static bool IsConvex(List<MyPoint> poly)
        {
            int n = poly.Count;
            if (n < 3) return true; // 2 або менше завжди вважається допустимим

            bool gotNegative = false;
            bool gotPositive = false;

            for (int i = 0; i < n; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % n];
                var c = poly[(i + 2) % n];

                double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                if (cross < 0) gotNegative = true;
                else if (cross > 0) gotPositive = true;

                if (gotNegative && gotPositive)
                    return false;
            }

            return true;
        }
    }



    public static class BruteForceAreaSearch
    {
        public static Polygon FindMaxPolygon(List<MyPoint> points, int vertexCount)
        {
            if (points.Count < vertexCount) return null;
            var combinations = GetCombinations(points, vertexCount);
            double maxArea = 0;
            Polygon maxPolygon = null;

            foreach (var combo in combinations)
            {
                if (!IsConvex(combo)) continue;
                var poly = new Polygon(combo);
                double area = poly.GetArea();
                if (area > maxArea)
                {
                    maxArea = area;
                    maxPolygon = poly;
                }
            }
            return maxPolygon;
        }

        private static IEnumerable<List<MyPoint>> GetCombinations(List<MyPoint> points, int length)
        {
            int n = points.Count;
            int[] indices = new int[length];
            for (int i = 0; i < length; i++) indices[i] = i;

            while (indices[0] <= n - length)
            {
                List<MyPoint> combination = new List<MyPoint>();
                for (int i = 0; i < length; i++) combination.Add(points[indices[i]]);
                yield return combination;

                int t = length - 1;
                while (t != 0 && indices[t] == n - length + t) t--;
                indices[t]++;
                for (int i = t + 1; i < length; i++) indices[i] = indices[i - 1] + 1;
            }
        }

        private static bool IsConvex(List<MyPoint> poly)
        {
            int n = poly.Count;
            if (n < 3) return false;

            bool gotNegative = false;
            bool gotPositive = false;

            for (int i = 0; i < n; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % n];
                var c = poly[(i + 2) % n];

                double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                if (cross < 0) gotNegative = true;
                else if (cross > 0) gotPositive = true;

                if (gotNegative && gotPositive)
                    return false;
            }

            return true;
        }
    }

    public class MainForm : Form
    {
        private Button generateButton, findButton, clearButton, loadButton;
        private NumericUpDown numPoints;
        private ComboBox methodSelector;
        private PictureBox mainPictureBox, trianglePreviewBox, quadPreviewBox, pentaPreviewBox;
        private Label lblResult;
        private List<MyPoint> points = new List<MyPoint>();

        public MainForm()
        {
            Text = "Polygon Finder";
            Size = new Size(1150, 700);

            numPoints = new NumericUpDown { Location = new Point(10, 10), Width = 60, Minimum = 3, Maximum = 5000, Value = 100 };
            generateButton = new Button { Text = "Згенерувати точки", Location = new Point(80, 10) };
            clearButton = new Button { Text = "Очистити", Location = new Point(210, 10) };
            loadButton = new Button { Text = "Завантажити з файлу", Location = new Point(300, 10) };
            methodSelector = new ComboBox { Location = new Point(450, 10), Width = 150 };
            methodSelector.Items.AddRange(new string[] { "Brute Force", "GreedyConvexHull" });
            methodSelector.SelectedIndex = 0;
            findButton = new Button { Text = "Пошук", Location = new Point(610, 10) };
            lblResult = new Label { Location = new Point(10, 40), Width = 1000 };

            mainPictureBox = new PictureBox { Location = new Point(10, 70), Size = new Size(500, 500), BorderStyle = BorderStyle.FixedSingle };
            trianglePreviewBox = new PictureBox { Location = new Point(520, 70), Size = new Size(180, 180), BorderStyle = BorderStyle.FixedSingle };
            quadPreviewBox = new PictureBox { Location = new Point(710, 70), Size = new Size(180, 180), BorderStyle = BorderStyle.FixedSingle };
            pentaPreviewBox = new PictureBox { Location = new Point(900, 70), Size = new Size(180, 180), BorderStyle = BorderStyle.FixedSingle };

            Controls.AddRange(new Control[] {
                numPoints, generateButton, clearButton, loadButton, methodSelector, findButton, lblResult,
                mainPictureBox, trianglePreviewBox, quadPreviewBox, pentaPreviewBox
            });

            generateButton.Click += GeneratePoints;
            clearButton.Click += Clear;
            loadButton.Click += LoadPointsFromFile;
            findButton.Click += RunAlgorithm;
        }

        private void GeneratePoints(object sender, EventArgs e)
        {
            var rnd = new Random();
            points.Clear();
            for (int i = 0; i < numPoints.Value; i++)
            {
                double x = rnd.Next(-150, 151);
                double y = rnd.Next(-150, 151);
                points.Add(new MyPoint(x, y));
            }
            Redraw();
        }

        private void Clear(object sender, EventArgs e)
        {
            points.Clear();
            mainPictureBox.Image = null;
            trianglePreviewBox.Image = null;
            quadPreviewBox.Image = null;
            pentaPreviewBox.Image = null;
            lblResult.Text = "";
        }

        private void LoadPointsFromFile(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string[] lines = File.ReadAllLines(ofd.FileName);
                points.Clear();
                foreach (string line in lines)
                {
                    var match = Regex.Match(line, @"x=([-]?[0-9]+),\s*y=([-]?[0-9]+)");
                    if (match.Success)
                    {
                        double x = double.Parse(match.Groups[1].Value);
                        double y = double.Parse(match.Groups[2].Value);
                        points.Add(new MyPoint(x, y));
                    }
                }
                Redraw();
            }
        }

        private void RunAlgorithm(object sender, EventArgs e)
        {
            if (points.Count < 3)
            {
                MessageBox.Show("Недостатньо точок");
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Polygon triangle = null, quad = null, penta = null;
            if (methodSelector.SelectedItem.ToString() == "Brute Force")
            {
                triangle = BruteForceAreaSearch.FindMaxPolygon(points, 3);
                quad = BruteForceAreaSearch.FindMaxPolygon(points, 4);
                penta = BruteForceAreaSearch.FindMaxPolygon(points, 5);
            }
            else
            {
                triangle = GreedyConvexHull.FindMaxGreedyPolygon(points, 3);
                quad = GreedyConvexHull.FindMaxGreedyPolygon(points, 4);
                penta = GreedyConvexHull.FindMaxGreedyPolygon(points, 5);
            }

            sw.Stop();

            lblResult.Text = $"Метод: {methodSelector.SelectedItem}, Час: {sw.ElapsedMilliseconds} мс | Площі: Трикутник={triangle?.GetArea():F2}, Чотирикутник={quad?.GetArea():F2}, П’ятикутник={penta?.GetArea():F2}";

            mainPictureBox.Image = DrawScene(points, triangle, quad, penta, mainPictureBox.Size);
            trianglePreviewBox.Image = DrawScene(new List<MyPoint>(), triangle, null, null, trianglePreviewBox.Size);
            quadPreviewBox.Image = DrawScene(new List<MyPoint>(), null, quad, null, quadPreviewBox.Size);
            pentaPreviewBox.Image = DrawScene(new List<MyPoint>(), null, null, penta, pentaPreviewBox.Size);
        }

        private void Redraw()
        {
            mainPictureBox.Image = DrawScene(points, null, null, null, mainPictureBox.Size);
        }

        private Bitmap DrawScene(List<MyPoint> pts, Polygon triangle, Polygon quad, Polygon penta, Size customSize)
        {
            Bitmap bmp = new Bitmap(customSize.Width, customSize.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            var allPoints = new List<MyPoint>(pts);
            if (allPoints.Count == 0)
            {
                if (triangle != null) allPoints.AddRange(triangle.Vertices);
                if (quad != null) allPoints.AddRange(quad.Vertices);
                if (penta != null) allPoints.AddRange(penta.Vertices);
            }

            if (allPoints.Count == 0)
            {
                g.Dispose();
                return bmp;
            }

            double minX = allPoints.Min(p => p.X);
            double maxX = allPoints.Max(p => p.X);
            double minY = allPoints.Min(p => p.Y);
            double maxY = allPoints.Max(p => p.Y);

            double paddingRatio = 0.1;
            double dx = maxX - minX;
            double dy = maxY - minY;
            if (dx == 0) dx = 1;
            if (dy == 0) dy = 1;

            double scaleX = (1 - 2 * paddingRatio) * customSize.Width / dx;
            double scaleY = (1 - 2 * paddingRatio) * customSize.Height / dy;
            double scale = Math.Min(scaleX, scaleY);

            double offsetX = -minX * scale + customSize.Width * paddingRatio;
            double offsetY = -minY * scale + customSize.Height * paddingRatio;

            foreach (var p in pts)
            {
                float x = (float)(p.X * scale + offsetX);
                float y = (float)(p.Y * scale + offsetY);
                g.FillEllipse(Brushes.Blue, x - 2, y - 2, 4, 4);
            }

            DrawPolygon(g, triangle, Pens.Red, scale, offsetX, offsetY);
            DrawPolygon(g, quad, Pens.Green, scale, offsetX, offsetY);
            DrawPolygon(g, penta, Pens.Orange, scale, offsetX, offsetY);

            g.Dispose();
            return bmp;
        }

        private void DrawPolygon(Graphics g, Polygon poly, Pen pen, double scale, double offsetX, double offsetY)
        {
            if (poly == null) return;
            for (int i = 0; i < poly.Vertices.Count; i++)
            {
                var a = poly.Vertices[i];
                var b = poly.Vertices[(i + 1) % poly.Vertices.Count];
                g.DrawLine(pen,
                    (float)(a.X * scale + offsetX),
                    (float)(a.Y * scale + offsetY),
                    (float)(b.X * scale + offsetX),
                    (float)(b.Y * scale + offsetY));
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}