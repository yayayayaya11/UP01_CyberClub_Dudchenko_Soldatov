using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberClub
{
    public class Field
    {
        public string Key, Title, Kind, Source;
        public int Max;
        public Field(string k, string t, string kind = "text", string source = "", int max = 100)
        {
            Key = k;
            Title = t;
            Kind = kind;
            Source = source;
            Max = max;
        }
    }
}
