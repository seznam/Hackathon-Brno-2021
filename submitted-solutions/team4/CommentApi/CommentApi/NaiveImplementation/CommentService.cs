using CommentApi.Helpers;
using CommentApi.Models;
using CommentApi.Repositories;
using CommentApi.Utils;

namespace CommentApi.NaiveImplementation;
public class CommentService
{
    private readonly CommentRepository _commentRepository;

    public CommentService(CommentRepository commentRepository)
    {
        _commentRepository = commentRepository;
    }

    public async Task<CommentDto?> GetByIdAsync(Guid id)
    {
        var comment = await _commentRepository.FindAsync(id);
        if (comment is null)
        {
            return null;
        }

        return new CommentDto
        {
            Author = comment.Author,
            Created = comment.Created,
            Id = comment.Id,
            Text = comment.Text,
            Parent = comment.ParentId is not null ? new Node { Id = comment.ParentId.Value } : null,
            RepliesStartCursor = CursorUtilities.CreateRepliesCursor(comment.Cursor),
        };
    }

    public async Task<CommentDto?> CreateNewCommentAsync(
        Guid? parentId,
        string author,
        string text,
        string location)
    {
        var level = 1;
        int? parent0 = null;
        int? parent1 = null;
        int? parent2 = null;
        int orderInDirectParent;
        string cursor;
        var locationHash = Sha256Helper.GenerateHash(location);

        if (parentId is not null)
        {
            var parentComment = await _commentRepository.FindAsync(parentId.Value, locationHash);
            if (parentComment is null)
            {
                return null;
            }
            parent0 = parentComment.OrderInDirectParent;
            parent1 = parentComment.Parent0;
            parent2 = parentComment.Parent1;
            var lastInParentOrder = await _commentRepository.FindLastInParentAsync(parentComment.Id);

            orderInDirectParent = (lastInParentOrder ?? 0) + 1;
            cursor = $"{parentComment.Cursor}/{orderInDirectParent}";
            level = parentComment.Level + 1;
        }
        else
        {
            var lastInDirectParentOrder = await _commentRepository.FindLastInDirectParentAsync(locationHash);
            orderInDirectParent = (lastInDirectParentOrder ?? 0) + 1;
            cursor = $"{orderInDirectParent}";
        }

        var newComment = new NewComment
        {
            Text = text,
            Author = author,
            ParentId = parentId,
            Level = level,
            OrderInDirectParent = orderInDirectParent,
            Parent0 = parent0,
            Parent1 = parent1,
            Parent2 = parent2,
            Cursor = cursor,
            LocationHash = locationHash,
            Created = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IsLastInDirectParent = true,
            Id = Guid.NewGuid(),
        };

        await _commentRepository.Create(newComment);

        return new CommentDto
        {
            Id = newComment.Id,
            Author = newComment.Author,
            Created = newComment.Created,
            Parent = parentId is not null ? new Node { Id = parentId.Value } : null,
            Text = text,
            RepliesStartCursor = CursorUtilities.CreateRepliesCursor(newComment.Cursor),
        };
    }

    public Task DeleteAllAsync()
    {
        return _commentRepository.DeleteAsync();
    }

    public async Task<CommentsConnection> SearchAsync(
        string location,
        int limit,
        string? after,
        int firstLevelLimit,
        int secondLevelLimit,
        int thirdLevelLimit)
    {
        var level = 1;
        var topLevelOffset = 0;
        var cursorPrefix = string.Empty;
        var locationHash = Sha256Helper.GenerateHash(location);
        if (!string.IsNullOrEmpty(after))
        {
            (level, cursorPrefix, topLevelOffset) = CursorUtilities.ParseCursor(after);
        }

        var returnLevels = (firstLevelLimit, secondLevelLimit, thirdLevelLimit) switch
        {
            ( > 0, > 0, > 0) => 3,
            ( > 0, > 0, _) => 2,
            ( > 0, _, _) => 1,
            _ => 0,
        };


        var dbComments = await _commentRepository.Get(locationHash, cursorPrefix, level, level + returnLevels);

        DomainMetrics.ReturnedDatabaseComments.Observe(dbComments.Count());
        DomainMetrics.RequestedCommentsLevel.Observe(level);
        DomainMetrics.RequestedMaxComments.Observe(
            limit + firstLevelLimit * limit + (firstLevelLimit * limit * secondLevelLimit) + (firstLevelLimit * limit * secondLevelLimit * thirdLevelLimit));

        var comments = dbComments
            .Where(x => x.Level != level || (x.OrderInDirectParent <= limit + topLevelOffset && x.OrderInDirectParent > topLevelOffset));

        if (firstLevelLimit > 0)
        {
            comments = comments.Where(x => x.Level != level + 1 || x.OrderInDirectParent <= firstLevelLimit);
        }

        if (secondLevelLimit > 0)
        {
            comments = comments.Where(x => x.Level != level + 2 || x.OrderInDirectParent <= secondLevelLimit);
        }

        if (thirdLevelLimit > 0)
        {
            comments = comments.Where(x => x.Level != level + 3 || x.OrderInDirectParent <= thirdLevelLimit);
        }

        var orderedComments = comments
            .Where(c => c.Level == level)
            .OrderBy(c => c.OrderInDirectParent);

        var lastCommentInLevel = orderedComments.LastOrDefault();
        return new CommentsConnection
        {
            Edges = orderedComments
                .Select(c => new CommentsEdge
                {
                    Cursor = c.Cursor,
                    Node = new CommentDto
                    {
                        Author = c.Author,
                        Created = c.Created,
                        Id = c.Id,
                        Parent = c.ParentId is not null ? new Node { Id = c.ParentId.Value } : null,
                        Text = c.Text,
                        Replies = MapRepliesToCommentsConnection(comments, c)
                    }
                }),
            PageInfo = new PageInfo
            {
                EndCursor = lastCommentInLevel?.Cursor,
                HasNextPage = lastCommentInLevel?.IsLastInDirectParent == false
            }
        };
    }

    private static CommentsConnection MapRepliesToCommentsConnection(IEnumerable<CommentListDto> comments, CommentListDto parent)
    {
        var orderedCommentsFromParent = comments
            .Where(x => x.ParentId == parent.Id)
            .OrderBy(x => x.OrderInDirectParent);

        var lastCommentFromParent = orderedCommentsFromParent.LastOrDefault();
        return new CommentsConnection
        {
            Edges = orderedCommentsFromParent
                .Select(x => new CommentsEdge
                {
                    Cursor = x.Cursor,
                    Node = new CommentDto
                    {
                        Author = x.Author,
                        Created = x.Created,
                        Id = x.Id,
                        Parent = x.ParentId is not null ? new Node { Id = x.ParentId.Value } : null,
                        Text = x.Text,
                        Replies = MapRepliesToCommentsConnection(comments, x)
                    }
                }),
            PageInfo = new PageInfo
            {
                EndCursor = lastCommentFromParent?.Cursor,
                HasNextPage = lastCommentFromParent?.IsLastInDirectParent == false
            },
        };
    }
}
