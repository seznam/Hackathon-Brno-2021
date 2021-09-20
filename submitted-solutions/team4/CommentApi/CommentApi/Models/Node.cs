using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CommentApi.Models
{
    [DataContract]
    public class Node
    {
        [Required]
        [DataMember(Name = "id")]
        public Guid Id { get; set; }
    }
}