using System;
using System.Collections.Generic;

namespace AmazonReportToQuicken.Models.Quicken
{
    class Transaction
    {
        private const string CurrencyFormat = "#,##0.00";

        public List<string> Lines { get; } = new();

        public DateTime Date
        {
            get => ConvertQifDateToDateTime(GetLine("D"));
            set => SetLine("D", ConvertDateTimeToQifDate(value));
        }
        
        public string Memo
        {
            get => GetLine("M");
            set => SetLine("M", value.Length > 65 ? value.Substring(0, 65) : value);
        }

        public decimal Amount
        {
            get => decimal.Parse(GetLine("U"));
            set
            {
                var s = value.ToString(CurrencyFormat);
                SetLine("U", s);
                SetLine("T", s);
            }
        }

        public bool IsSplit()
        {
            return GetLine("$") != null;
        }

        public void ClearSplits()
        {
            Lines.RemoveAll(s => s.Length > 0 && (s[0] == 'L' || s[0] == 'S' || s[0] == 'E' || s[0] == '$'));
        }

        public void AddSplit(decimal amount, string memo = null, string category = null)
        {
            Lines.Add("S" + category);
            if (!string.IsNullOrEmpty(memo))
                Lines.Add("E" + memo);
            Lines.Add("$" + amount.ToString(CurrencyFormat));
        }

        public string GetLine(string id)
        {
            var line = Lines.Find(l => l.StartsWith(id));
            return line?.Substring(id.Length);
        }

        public void SetLine(string id, string value)
        {
            var newLine = id + value;

            var idx = Lines.FindIndex(l => l.StartsWith(id));
            if (idx >= 0)
                Lines[idx] = newLine;
            else
                Lines.Add(newLine);
        }

        private static DateTime ConvertQifDateToDateTime(string qifDate)
        {
            var split = qifDate.Split('/', '\'');
            return new DateTime(2000 + int.Parse(split[2]), int.Parse(split[0]), int.Parse(split[1]));
        }

        private static string ConvertDateTimeToQifDate(DateTime dateTime)
        {
            var year = (dateTime.Year - 2000).ToString();
            var month = dateTime.Month.ToString().PadLeft(2, ' ');
            var day = dateTime.Day.ToString().PadLeft(2, ' ');
            return month + '/' + day + '\'' + year;
        }
    }
}