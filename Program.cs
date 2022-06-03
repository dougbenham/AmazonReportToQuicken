using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AmazonReportToQuicken.Models.Amazon;
using AmazonReportToQuicken.Models.Quicken;
using CsvHelper;
using CsvHelper.Configuration;
using NLog;

namespace AmazonReportToQuicken
{
    static class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private const string AmazonReportsPath = "P:\\Notes\\Documents\\Financial\\Amazon Orders\\";
        private const string QuickenPath = @"C:\work\transactions.qif";
        private const string QuickenResultsPath = "C:\\work\\transactions-new.qif";
        
        [STAThread]
        static async Task Main()
        {
            try
            {
                // items and refunds combined are keyed on order id & shipment date
                // TODO: need to add support for single item orders with promotional value and potentially multi-item orders where the promotion is theoretically applied to each item

                // orders have no unique key (even [order id, shipment date] can have duplicates)
                // sometimes there are even 2+ order records for a single item record (when purchasing quantity is 2+)
                var orderRecords = (await ParseAsync<OrderRecord>(AmazonReportsPath + "2021 - Orders.csv")).Concat(await ParseAsync<OrderRecord>(AmazonReportsPath + "2022 - Orders.csv")).ToList();
                var orders = orderRecords.Select(p => p.OrderId).Distinct().ToDictionary(i => i, i => new Order() { OrderId = i });
                var ordersByShipmentDate = orderRecords.GroupBy(p => p.ShipmentDate).ToDictionary(g => g.Key);
                var ordersByDay = orderRecords
                    .GroupBy(p => new OrderIdAndDateKey() { Date = p.ShipmentDate, OrderId = p.OrderId })
                    .ToDictionary(g => g.Key, g =>
                    {
                        var order = orders[g.Key.OrderId];
                        var children = g.ToList();
                        var r = new OrderForSingleDay() { Key = g.Key, Parent = order, Children = children };
                        order.Children.Add(r);
                        children.ForEach(c => c.Parent = r);
                        return r;
                    });

                var itemRecords = (await ParseAsync<ItemRecord>(AmazonReportsPath + "2021 - Items.csv")).Concat(await ParseAsync<ItemRecord>(AmazonReportsPath + "2022 - Items.csv")).ToList();
                var itemsByDay = itemRecords
                    .GroupBy(p => new OrderIdAndDateKey() { Date = p.ShipmentDate, OrderId = p.OrderId })
                    .ToDictionary(g => g.Key, g =>
                    {
                        var orderByDay = ordersByDay[g.Key];
                        var children = g.ToList();
                        var r = new ItemForSingleDay() { Key = g.Key, Parent = orderByDay, Children = children };
                        orderByDay.Items.AddRange(children);
                        children.ForEach(c => c.Parent = r);
                        return r;
                    });

                foreach (var o in ordersByDay.Values)
                {
                    foreach (var item in o.Items)
                    {
                        foreach (var order in o.Children)
                        {
                            if (item.ItemSubtotal == order.Subtotal)
                            {
                                item.Match = order;
                                order.Match = item;
                            }
                        }
                    }
                }

                var refundRecords = (await ParseAsync<RefundRecord>(AmazonReportsPath + "2021 - Refunds.csv")).Concat(await ParseAsync<RefundRecord>(AmazonReportsPath + "2022 - Refunds.csv")).ToList();
                var refundsByRefundDate = refundRecords.GroupBy(p => p.RefundDate).ToDictionary(g => g.Key);
                refundRecords.ForEach(r =>
                {
                    var order = orders[r.OrderId];
                    order.Refunds.Add(r);
                    r.Parent = order;

                    var found = false;
                    foreach (var orderForSingleDay in order.Children)
                    {
                        foreach (var item in orderForSingleDay.Items)
                        {
                            if (r.ItemSubtotal == item.ItemSubtotal)
                            {
                                r.Match = item;
                                found = true;
                                break;
                            }
                        }

                        if (found)
                            break;
                    }
                });
                /*var refundsCombined = refunds
                    .GroupBy(p => new OrderIdAndDateKey() { Date = p.RefundDate, OrderId = p.OrderId })
                    .ToDictionary(g => g.Key, g => new RefundCombined() { Key = g.Key, Children = g.ToList() });*/
                
                var qif = await ReadQifAsync(QuickenPath);
                var failedTransactions = new List<Transaction>();
                foreach (var transaction in qif.Transactions)
                {
                    if (transaction.Amount > 0)
                    {
                        // Refund
                        var found = false;
                        for (int days = 0; days < 7; days++)
                        {
                            var dateToTest = transaction.Date.AddDays(-1 * days);

                            if (refundsByRefundDate.TryGetValue(dateToTest, out var refundList))
                            {
                                foreach (var refund in refundList)
                                {
                                    if (!refund.Used && refund.ItemTotal == transaction.Amount)
                                    {
                                        refund.Used = true;
                                        if (string.IsNullOrEmpty(transaction.Memo))
                                            transaction.Memo = refund.GetMemo();
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                    break;
                            }

                            /*if (refundsCombined.TryGetValue(dateToTest, out var refundCombinedList))
                            {
                                foreach (var refundCombined in refundCombinedList)
                                {
                                    if (!refundCombined.Used && refundCombined.Total == transaction.Payment)
                                    {
                                        refundCombined.Used = true;
                                        if (string.IsNullOrEmpty(transaction.Memo))
                                            transaction.Memo = "[FOUND]";
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                    break;
                            }*/
                        }
                    }
                    else if (transaction.Amount < 0)
                    {
                        var transactionCharge = -transaction.Amount;

                        // Purchase
                        var found = false;
                        for (int days = 0; days < 7; days++)
                        {
                            var dateToTest = transaction.Date.AddDays(-1 * days);

                            if (ordersByShipmentDate.TryGetValue(dateToTest, out var orderList))
                            {
                                foreach (var order in orderList)
                                {
                                    if (!order.Used && order.TotalCharged == transactionCharge)
                                    {
                                        order.Used = true;
                                        if (order.Match != null)
                                            order.Match.Used = true;
                                        if (string.IsNullOrEmpty(transaction.Memo))
                                            transaction.Memo = order.GetMemo();
                                        found = true;
                                        break;
                                    }
                                    
                                    var unusedChildren = order.Parent.Children.Where(r => !r.Used).ToArray();
                                    if (unusedChildren.Sum(r => r.TotalCharged) == transactionCharge)
                                    {
                                        foreach (var child in unusedChildren)
                                        {
                                            child.Used = true;
                                            if (child.Match != null)
                                                child.Match.Used = true;
                                        }

                                        if (string.IsNullOrEmpty(transaction.Memo))
                                        {
                                            transaction.Memo = order.OrderId;
                                            transaction.ClearSplits();
                                            foreach (var child in unusedChildren)
                                            {
                                                transaction.AddSplit(-child.TotalCharged, child.GetMemo());
                                            }
                                        }

                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                    break;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(transaction.Memo))
                    {
                        failedTransactions.Add(transaction);
                    }
                }

                foreach (var transaction in failedTransactions)
                {
                    if (transaction.Amount > 0)
                    {
                        // Refund
                        /*var found = false;
                        for (int days = 0; days < 4; days++)
                        {
                            var dateToTest = transaction.Date.AddDays(-1 * days);

                            if (refundsByRefundDate.TryGetValue(dateToTest, out var refundList))
                            {
                                foreach (var refund in refundList)
                                {
                                    if (!refund.Used && refund.ItemTotal == transaction.Payment)
                                    {
                                        refund.Used = true;
                                        if (string.IsNullOrEmpty(transaction.Memo))
                                            transaction.Memo = "[FOUND]";
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                    break;
                            }
                        }*/
                        ;
                    }
                    else if (transaction.Amount < 0)
                    {
                        var transactionCharge = -transaction.Amount;

                        // Purchase
                        var found = false;
                        for (int days = 0; days < 7; days++)
                        {
                            var dateToTest = transaction.Date.AddDays(-1 * days);

                            if (ordersByShipmentDate.TryGetValue(dateToTest, out var orderList))
                            {
                                foreach (var order in orderList)
                                {
                                    var orderForSingleDay = order.Parent;
                                    var sum = orderForSingleDay.Children.Where(r => !r.Used).Sum(r => r.TotalCharged);
                                    if (sum > transactionCharge)
                                    {
                                        if (string.IsNullOrEmpty(transaction.Memo))
                                            transaction.Memo = orderForSingleDay.Parent.OrderId;
                                        found = true;
                                        break;
                                    }
                                }

                                if (found)
                                    break;
                            }
                        }
                    }

                    /*if (string.IsNullOrEmpty(transaction.Memo))
                        transaction.Memo = "[[[[CAN'T FIND]]]]";*/
                }
                
                await WriteQifAsync(QuickenResultsPath, qif);
                await WriteAsync<OrderRecord, OrderMap>(AmazonReportsPath + "2022 - Orders - Leftover.csv", orderRecords.Where(i => !i.Used));
                await WriteAsync<ItemRecord, ItemMap>(AmazonReportsPath + "2022 - Items - Leftover.csv", itemRecords.Where(i => !i.Used));
                await WriteAsync<RefundRecord>(AmazonReportsPath + "2022 - Refunds - Leftover.csv", refundRecords.Where(i => !i.Used));
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static async Task<Qif> ReadQifAsync(string path)
        {
            var lines = await File.ReadAllLinesAsync(path);
            var qif = new Qif() { Header = lines[0] };
            var currentTransaction = new Transaction();
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == "^")
                {
                    qif.Transactions.Add(currentTransaction);
                    currentTransaction = new();
                }
                else
                    currentTransaction.Lines.Add(line);
            }

            return qif;
        }

        private static async Task WriteQifAsync(string path, Qif qif)
        {
            var lines = new List<string>();
            lines.Add(qif.Header);
            foreach (var transaction in qif.Transactions)
            {
                foreach (var line in transaction.Lines)
                    lines.Add(line);
                lines.Add("^");
            }

            await File.WriteAllLinesAsync(path, lines);
        }

        private static async Task<List<T>> ParseAsync<T>(string path)
        {
            var results = new List<T>();
            var config = new CsvConfiguration(CultureInfo.CurrentCulture)
            {
                ReadingExceptionOccurred = args =>
                {
                    _logger.Trace(args.Exception);
                    return false;
                }
            };
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            using var csv = new CsvReader(sr, config);
            await foreach (var record in csv.GetRecordsAsync<T>())
            {
                results.Add(record);
            }

            return results;
        }

        private static async Task WriteAsync<T>(string path, IEnumerable<T> records)
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs);
            using var csv = new CsvWriter(sw, config);
            await csv.WriteRecordsAsync(records);
        }

        private static async Task WriteAsync<T, TMap>(string path, IEnumerable<T> records) where TMap : ClassMap<T>
        {
            var config = new CsvConfiguration(CultureInfo.CurrentCulture);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            using var sw = new StreamWriter(fs);
            using var csv = new CsvWriter(sw, config);
            csv.Context.RegisterClassMap<TMap>();
            await csv.WriteRecordsAsync(records);
        }
    }
}
