using System;

namespace AmazonReportToQuicken.Models.Amazon
{
    public struct OrderIdAndDateKey
    {
        public string OrderId { get; set; }

        public DateTime Date { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return OrderId + " (" + Date.ToShortDateString() + ")";
        }
    }
}