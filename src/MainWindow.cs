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
    public partial class MainWindow : Window
    {
        public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
        public readonly AuthService Auth = new AuthService();
        public readonly ClubService Service;
        readonly Grid shell;
        readonly Frame frame;
        readonly TextBlock title, account, clock;
        readonly DispatcherTimer timer = new DispatcherTimer();
        Entity selected = Catalog.All[0];
        DataGrid table;
        TextBox search;
        ComboBox filterColumn, filterValue, sortColumn, sortDirection;
        DataTable source;
        bool refreshing, dark;
        public bool EvidenceMode;
        public string LastMessage;
        public int EvidenceIndex;
        public MainWindow()
        {
            Service = new ClubService(Auth);
            Width = 1240;
            Height = 740;
            MinWidth = 1040;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            using (var s = File.OpenRead(System.IO.Path.Combine(Root, "MainWindow.xaml")))
                shell = (Grid)XamlReader.Load(s);
            Content = shell;
            frame = (Frame)shell.FindName("MainFrame");
            title = (TextBlock)shell.FindName("PageTitle");
            account = (TextBlock)shell.FindName("AccountLabel");
            clock = (TextBlock)shell.FindName("Clock");
            foreach (var label in new[]
            {
                title,
                account,
                clock
            }

            )
                label.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
            ((Button)shell.FindName("BackButton")).Click += (s, e) => Run(() =>
            {
                if (Auth.Current == null)
                    ShowLogin();
                else
                    ShowData(selected);
            });
            ((Button)shell.FindName("ThemeButton")).Click += (s, e) => Run(() =>
            {
                SetTheme(!dark);
            });
            ((Button)shell.FindName("LogoutButton")).Click += (s, e) => Run(() =>
            {
                if (Auth.Current != null && Confirm("Выйти из учётной записи?"))
                {
                    Auth.Logout();
                    ShowLogin();
                }
            });
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => clock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            timer.Start();
            clock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            Closing += (s, e) =>
            {
                if (!EvidenceMode && !Confirm("Вы действительно хотите выйти?"))
                    e.Cancel = true;
            };
            Closed += (s, e) => timer.Stop();
            dark = File.Exists(System.IO.Path.Combine(Root, "theme.txt")) && File.ReadAllText(System.IO.Path.Combine(Root, "theme.txt")).Trim() == "dark";
            SetTheme(dark);
            ShowLogin();
        }

        public void SetTheme(bool value)
        {
            dark = value;
            Application.Current.Resources.MergedDictionaries.Clear();
            foreach (var file in new[]
            {
                value ? "OwnDictionary.xaml" : "Dictionary.xaml",
                "Controls.xaml"
            }

            )
                using (var s = File.OpenRead(System.IO.Path.Combine(Root, "Themes", file)))
                    Application.Current.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(s));
            FontFamily = new FontFamily(new Uri(Root), "./res/fonts/#Open Sans");
            File.WriteAllText(System.IO.Path.Combine(Root, "theme.txt"), value ? "dark" : "light");
        }

        public static TextBlock Text(string s, int size = 13)
        {
            return new TextBlock
            {
                Text = s,
                FontSize = size,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 5)
            };
        }

        public static Button Button(string text, Action action, string hint = null)
        {
            var b = new Button
            {
                Content = text,
                ToolTip = hint ?? text
            };
            b.Click += (s, e) =>
            {
                var w = Application.Current.MainWindow as MainWindow;
                if (w != null)
                    w.Run(action);
                else
                    action();
            };
            return b;
        }

        public static TextBox Input(string placeholder, string value = "", int max = 100)
        {
            return new TextBox
            {
                Tag = placeholder,
                Text = value ?? "",
                MaxLength = max,
                ToolTip = placeholder
            };
        }

        public static PasswordBox Password(string hint)
        {
            return new PasswordBox
            {
                MaxLength = 64,
                ToolTip = hint
            };
        }

        public static void Label(Panel p, string text, UIElement input)
        {
            p.Children.Add(Text(text));
            p.Children.Add(input);
        }

        public void Navigate(string name, UIElement body)
        {
            Title = "CyberClub — " + name;
            title.Text = "CyberClub • " + name;
            var p = new Page
            {
                Content = body,
                FontFamily = FontFamily
            };
            frame.Navigate(p);
            frame.RemoveBackEntry();
            account.Text = Auth.Current == null ? "Сеть киберспортивных клубов" : Auth.Current.FullName + " • " + Auth.Current.Role;
            ((Button)shell.FindName("LogoutButton")).IsEnabled = Auth.Current != null;
        }

        StackPanel Form(string heading, int width = 460)
        {
            var p = new StackPanel
            {
                Width = width,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 22, 20, 20)
            };
            var h = Text(heading, 26);
            h.FontWeight = FontWeights.SemiBold;
            p.Children.Add(h);
            return p;
        }

        ScrollViewer Scroll(UIElement p)
        {
            return new ScrollViewer
            {
                Content = p,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }
    }
}
