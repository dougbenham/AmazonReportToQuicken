using System.Collections.Generic;

namespace AmazonReportToQuicken.Models.Amazon
{
    class Order
    {
        public string OrderId { get; set; }

        public List<OrderForSingleDay> Children { get; set; } = new();

        public List<RefundRecord> Refunds { get; set; } = new();
    }
}