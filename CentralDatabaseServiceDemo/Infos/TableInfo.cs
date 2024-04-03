﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CentralDatabaseServiceDemo.Infos
{
    public class TableInfo
    {
        public string Name { get; set; }
        public string MostUsedIndexColumn { get; set; }
        public string TotalTableSizeColumn { get; set; }
        public string TotalIndexSizeColumn { get; set; }
    }
}
