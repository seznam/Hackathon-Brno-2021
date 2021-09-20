namespace CommentApi.Repositories;
public partial class CommentRepository
{
    public class CommentDetailDto
    {
        public Guid Id { get; set; }
        public string Author { get; set; } = null!;
        public long Created { get; set; }
        public string Text { get; set; } = null!;
        public Guid? ParentId { get; set; }
        public string Cursor { get; set; } = null!;
    }
}
