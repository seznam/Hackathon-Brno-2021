using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CommentApi.Models
{
    /// <summary>
    ///     Edge of a comment records connection referencing a single comment.
    /// </summary>
    public class CommentsEdge
    {
        /// <summary>
        ///     Gets or Sets Node
        /// </summary>
        [Required]
        [DataMember(Name = "node")]
        public CommentDto Node { get; set; }

        [Required]
        [DataMember(Name = "cursor")]
        public string Cursor { get; set; }
    }
}
