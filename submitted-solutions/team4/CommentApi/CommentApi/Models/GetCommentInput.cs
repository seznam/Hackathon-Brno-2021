using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CommentApi.Models
{
    /// <summary>
    ///     Input for read operation for getting a single comment by its ID. Used by the batch operation.
    /// </summary>
    public class GetCommentInput
    {
        /// <summary>
        ///     Gets or Sets Id
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength = 1)]
        [DataMember(Name = "id")]
        public string Id { get; set; }
    }
}
