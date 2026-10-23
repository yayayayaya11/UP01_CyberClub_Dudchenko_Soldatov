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
    // Обработка ошибок, сообщения и подтверждения действий.
    public partial class MainWindow
    {
        public void Run(Action a)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Notify(ErrorText(ex));
            }
        }

        // Преобразовать вложенную ошибку базы данных в понятное сообщение.
        public static string ErrorText(Exception ex)
        {
            System.Data.SqlClient.SqlException sql = null;
            for (var current = ex; current != null; current = current.InnerException)
            {
                sql = current as System.Data.SqlClient.SqlException;
                if (sql != null)
                    break;
            }

            if (sql != null)
            {
                if (sql.Number == 2601 || sql.Number == 2627)
                    return "Такое значение уже существует. Проверьте уникальные поля.";
                if (sql.Number == 547)
                    return "Нарушено ограничение данных. Проверьте значения и связанные записи.";
                if (sql.Number < 50000)
                    return "Ошибка подключения или операции с базой. Проверьте LocalDB и наличие базы. Подробности: " + sql.Message;
                return sql.Message;
            }

            var validation = ex as System.Data.Entity.Validation.DbEntityValidationException;
            if (validation != null)
                return String.Join("\n", validation.EntityValidationErrors.SelectMany(e => e.ValidationErrors).Select(e => e.PropertyName + ": " + e.ErrorMessage));
            return ex.Message;
        }

        Window MessageWindow(string message, bool question)
        {
            var p = new StackPanel
            {
                Margin = new Thickness(22)
            };
            p.Children.Add(Text(question ? "Подтверждение действия" : "Сообщение CyberClub", 20));
            p.Children.Add(Text(message, 15));
            var buttons = new WrapPanel
            {
                Margin = new Thickness(0, 14, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var surface = new Border
            {
                Child = p
            };
            surface.SetResourceReference(Border.BackgroundProperty, "BackgroundBrush");
            var w = new Window
            {
                Owner = this,
                Title = question ? "Подтверждение" : "CyberClub",
                Width = 620,
                SizeToContent = SizeToContent.Height,
                MinHeight = 190,
                MaxHeight = 550,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                FontFamily = FontFamily,
                Content = surface
            };
            if (question)
            {
                buttons.Children.Add(Button("Да", () =>
                {
                    w.DialogResult = true;
                }));
                buttons.Children.Add(Button("Нет", () =>
                {
                    w.DialogResult = false;
                }));
            }
            else
                buttons.Children.Add(Button("ОК", () => w.Close()));
            p.Children.Add(buttons);
            return w;
        }

        public void Notify(string message)
        {
            LastMessage = message;
            var w = MessageWindow(message, false);
            if (EvidenceMode)
            {
                w.Show();
                Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                }));
                SnapshotElement((FrameworkElement)w.Content, "message_" + (EvidenceIndex++));
                w.Close();
            }
            else
                w.ShowDialog();
        }

        public bool Confirm(string message)
        {
            LastMessage = message;
            var w = MessageWindow(message, true);
            if (EvidenceMode)
            {
                w.Show();
                Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                {
                }));
                SnapshotElement((FrameworkElement)w.Content, "confirm_" + (EvidenceIndex++));
                w.Close();
                return false;
            }

            return w.ShowDialog() == true;
        }
    }
}
