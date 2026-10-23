using System;
using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Linq;
using CyberClub.Models;

namespace CyberClub
{
    public class Account
    {
        public int Id;
        public string Login, FullName, Role;
        public bool IsAdmin
        {
            get
            {
                return Role == "Администратор";
            }
        }
    }
}
