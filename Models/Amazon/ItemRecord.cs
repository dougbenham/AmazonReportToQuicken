using System;
using System.Globalization;
using CsvHelper.Configuration.Attributes;

namespace AmazonReportToQuicken.Models.Amazon
{
    class ItemRecord
    {
        [Name("Order Date")]
        public DateTime OrderDate { get; set; }

        [Name("Order ID")]
        public string OrderId { get; set; }

        public string Title { get; set; }

        public string Category { get; set; }

        [Name("Shipment Date")]
        public DateTime ShipmentDate { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Item Subtotal")]
        public decimal ItemSubtotal { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Item Subtotal Tax")]
        public decimal ItemSubtotalTax { get; set; }

        [NumberStyles(NumberStyles.Currency)]
        [Name("Item Total")]
        public decimal ItemTotal { get; set; }

        [Ignore]
        public bool Used { get; set; }

        [Ignore]
        public ItemForSingleDay Parent { get; set; }

        [Ignore]
        public OrderRecord Match { get; set; }
    }
}