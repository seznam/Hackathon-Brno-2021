using System;
using System.Collections.Generic;
using CommentApi.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CommentApi.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class BatchInputItem
    {
        [DataMember(Name = "id", IsRequired = false)]
        public Guid? Id { get; set; }
        [DataMember(Name = "location", IsRequired = false)]
        public string? Location { get; set; }
        [DataMember(Name = "limit", IsRequired = false)]
        public int? Limit { get; set; }
        [DataMember(Name = "after", IsRequired = false)]
        public string? After { get; set; }
        [DataMember(Name = "replies1stLevelLimit", IsRequired = false)]
        public int? Replies1stLevelLimit { get; set; }
        [DataMember(Name = "replies2ndLevelLimit", IsRequired = false)]
        public int? Replies2ndLevelLimit { get; set; }
        [DataMember(Name = "replies3rdLevelLimit", IsRequired = false)]
        public int? Replies3rdLevelLimit { get; set; }

        public bool Validate()
        {
            var isSingleCommentRequest = Id is not null;

            var isSearchCommentsRequest =
                Limit is not null
                || Location is not null
                || Replies1stLevelLimit is not null
                || Replies2ndLevelLimit is not null
                || Replies3rdLevelLimit is not null;

            if (isSingleCommentRequest && isSearchCommentsRequest)
            {
                return false;
            }

            if (Limit < 1 
                || Replies1stLevelLimit < 0 
                || Replies2ndLevelLimit < 0 
                || Replies3rdLevelLimit < 0)
            {
                return false;
            }


            return true;
        }
    }
}
