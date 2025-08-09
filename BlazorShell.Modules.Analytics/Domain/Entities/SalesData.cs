using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorShell.Modules.Analytics.Domain.Entities
{
    public class SalesData
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string ProductCategory { get; set; }
        public string Region { get; set; }
        public string SalesRepresentative { get; set; }
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        [NotMapped]
        public decimal Profit => Revenue - Cost;
        public string Channel { get; set; } // Online, Retail, Wholesale
        public string CustomerSegment { get; set; } // Enterprise, SMB, Consumer
    }
}
