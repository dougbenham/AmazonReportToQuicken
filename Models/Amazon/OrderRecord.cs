using System;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace AmazonReportToQuicken.Models.Amazon
{
    class OrderRecord
    {
        [Name("Order Date")]
        public DateTime OrderDate { get; set; }

        [Name("Order ID")]
        public string OrderId { get; set; }

        [Name("Payment Instrument Type")]
        public string PaymentInstrumentType { get; set; }

        [Name("Shipment Date")]
        public DateTime ShipmentDate { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        public decimal Subtotal { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Shipping Charge")]
        public decimal ShippingCharge { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Tax Before Promotions")]
        public decimal TaxBeforePromotions { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Total Promotions")]
        public decimal TotalPromotions { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Tax Charged")]
        public decimal TaxCharged { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Total Charged")]
        public decimal TotalCharged { get; set; }

        [Ignore]
        public bool Used { get; set; }

        [Ignore]
        public OrderForSingleDay Parent { get; set; }

        [Ignore]
        public ItemRecord Match { get; set; }

        public string GetMemo()
        {
            if (!string.IsNullOrEmpty(Match?.Title))
                return Match.Title.Trim();

            return OrderId;
        }

        public override string ToString()
        {
            if (Match != null)
            {
                if (string.IsNullOrEmpty(Match.Category))
                    return $"[{OrderId}] {Match.Title.Trim()}".Trim();
                return $"[{OrderId}] [{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Match.Category.Replace("_", " ").ToLower())}] {Match.Title.Trim()}".Trim();
            }

            return $"[{OrderId}]";
        }
    }
}