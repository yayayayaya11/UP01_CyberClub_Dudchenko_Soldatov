using System;
using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Linq;
using CyberClub.Models;

namespace CyberClub
{
    public class AuthService
    {
        public int FailedAttempts { get; private set; }
        public string Captcha { get; private set; }

        public bool NeedsCaptcha
        {
            get
            {
                return FailedAttempts >= 3;
            }
        }

        public Account Current { get; private set; }

        public AuthService()
        {
            GenerateCaptcha();
        }

        public void GenerateCaptcha()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var b = new byte[5];
            using (var r = RandomNumberGenerator.Create())
                r.GetBytes(b);
            Captcha = "";
            foreach (byte x in b)
                Captcha += alphabet[x % alphabet.Length];
        }

        public bool CheckCaptcha(string answer)
        {
            return String.Equals((answer ?? "").Trim(), Captcha, StringComparison.OrdinalIgnoreCase);
        }

        public Account Login(string login, string password, string captcha)
        {
            if (String.IsNullOrWhiteSpace(login) || String.IsNullOrEmpty(password))
                throw new Exception("Введите логин и пароль.");
            if (NeedsCaptcha && !CheckCaptcha(captcha))
            {
                GenerateCaptcha();
                throw new Exception("Неверная капча. Введите символы с нового изображения.");
            }

            {
                var context = Core.Context;
                string normalized = login.Trim();
                var user = context.Users.AsNoTracking().SingleOrDefault(u => u.Login == normalized);
                if (user == null || !Passwords.Verify(password, user.PasswordHash))
                {
                    FailedAttempts++;
                    GenerateCaptcha();
                    throw new Exception("Неверный логин или пароль." + (NeedsCaptcha ? " Требуется капча." : ""));
                }

                Current = new Account
                {
                    Id = user.Id,
                    Login = user.Login,
                    FullName = user.FullName,
                    Role = user.Role
                };
                FailedAttempts = 0;
                GenerateCaptcha();
                return Current;
            }
        }

        public void Logout()
        {
            Current = null;
        }

        public static void ValidateIdentity(string login, string name)
        {
            if (!Regex.IsMatch(login ?? "", @"\A[A-Za-z][A-Za-z0-9_.]{2,39}\z"))
                throw new Exception("Логин: 3–40 латинских букв, цифр, точек или подчёркиваний; начало — буква.");
            if (!Regex.IsMatch(name ?? "", @"\A[\p{L} .'-]{3,100}\z"))
                throw new Exception("ФИО: от 3 до 100 символов, только буквы, пробелы, точки, дефис и апостроф.");
        }

        public void Register(string login, string name, string password, string confirmation)
        {
            ValidateIdentity(login, name);
            Passwords.Validate(password);
            if (password != confirmation)
                throw new Exception("Пароли не совпадают.");
            {
                var context = Core.Context;
                context.Users.Add(new User { Login = login.Trim(), FullName = name.Trim(), PasswordHash = Passwords.Hash(password), Role = "Пользователь" });
                Core.SaveChanges();
            }
        }

        public void ChangePassword(string login, string oldPassword, string newPassword, string confirmation)
        {
            if (String.IsNullOrWhiteSpace(login) || String.IsNullOrEmpty(oldPassword))
                throw new Exception("Введите логин и текущий пароль.");
            Passwords.Validate(newPassword);
            if (newPassword != confirmation)
                throw new Exception("Пароли не совпадают.");
            {
                var context = Core.Context;
                string normalized = login.Trim();
                var user = context.Users.SingleOrDefault(u => u.Login == normalized);
                if (user == null || !Passwords.Verify(oldPassword, user.PasswordHash))
                    throw new Exception("Неверный логин или текущий пароль.");
                if (oldPassword == newPassword)
                    throw new Exception("Новый пароль должен отличаться от текущего.");
                user.PasswordHash = Passwords.Hash(newPassword);
                Core.SaveChanges();
            }
        }
    }
}
