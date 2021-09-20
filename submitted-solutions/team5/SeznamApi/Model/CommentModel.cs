using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeznamApi.Model
{
    public class CommentModel
    {
        public virtual PageInfoModel PageInfo { get; set; }
        public virtual EdgesModel edges { get; set; }

    }
}
