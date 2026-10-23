using System;
using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Linq;
using CyberClub.Models;

namespace CyberClub
{
    public static class Passwords
    {
        public static void Validate(string password)
        {
            if (password == null || !Regex.IsMatch(password, @"\A(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d!@#$%^&*()_+\-=.?]{6,64}\z"))
                throw new Exception("Пароль: 6–64 символа, хотя бы одна латинская буква и цифра. Кириллица и пробелы запрещены.");
        }

        public static string Hash(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);
            using (var kdf = new Rfc2898DeriveBytes(password, salt, 120000, HashAlgorithmName.SHA256))
                return "120000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(kdf.GetBytes(32));
        }

        public static bool Verify(string password, string stored)
        {
            try
            {
                var p = stored.Split('$');
                int n = Int32.Parse(p[0]);
                if (n < 10000 || n > 1000000)
                    return false;
                using (var kdf = new Rfc2898DeriveBytes(password, Convert.FromBase64String(p[1]), n, HashAlgorithmName.SHA256))
                {
                    byte[] a = kdf.GetBytes(32), b = Convert.FromBase64String(p[2]);
                    if (a.Length != b.Length)
                        return false;
                    int d = 0;
                    for (int i = 0; i < a.Length; i++)
                        d |= a[i] ^ b[i];
                    return d == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
