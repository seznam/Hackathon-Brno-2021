using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeznamApi.Model
{
    public class NodeModel
    {
        public string id { get; set; }
        public string autor { get; set; }
        public string text { get; set; }
        public virtual ParentModel parent { get; set; }
        public int created { get; set; }
        public string repliesStartCursor { get; set; }
        public string replies { get; set; }
    }
}
