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
    // Просмотр таблиц, поиск, фильтрация и редактор записей.
    public partial class MainWindow
    {
        // Показать раздел: навигацию, поиск, фильтры, действия и таблицу.
        public void ShowData(Entity e)
        {
            selected = e;
            refreshing = true;
            source = Service.List(e);
            var root = new Grid
            {
                Margin = new Thickness(14)
            };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(165) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var nav = new StackPanel();
            nav.Children.Add(Text("РАЗДЕЛЫ", 12));
            foreach (var entity in Catalog.All)
            {
                var item = entity;
                nav.Children.Add(Button(item.Title, () => ShowData(item)));
            }

            if (Auth.Current.IsAdmin)
                nav.Children.Add(Button("Диаграммы и отчёты", ShowAnalytics));
            nav.Children.Add(Button("Смена пароля", ShowPassword));
            root.Children.Add(Scroll(nav));
            var content = new Grid
            {
                Margin = new Thickness(14, 0, 0, 0)
            };
            Grid.SetColumn(content, 1);
            root.Children.Add(content);
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.Children.Add(Text(e.Title, 24));
            var tools = new WrapPanel();
            Grid.SetRow(tools, 1);
            content.Children.Add(tools);
            search = Input("Поиск по всем столбцам", "", 100);
            search.Width = 230;
            search.Margin = new Thickness(4);
            tools.Children.Add(search);
            filterColumn = new ComboBox
            {
                Width = 150,
                Margin = new Thickness(4),
                ToolTip = "Столбец для точного фильтра"
            };
            filterColumn.Items.Add("Без фильтра");
            foreach (DataColumn col in source.Columns)
                filterColumn.Items.Add(col.ColumnName);
            filterColumn.SelectedIndex = 0;
            tools.Children.Add(filterColumn);
            filterValue = new ComboBox
            {
                Width = 190,
                Margin = new Thickness(4),
                ToolTip = "Значение выбранного столбца"
            };
            tools.Children.Add(filterValue);
            sortColumn = new ComboBox
            {
                Width = 145,
                Margin = new Thickness(4),
                ToolTip = "Сортировать по столбцу"
            };
            foreach (DataColumn col in source.Columns)
                sortColumn.Items.Add(col.ColumnName);
            sortColumn.SelectedIndex = 0;
            tools.Children.Add(sortColumn);
            sortDirection = new ComboBox
            {
                Width = 130,
                Margin = new Thickness(4),
                ToolTip = "Направление сортировки"
            };
            sortDirection.ItemsSource = new[]
            {
                "По возрастанию",
                "По убыванию"
            };
            sortDirection.SelectedIndex = 0;
            tools.Children.Add(sortDirection);
            var actions = new WrapPanel();
            Grid.SetRow(actions, 2);
            content.Children.Add(actions);
            if (Auth.Current.IsAdmin)
            {
                actions.Children.Add(Button("Добавить", () => ShowEditor(e, null)));
                actions.Children.Add(Button("Редактировать", () => ShowEditor(e, SelectedId())));
                actions.Children.Add(Button("Удалить", () =>
                {
                    int id = SelectedId();
                    if (Confirm("Удалить запись №" + id + " из раздела «" + e.Title + "»?\nСвязанные записи будут удалены каскадно. Отменить это действие нельзя."))
                    {
                        Service.Delete(e, id);
                        ShowData(e);
                        Notify("Запись и связанные данные удалены.");
                    }
                }));
                actions.Children.Add(Button("Экспорт отчёта", () => ExportData()));
            }

            actions.Children.Add(Button("Обновить", () => ShowData(e)));
            table = new DataGrid
            {
                ItemsSource = source.DefaultView
            };
            Grid.SetRow(table, 3);
            content.Children.Add(table);
            table.AutoGeneratingColumn += (s, args) =>
            {
                var column = args.Column as DataGridTextColumn;
                if (column != null)
                {
                    var style = new Style(typeof(TextBlock));
                    style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(7, 3, 7, 3)));
                    style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    column.ElementStyle = style;
                    if (args.PropertyType == typeof(DateTime))
                        column.Binding = new Binding(args.PropertyName)
                        {
                            StringFormat = "dd.MM.yyyy HH:mm"
                        };
                }
            };
            var count = Text("Записей: " + source.Rows.Count + ". Для сортировки также нажмите заголовок столбца.", 12);
            Grid.SetRow(count, 4);
            content.Children.Add(count);
            Action apply = () =>
            {
                if (refreshing)
                    return;
                ApplyFilters();
                count.Text = "Показано: " + ((DataView)table.ItemsSource).Count + " из " + source.Rows.Count + ". " + (Auth.Current.IsAdmin ? "Добавление, редактирование и удаление доступны." : "Режим просмотра.");
            };
            search.TextChanged += (s, args) => apply();
            filterColumn.SelectionChanged += (s, args) =>
            {
                refreshing = true;
                filterValue.Items.Clear();
                filterValue.Items.Add("Все значения");
                if (filterColumn.SelectedIndex > 0)
                {
                    string col = (string)filterColumn.SelectedItem;
                    foreach (string val in source.Rows.Cast<DataRow>().Select(r => Format(r[col])).Distinct().OrderBy(x => x))
                        filterValue.Items.Add(val);
                }

                filterValue.SelectedIndex = 0;
                refreshing = false;
                apply();
            };
            filterValue.SelectionChanged += (s, args) => apply();
            sortColumn.SelectionChanged += (s, args) => apply();
            sortDirection.SelectionChanged += (s, args) => apply();
            refreshing = false;
            Navigate((Auth.Current.IsAdmin ? "Администратор • " : "Пользователь • ") + e.Title, root);
        }

        public static string Format(object value)
        {
            return value is DateTime ? ((DateTime)value).ToString("dd.MM.yyyy HH:mm") : value is decimal ? ((decimal)value).ToString("0.00", CultureInfo.GetCultureInfo("ru-RU")) : Convert.ToString(value);
        }

        // Применить поиск, точный фильтр и сортировку к загруженным строкам.
        public void ApplyFilters()
        {
            string q = search.Text.Trim();
            string fc = filterColumn.SelectedIndex > 0 ? Convert.ToString(filterColumn.SelectedItem) : null;
            string fv = filterValue.SelectedIndex > 0 ? Convert.ToString(filterValue.SelectedItem) : null;
            var t = source.Clone();
            foreach (DataRow r in source.Rows)
            {
                if (q != "" && !source.Columns.Cast<DataColumn>().Any(c => Format(r[c]).IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    continue;
                if (fc != null && fv != null && Format(r[fc]) != fv)
                    continue;
                t.ImportRow(r);
            }

            string sort = Convert.ToString(sortColumn.SelectedItem);
            if (sort != "")
                t.DefaultView.Sort = "[" + sort.Replace("]", "") + "] " + (sortDirection.SelectedIndex == 1 ? "DESC" : "ASC");
            table.ItemsSource = t.DefaultView;
        }

        int SelectedId()
        {
            var row = table.SelectedItem as DataRowView;
            if (row == null)
                throw new Exception("Выберите запись в таблице.");
            return Convert.ToInt32(row["Id"]);
        }

        // Построить форму по описанию полей из Catalog.
        public void ShowEditor(Entity e, int? id)
        {
            Service.RequireAdmin();
            var data = id.HasValue ? Service.Read(e, id.Value) : new Dictionary<string, object>();
            var p = Form((id.HasValue ? "Изменить • " : "Добавить • ") + e.Title, 560);
            var controls = new Dictionary<Field, Control>();
            foreach (var f in e.Fields)
            {
                Control control;
                object value = data.ContainsKey(f.Key) ? data[f.Key] : null;
                if (f.Kind == "ref")
                {
                    var c = new ComboBox
                    {
                        ItemsSource = Catalog.Lookup(f.Source).DefaultView,
                        DisplayMemberPath = "Label",
                        SelectedValuePath = "Id",
                        ToolTip = "Выберите: " + f.Title
                    };
                    if (value != null)
                        c.SelectedValue = value;
                    control = c;
                }
                else if (f.Kind == "enum")
                {
                    var c = new ComboBox
                    {
                        ItemsSource = f.Source.Split('|'),
                        ToolTip = f.Title
                    };
                    c.SelectedItem = value ?? f.Source.Split('|')[0];
                    control = c;
                }
                else if (f.Kind == "password")
                    control = Password("Оставьте пустым, чтобы сохранить текущий пароль");
                else
                {
                    var tb = Input(f.Kind == "date" ? "18.10.2026 12:00" : f.Title, value == null ? (f.Kind == "date" ? DateTime.Now.AddMinutes(60).ToString("dd.MM.yyyy HH:mm") : "") : Format(value), f.Max);
                    if (f.Kind == "money" || f.Kind == "amount")
                        tb.PreviewTextInput += (s, a) => a.Handled = a.Text.Any(ch => !Char.IsDigit(ch) && ch != ',' && ch != '.');
                    if (f.Kind == "phone")
                        tb.PreviewTextInput += (s, a) => a.Handled = a.Text.Any(ch => !Char.IsDigit(ch) && ch != '+');
                    control = tb;
                }

                Label(p, f.Title, control);
                controls.Add(f, control);
            }

            if (e.Table == "Bookings")
                p.Children.Add(Text("Стоимость рассчитывается по минутам. Цена тарифа фиксируется при создании брони; смена тарифа обновит её. Пересечения времени запрещены."));
            var buttons = new WrapPanel();
            buttons.Children.Add(Button("Сохранить", () =>
            {
                var values = new Dictionary<string, object>();
                foreach (var pair in controls)
                {
                    var f = pair.Key;
                    var c = pair.Value;
                    string s = c is PasswordBox ? ((PasswordBox)c).Password : c is ComboBox ? (f.Kind == "ref" ? Convert.ToString(((ComboBox)c).SelectedValue) : Convert.ToString(((ComboBox)c).SelectedItem)) : ((TextBox)c).Text;
                    values[f.Key] = f.Kind == "password" ? s : ClubService.Parse(f, s);
                }

                Service.Save(e, id, values);
                ShowData(e);
                Notify("Данные сохранены.");
            }));
            buttons.Children.Add(Button("Отмена", () => ShowData(e)));
            p.Children.Add(buttons);
            Navigate((id.HasValue ? "Редактирование • " : "Добавление • ") + e.Title, Scroll(p));
        }
    }
}
