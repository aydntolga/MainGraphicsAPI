using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralDatabaseServiceDemo.Infos
{
    public class TotalTableSize
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public string TableName { get; set; }
        public decimal TotalTableSizeGB { get; set; }
        public decimal TotalIndexSize { get; set; }
        
    }
}
