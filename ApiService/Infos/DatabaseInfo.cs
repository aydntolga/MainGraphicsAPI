using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiService.Infos
{
    public class DatabaseInfo
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public List<TableInfo> Tables { get; set; }
    }
}
