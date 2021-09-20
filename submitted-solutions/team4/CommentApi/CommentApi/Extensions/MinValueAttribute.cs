
using System.ComponentModel.DataAnnotations;

namespace CommentApi.Extensions
{
    public class MinValueAttribute : RangeAttribute
    {
        public MinValueAttribute(int minValue) : base(minValue, int.MaxValue)
        { 
        
        }
    }
}
