using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace tinderbot.tablemodels
{
    public class Matches : TableEntity
    {
        public bool DateOpportunity { get; set; }
    }

    public class MatchesHistory : TableEntity
    {
        public string Message{ get; set; }
        public string Intent { get; set; }
        public double Score { get; set; }
    }
}