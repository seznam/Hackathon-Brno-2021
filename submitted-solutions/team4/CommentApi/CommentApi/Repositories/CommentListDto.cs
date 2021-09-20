namespace CommentApi.Repositories;

public class CommentListDto
{
    public Guid Id { get; set; }
    public string Author { get; set; } = null!;
    public long Created { get; set; }
    public string Text { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public string Cursor { get; set; } = null!;

    public int Level { get; set; }
    public int OrderInDirectParent { get; set; }
    public bool IsLastInDirectParent { get; set; }
}
