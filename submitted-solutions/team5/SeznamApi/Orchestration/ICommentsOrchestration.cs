using SeznamApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SeznamApi.Orchestration
{
    public interface ICommentsOrchestration 
    {
        public CommentModel GetCommentByUrl(string URL);
    }
}
