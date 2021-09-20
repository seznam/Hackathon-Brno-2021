using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeznamApi.Model
{
    public class EdgesModel
    {
        public string cursor { get; set; }
        public virtual EdgesModel edges { get; set; }
    }
}
