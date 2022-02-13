using System.Collections.Generic;
using System.Linq;

namespace AmazonReportToQuicken.Models.Amazon
{
    class OrderForSingleDay
    {
        public OrderIdAndDateKey Key { get; set; }

        public Order Parent { get; set; }

        public List<OrderRecord> Children { get; set; }

        public List<ItemRecord> Items { get; set; } = new();
        
        public decimal TotalFromChildren => Children.Sum(r => r.TotalCharged);
        
        //public decimal TotalFromItems => Items.Sum(r => r.ItemSubtotal); // doesn't include shipping costs, promotional coupons - and the tax might be different if shipping costs > 0
        
        public bool Used
        {
            get => Children.Any(r => r.Used);
            set => Children.ForEach(r => r.Used = value);
        }
    }
}