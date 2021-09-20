using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CommentApi.Models
{
    /// <summary>
    ///     Input for read operation for traversing the comments tree. Used by the batch operation.
    /// </summary>
    public class GetCommentsInput
    {
        /// <summary>
        ///     Gets or Sets Location
        /// </summary>
        [Required]
        [DataMember(Name = "location")]
        public string Location { get; set; }

        /// <summary>
        ///     Gets or Sets Limit
        /// </summary>
        [Required]
        [DataMember(Name = "limit")]
        public decimal? Limit { get; set; }

        /// <summary>
        ///     Gets or Sets After
        /// </summary>

        [DataMember(Name = "after")]
        public string? After { get; set; }

        /// <summary>
        ///     Gets or Sets Replies1stLevelLimit
        /// </summary>

        [DataMember(Name = "replies1stLevelLimit")]
        public decimal? Replies1stLevelLimit { get; set; }

        /// <summary>
        ///     Gets or Sets Replies2ndLevelLimit
        /// </summary>

        [DataMember(Name = "replies2ndLevelLimit")]
        public decimal? Replies2ndLevelLimit { get; set; }

        /// <summary>
        ///     Gets or Sets Replies3rdLevelLimit
        /// </summary>

        [DataMember(Name = "replies3rdLevelLimit")]
        public decimal? Replies3rdLevelLimit { get; set; }
    }
}
