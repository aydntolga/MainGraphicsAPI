using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralDatabaseServiceDemo.Infos
{
    public class FragmenatationTable
    {
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public string IndexName { get; set; }
        public decimal FragmentationRatio { get; set; }
    }
}
