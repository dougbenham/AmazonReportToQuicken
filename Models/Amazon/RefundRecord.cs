using System;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace AmazonReportToQuicken.Models.Amazon
{
    class RefundRecord
    {
        [Name("Order ID")]
        public string OrderId { get; set; }

        [Name("Order Date")]
        public DateTime OrderDate { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        [Name("Refund Date")]
        public DateTime RefundDate { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Refund Amount")]
        public decimal ItemSubtotal { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Refund Tax Amount")]
        public decimal ItemSubtotalTax { get; set; }

        [Ignore]
        public decimal ItemTotal => ItemSubtotal + ItemSubtotalTax;

        [Ignore]
        public bool Used { get; set; }
        
        [Ignore]
        public Order Parent { get; set; }

        [Ignore]
        public ItemRecord Match { get; set; }

        public string GetMemo()
        {
            if (string.IsNullOrEmpty(Title))
                return OrderId;
            return Title.Trim();
        }
        
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Category))
                return $"[{OrderId}] {Title.Trim()}".Trim();
            return $"[{OrderId}] [{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Category.Replace("_", " ").ToLower())}] {Title.Trim()}".Trim();
        }
    }
}