using System;
using CommentApi.NaiveImplementation;

namespace CommentApi.NaiveImplementation;

public class NewComment
{
    public Guid Id { get; set; }
    public string Author { get; set; } = null!;
    public string Text { get; set; } = null!;
    public long Created { get; set; }
    public Guid? ParentId { get; set; }
    public string Cursor { get; set; } = null!;
    public string LocationHash { get; set; } = null!;
    public int Level { get; set; }
    public int? Parent0 { get; set; }
    public int? Parent1 { get; set; }
    public int? Parent2 { get; set; }
    public int OrderInDirectParent { get; set; }
    public bool IsLastInDirectParent { get; set; }
}