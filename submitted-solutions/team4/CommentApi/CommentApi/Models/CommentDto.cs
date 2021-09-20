using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CommentApi.Models
{
    /// <summary>
    /// Record representing a persisted (or soon-to-be persisted) comment.
    /// </summary>
    public class CommentDto
    {
        /// <summary>
        /// Gets or Sets Author
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength = 1)]
        [DataMember(Name = "author")]
        public string Author { get; set; }

        /// <summary>
        /// Comment&#x27;s actual content. The content must be at least 5 bytes long, may contain any valid Unicode characters and can be up to 128 KiB long when using UTF-8 encoding.
        /// </summary>
        /// <value>Comment&#x27;s actual content. The content must be at least 5 bytes long, may contain any valid Unicode characters and can be up to 128 KiB long when using UTF-8 encoding.</value>
        [Required]
        [DataMember(Name = "text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or Sets Parent
        /// </summary>

        [DataMember(Name = "parent")]
        public Node? Parent { get; set; }

        /// <summary>
        /// Gets or Sets Created
        /// </summary>
        [Required]
        [DataMember(Name = "created")]
        public long Created { get; set; }

        /// <summary>
        /// Gets or Sets RepliesStartCursor
        /// </summary>
        [Required]
        [DataMember(Name = "repliesStartCursor")]
        public string? RepliesStartCursor { get; set; }

        /// <summary>
        /// Gets or Sets Replies
        /// </summary>

        [DataMember(Name = "replies")]
        public CommentsConnection Replies { get; set; }


        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [Required]
        [DataMember(Name="id")]
        public Guid Id { get; set; }
    }
}
