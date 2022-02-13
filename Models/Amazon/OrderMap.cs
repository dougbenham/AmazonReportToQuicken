using System.Globalization;
using CsvHelper.Configuration;

namespace AmazonReportToQuicken.Models.Amazon
{
    class OrderMap : ClassMap<OrderRecord>
    {
        /// <inheritdoc />
        public OrderMap()
        {
            AutoMap(CultureInfo.CurrentCulture);
            Map(i => i.OrderDate).Convert(q => q.Value.OrderDate.ToShortDateString());
            Map(i => i.ShipmentDate).Convert(q => q.Value.ShipmentDate.ToShortDateString());
        }
    }
}