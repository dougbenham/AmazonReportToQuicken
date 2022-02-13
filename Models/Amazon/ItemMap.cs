using System.Globalization;
using CsvHelper.Configuration;

namespace AmazonReportToQuicken.Models.Amazon
{
    class ItemMap : ClassMap<ItemRecord>
    {
        /// <inheritdoc />
        public ItemMap()
        {
            AutoMap(CultureInfo.CurrentCulture);
            Map(i => i.OrderDate).Convert(q => q.Value.OrderDate.ToShortDateString());
            Map(i => i.ShipmentDate).Convert(q => q.Value.ShipmentDate.ToShortDateString());
        }
    }
}