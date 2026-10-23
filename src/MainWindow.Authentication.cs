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
    // Страницы входа, регистрации, смены пароля и изображение капчи.
    public partial class MainWindow
    {
        public void ShowLogin()
        {
            var p = Form("Вход в CyberClub");
            p.Children.Add(Text("Управление сетью клубов, бронированиями и оплатой."));
            var login = Input("Например, admin", "", 40);
            var password = Password("Введите пароль");
            Label(p, "Логин", login);
            Label(p, "Пароль", password);
            var captcha = new StackPanel();
            var image = new Canvas
            {
                Width = 280,
                Height = 64,
                Background = Brushes.White
            };
            var answer = Input("Символы с изображения", "", 5);
            Action redraw = () =>
            {
                captcha.Visibility = Auth.NeedsCaptcha ? Visibility.Visible : Visibility.Collapsed;
                DrawCaptcha(image);
                answer.Text = "";
            };
            captcha.Children.Add(Text("Проверка после трёх неверных попыток"));
            captcha.Children.Add(image);
            captcha.Children.Add(answer);
            captcha.Children.Add(Button("Обновить капчу", () =>
            {
                Auth.GenerateCaptcha();
                redraw();
            }));
            p.Children.Add(captcha);
            redraw();
            Action enter = () =>
            {
                try
                {
                    Auth.Login(login.Text, password.Password, answer.Text);
                    ShowData(Catalog.All[0]);
                }
                catch (Exception ex)
                {
                    redraw();
                    Notify(ErrorText(ex));
                }
            };
            var buttons = new WrapPanel();
            buttons.Children.Add(Button("Войти", enter));
            buttons.Children.Add(Button("Регистрация", ShowRegistration));
            buttons.Children.Add(Button("Смена пароля", () => ShowPassword()));
            p.Children.Add(buttons);
            password.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    Run(enter);
            };
            p.Children.Add(Text("Учебные аккаунты: admin / CyberAdmin1 и user / CyberUser1", 12));
            Navigate("Авторизация", Scroll(p));
        }

        void DrawCaptcha(Canvas canvas)
        {
            canvas.Children.Clear();
            var rand = new Random();
            for (int i = 0; i < 10; i++)
                canvas.Children.Add(new Line { X1 = rand.Next(280), Y1 = rand.Next(64), X2 = rand.Next(280), Y2 = rand.Next(64), Stroke = Brushes.Silver, StrokeThickness = 1 });
            for (int i = 0; i < Auth.Captcha.Length; i++)
            {
                var t = Text(Auth.Captcha[i].ToString(), 29);
                t.Foreground = Brushes.DarkSlateBlue;
                t.FontWeight = FontWeights.Bold;
                t.RenderTransform = new RotateTransform(rand.Next(-15, 16));
                Canvas.SetLeft(t, 22 + i * 48);
                Canvas.SetTop(t, 10);
                canvas.Children.Add(t);
            }
        }

        public void ShowRegistration()
        {
            var p = Form("Регистрация пользователя");
            var login = Input("От 3 латинских символов", "", 40);
            var name = Input("Иванов Иван Иванович");
            var pw = Password("От 6 символов, латинская буква и цифра");
            var repeat = Password("Повторите пароль");
            Label(p, "Логин", login);
            Label(p, "ФИО", name);
            Label(p, "Пароль", pw);
            Label(p, "Повтор пароля", repeat);
            p.Children.Add(Text("Новая учётная запись получает роль «Пользователь». Роль администратора назначает администратор."));
            p.Children.Add(Button("Зарегистрироваться", () =>
            {
                Auth.Register(login.Text, name.Text, pw.Password, repeat.Password);
                Notify("Регистрация завершена. Теперь войдите в систему.");
                ShowLogin();
            }));
            Navigate("Регистрация", Scroll(p));
        }

        public void ShowPassword()
        {
            var p = Form("Смена пароля");
            var login = Input("Логин", Auth.Current == null ? "" : Auth.Current.Login, 40);
            var old = Password("Текущий пароль");
            var pw = Password("Новый пароль: латиница и цифры, от 6 символов");
            var repeat = Password("Повтор нового пароля");
            Label(p, "Логин", login);
            Label(p, "Текущий пароль", old);
            Label(p, "Новый пароль", pw);
            Label(p, "Повтор нового пароля", repeat);
            p.Children.Add(Button("Сохранить пароль", () =>
            {
                Auth.ChangePassword(login.Text, old.Password, pw.Password, repeat.Password);
                Notify("Пароль успешно изменён.");
                if (Auth.Current == null)
                    ShowLogin();
                else
                    ShowData(selected);
            }));
            Navigate("Смена пароля", Scroll(p));
        }
    }
}
