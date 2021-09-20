using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeznamApi.Model
{
    public class PageInfoModel
    {
        public bool hasNextPage { get; set; }
        public string endCursor { get; set; }
    }
}
