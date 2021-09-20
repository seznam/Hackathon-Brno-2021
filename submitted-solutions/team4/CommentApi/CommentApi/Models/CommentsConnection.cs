using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CommentApi.Models
{
    /// <summary>
    ///     Records connection containing a single page of comment records.
    /// </summary>
    public class CommentsConnection
    {
        /// <summary>
        ///     Gets or Sets Edges
        /// </summary>
        [Required]
        [DataMember(Name = "edges")]
        public IEnumerable<CommentsEdge> Edges { get; set; }

        /// <summary>
        ///     Gets or Sets PageInfo
        /// </summary>
        [Required]
        [DataMember(Name = "pageInfo")]
        public PageInfo PageInfo { get; set; }
    }
}
