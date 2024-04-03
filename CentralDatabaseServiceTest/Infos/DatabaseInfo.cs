using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralDatabaseServiceTest.Infos
{
    public class DatabaseInfo
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public List<TableInfo> Tables { get; set; }
        public string TotalTableSizeColumn { get; set; }
        public string TotalIndexSizeColumn { get; set; }
    }
}
