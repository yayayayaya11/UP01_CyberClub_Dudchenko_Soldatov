using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CyberClub
{
    // Диаграммы выручки и сохранение отчётов.
    public partial class MainWindow
    {
        public void ExportData()
        {
            Service.RequireAdmin();
            var dialog = new SaveFileDialog
            {
                Title = "Сохранить отчёт",
                FileName = selected.Table + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                Filter = "Документ Word|*.docx|PDF|*.pdf|Книга Excel|*.xlsx"
            };
            if (dialog.ShowDialog(this) == true)
            {
                var view = table.ItemsSource as DataView;
                Exporter.Save(dialog.FileName, selected.Title, view.ToTable());
                Notify("Отчёт сохранён: " + dialog.FileName);
            }
        }

        // Показать выручку по клубам и элементы выбора периода.
        public void ShowAnalytics()
        {
            Service.RequireAdmin();
            var root = new Grid
            {
                Margin = new Thickness(28)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.Children.Add(Text("Выручка клубов по платежам", 24));
            var filters = new WrapPanel();
            Grid.SetRow(filters, 1);
            root.Children.Add(filters);
            var clubs = new ComboBox
            {
                Width = 240,
                Margin = new Thickness(5),
                DisplayMemberPath = "Label",
                SelectedValuePath = "Id",
                ToolTip = "Все клубы или один клуб"
            };
            var ct = Catalog.Lookup("Clubs");
            var all = ct.NewRow();
            all["Id"] = 0;
            all["Label"] = "Все клубы";
            ct.Rows.InsertAt(all, 0);
            clubs.ItemsSource = ct.DefaultView;
            clubs.SelectedIndex = 0;
            filters.Children.Add(clubs);
            var from = new DatePicker
            {
                SelectedDate = new DateTime(2026, 10, 1),
                Width = 150,
                Margin = new Thickness(5),
                ToolTip = "Начало периода включительно"
            };
            var to = new DatePicker
            {
                SelectedDate = DateTime.Today.AddDays(1),
                Width = 150,
                Margin = new Thickness(5),
                ToolTip = "Конец периода включительно"
            };
            filters.Children.Add(from);
            filters.Children.Add(to);
            var kind = new ComboBox
            {
                Width = 160,
                Margin = new Thickness(5),
                ItemsSource = new[]
                {
                    "Столбцы",
                    "Круговая"
                },
                SelectedIndex = 0,
                ToolTip = "Тип диаграммы"
            };
            filters.Children.Add(kind);
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(body, 2);
            root.Children.Add(body);
            var canvas = new Canvas
            {
                MinHeight = 270,
                Margin = new Thickness(15),
                Background = Brushes.White,
                ClipToBounds = true
            };
            body.Children.Add(canvas);
            var grid = new DataGrid
            {
                Margin = new Thickness(10)
            };
            Grid.SetColumn(grid, 1);
            body.Children.Add(grid);
            var total = Text("");
            var bottom = new WrapPanel();
            bottom.Children.Add(total);
            Grid.SetRow(bottom, 3);
            root.Children.Add(bottom);
            DataTable result = null;
            Action update = () =>
            {
                if (!from.SelectedDate.HasValue || !to.SelectedDate.HasValue || from.SelectedDate > to.SelectedDate)
                    throw new Exception("Укажите корректный период.");
                int c = Convert.ToInt32(clubs.SelectedValue);
                result = Service.Revenue(c == 0 ? (int? )null : c, from.SelectedDate.Value, to.SelectedDate.Value.AddDays(1));
                grid.ItemsSource = result.DefaultView;
                total.Text = "Итого: " + result.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r[1])).ToString("N2") + " руб.   ";
                DrawChart(canvas, result, kind.SelectedIndex == 1);
            };
            filters.Children.Add(Button("Построить", update));
            bottom.Children.Add(Button("Экспорт отчёта", () =>
            {
                if (result == null)
                    update();
                var d = new SaveFileDialog
                {
                    FileName = "Выручка_клубов",
                    Filter = "Word|*.docx|PDF|*.pdf|Excel|*.xlsx"
                };
                if (d.ShowDialog(this) == true)
                {
                    Exporter.Save(d.FileName, "Выручка с " + from.SelectedDate.Value.ToShortDateString() + " по " + to.SelectedDate.Value.ToShortDateString(), result);
                    Notify("Отчёт сохранён.");
                }
            }));
            canvas.SizeChanged += (s, e) =>
            {
                if (result != null)
                    DrawChart(canvas, result, kind.SelectedIndex == 1);
            };
            kind.SelectionChanged += (s, e) =>
            {
                if (result != null)
                    DrawChart(canvas, result, kind.SelectedIndex == 1);
            };
            Navigate("Диаграммы и отчёты", root);
            update();
        }

        public static void DrawChart(Canvas canvas, DataTable data, bool pie)
        {
            canvas.Children.Clear();
            double w = Math.Max(350, canvas.ActualWidth), h = Math.Max(260, canvas.ActualHeight);
            var colors = new[]
            {
                Color.FromRgb(1, 152, 175),
                Color.FromRgb(35, 62, 118),
                Color.FromRgb(77, 198, 197)
            };
            double total = data.Rows.Cast<DataRow>().Sum(r => Convert.ToDouble(r[1])), max = data.Rows.Cast<DataRow>().Select(r => Convert.ToDouble(r[1])).DefaultIfEmpty(0).Max();
            if (total == 0)
            {
                var empty = Text("За период платежей нет", 18);
                empty.Foreground = Brushes.Black;
                Canvas.SetLeft(empty, 20);
                Canvas.SetTop(empty, 20);
                canvas.Children.Add(empty);
                return;
            }

            double angle = -90;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                double value = Convert.ToDouble(data.Rows[i][1]);
                var brush = new SolidColorBrush(colors[i % colors.Length]);
                if (pie)
                {
                    double sweep = value / total * 360, cx = w * .40, cy = h * .43, r = Math.Min(w * .30, h * .32);
                    if (sweep >= 359.999)
                    {
                        var ellipse = new Ellipse
                        {
                            Width = r * 2,
                            Height = r * 2,
                            Fill = brush
                        };
                        Canvas.SetLeft(ellipse, cx - r);
                        Canvas.SetTop(ellipse, cy - r);
                        canvas.Children.Add(ellipse);
                    }
                    else if (sweep > 0)
                    {
                        var a = new Point(cx + r * Math.Cos(angle * Math.PI / 180), cy + r * Math.Sin(angle * Math.PI / 180));
                        var b = new Point(cx + r * Math.Cos((angle + sweep) * Math.PI / 180), cy + r * Math.Sin((angle + sweep) * Math.PI / 180));
                        var figure = new PathFigure
                        {
                            StartPoint = new Point(cx, cy),
                            IsClosed = true
                        };
                        figure.Segments.Add(new LineSegment(a, true));
                        figure.Segments.Add(new ArcSegment(b, new Size(r, r), 0, sweep > 180, SweepDirection.Clockwise, true));
                        canvas.Children.Add(new System.Windows.Shapes.Path { Fill = brush, Data = new PathGeometry(new[] { figure }) });
                    }

                    angle += sweep;
                }
                else
                {
                    double bw = (w - 60) / data.Rows.Count, bh = (h - 110) * value / max;
                    var rect = new Rectangle
                    {
                        Width = Math.Max(16, bw - 22),
                        Height = bh,
                        Fill = brush
                    };
                    Canvas.SetLeft(rect, 35 + i * bw);
                    Canvas.SetTop(rect, h - 75 - bh);
                    canvas.Children.Add(rect);
                    var amount = Text(value.ToString("N2"), 12);
                    amount.Foreground = Brushes.Black;
                    Canvas.SetLeft(amount, 35 + i * bw);
                    Canvas.SetTop(amount, h - 100 - bh);
                    canvas.Children.Add(amount);
                }

                var label = Text(Convert.ToString(data.Rows[i][0]) + ": " + value.ToString("N2") + " руб.", 11);
                label.Foreground = Brushes.Black;
                label.Width = pie ? w - 20 : Math.Max(60, (w - 60) / data.Rows.Count - 6);
                Canvas.SetLeft(label, pie ? 12 : 35 + i * (w - 60) / data.Rows.Count);
                Canvas.SetTop(label, pie ? h - 75 + i * 22 : h - 70);
                canvas.Children.Add(label);
            }
        }
    }
}
