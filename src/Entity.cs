using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberClub
{
    public class Entity
    {
        public string Table, Title, Query;
        public Field[] Fields;
        public Entity(string table, string title, string query, params Field[] fields)
        {
            Table = table;
            Title = title;
            Query = query;
            Fields = fields;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
