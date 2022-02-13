using System.Collections.Generic;
using System.Linq;

namespace AmazonReportToQuicken.Models.Amazon
{
    class ItemForSingleDay
    {
        public OrderIdAndDateKey Key { get; set; }

        public OrderForSingleDay Parent { get; set; }

        public List<ItemRecord> Children { get; set; }
        
        public decimal Total => Children.Sum(r => r.ItemTotal);
        
        public bool Used
        {
            get => Children.Any(r => r.Used);
            set => Children.ForEach(r => r.Used = value);
        }
    }
}