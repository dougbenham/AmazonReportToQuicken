using System.Collections.Generic;

namespace AmazonReportToQuicken.Models.Quicken
{
    class Qif
    {
        public string Header { get; set; }

        public List<Transaction> Transactions { get; } = new();
    }
}