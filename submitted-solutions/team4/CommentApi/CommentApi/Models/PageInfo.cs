using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace CommentApi.Models
{
    /// <summary>
    ///     Pagination-related information.
    /// </summary>
    public class PageInfo
    {
        /// <summary>
        ///     True iff there is another page of records available at the moment the information was retrieved. The endCursor must
        ///     be non-null if this flag is true.
        /// </summary>
        /// <value>
        ///     True iff there is another page of records available at the moment the information was retrieved. The endCursor
        ///     must be non-null if this flag is true.
        /// </value>
        [Required]
        public bool HasNextPage { get; set; }

        /// <summary>
        ///     Gets or Sets EndCursor
        /// </summary>
        public string? EndCursor { get; set; }
    }
}
