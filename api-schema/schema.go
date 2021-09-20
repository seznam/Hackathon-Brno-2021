package apischema

type ID string
type MillisecondUnixTimestamp int64
type URL string
type Cursor string

type PageInfo struct {
	HasNextPage bool
	EndCursor   *Cursor
}

type CommentsConnection struct {
	Edges    []*CommentsEdge
	PageInfo *PageInfo
}

type CommentsEdge struct {
	Node   *Comment
	Cursor Cursor
}

type Node struct {
	Id ID
}

type Comment struct {
	Id                 ID
	Parent             *Node
	Author             ID
	Text               string
	Created            MillisecondUnixTimestamp
	RepliesStartCursor Cursor
	Replies            *CommentsConnection
}

type CommentInput struct {
	Parent *ID    `json:"parent"`
	Author ID     `json:"author"`
	Text   string `json:"text"`
}

type CommentsApiAdapter interface {
	Comments(context URL, first int, after *Cursor) (*CommentsConnection, error)
	CommentsWithDirectReplies(context URL, first int, after *Cursor, firstReplies int) (*CommentsConnection, error)
	CommentsWithDeepReplies(
		context URL,
		first int,
		after *Cursor,
		firstReplies1stLevel int,
		firstReplies2ndLevel int,
		firstReplies3rdLevel int,
	) (*CommentsConnection, error)
	DirectRepliesToComment(context URL, repliesCursor Cursor, first int) (*CommentsConnection, error)
	SingleComment(id ID) (*Comment, error)
	SingleComments(id ...ID) ([]*Comment, error)
	PostComment(context URL, comment *CommentInput) (*Comment, error)
	DeleteAllComments() (bool, error)
}
