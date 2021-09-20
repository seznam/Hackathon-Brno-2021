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
    /// A
    /// </summary>
    public class CommentInput : IValidatableObject
    { 
        /// <summary>
        /// Gets or Sets Parent
        /// </summary>

        [DataMember(Name="parent")]
        public Guid? Parent { get; set; }

        /// <summary>
        /// Gets or Sets Author
        /// </summary>
        [Required]
        [StringLength(36, MinimumLength=1)]
        [DataMember(Name="author")]
        public string Author { get; set; }

        /// <summary>
        /// Comment&#x27;s actual content. The content must be at least 5 bytes long, may contain any valid Unicode characters and can be up to 128 KiB long when using UTF-8 encoding.
        /// </summary>
        /// <value>Comment&#x27;s actual content. The content must be at least 5 bytes long, may contain any valid Unicode characters and can be up to 128 KiB long when using UTF-8 encoding.</value>
        [Required]
        [DataMember(Name="text")]
        public string Text { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var textByteCount = Encoding.UTF8.GetByteCount(Text);

            if (textByteCount is < 5 or > 131_072)
            {
                yield return new ValidationResult("Size of text property must be between 5 and 131072 bytes");
            }
        }
    }
}
