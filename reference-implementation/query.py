import graphene
import re

from comment_cursor import CommentCursor
from custom_types import (
    CommentData,
    Connection,
    Cursor,
    Edge,
    MillisecondUnixTimestamp,
    Node,
    PageInfo,
    URL,
)


class Comment(graphene.ObjectType):
    '''Record representing a persisted (or soon-to-be persisted) comment.'''

    class Meta:
        interfaces = (Node, CommentData)

    parent = graphene.Field(
        'query.Comment',
        description='''
            The comment to which this comment is a reply to. This field is null
            if this is a root comment (not a reply to another comment).
        ''',
    )

    created = graphene.Field(
        MillisecondUnixTimestamp,
        required=True,
        description='''
            The UNIX timestamp with millisecond precision of the moment the
            comment has been accepted by the API. Note the comment may be
            persisted at a later moment.
        ''',
    )

    replies_start_cursor = graphene.Field(
        Cursor,
        required=True,
        description='''
            A cursor that can be used to retrieve the direct replies to this
            comment (this is an alternative to the replies resolver). The
            comments returned when using this cursor are chronologically sorted
            from the oldest to the newest.
        ''',
    )

    replies = graphene.Field(
        'query.CommentsConnection',
        required=True,
        description='''
            Retrieves the direct replies to this comment. The returned comments
            are chronologically sorted from the oldest to the newest. Replies
            to the returned comments can be retrieved using the replies field
            of the returned comments or using their repliesStartCursor cursors.
            The resolver returns the beginning of replies if no cursor is
            provided.
        ''',
        first=graphene.Int(required=True),
        after=graphene.Argument(Cursor),
    )

    __web_page_url = None

    def __init__(self, web_page_url, **kwargs):
        super().__init__(**kwargs)
        self.__web_page_url = web_page_url

    @staticmethod
    def resolve_parent(parent, info):
        if parent.parent is None or parent.parent is Comment:
            return parent.parent

        if parent.parent is dict and 'created' in parent.parent:
            return Comment(parent.__web_page_url, **parent.parent)

        if parent.parent is dict:
            parent_id = parent.parent['id']
        else:
            parent_id = parent.parent

        db = info.context['db']
        comment = db.get_comment(parent_id)
        return Comment(
            parent.__web_page_url,
            **comment,
        ) if comment is not None else None

    @staticmethod
    def resolve_replies_start_cursor(parent, info):
        # Note: returning the following with after_id=parent.id set as well
        # might provide slightly better performance if the returned cursor is
        # used.
        return str(CommentCursor(parent_comment_id=parent.id))

    @staticmethod
    def resolve_replies(parent, info, first, after=None):
        db = info.context['db']
        if after is not None:
            cursor = CommentCursor(after)
        else:
            cursor = CommentCursor(
                Comment.resolve_replies_start_cursor(parent, info),
            )
        return get_comments_connection(
            db,
            parent.__web_page_url,
            first,
            cursor,
        )


class CommentsEdge(graphene.ObjectType):
    '''Edge of a comment records connection referencing a single comment.'''

    class Meta:
        interfaces = (Edge, )

    node = graphene.Field(
        Comment,
        required=True,
        description='''
            The single comment record referenced by the connection edge.
        ''',
    )

    __parent_comment_id = None

    def __init__(self, parent_comment_id=None, **kwargs) -> None:
        super().__init__(**kwargs)
        self.__parent_comment_id = parent_comment_id

    @staticmethod
    def resolve_cursor(parent, info):
        return str(CommentCursor(
            parent_comment_id=parent.__parent_comment_id,
            after_id=parent.node.id,
        ))


class CommentsConnection(graphene.ObjectType):
    '''Records connection containing a single page of comment records.'''

    class Meta:
        interfaces = (Connection, )

    edges = graphene.List(
        graphene.NonNull(CommentsEdge),
        required=True,
        description='Edges in the comment record connections graph.',
    )


class WebPage(graphene.ObjectType):
    '''
    A virtual (non-persistent) record used to access comments related to a
    specific web page. The API does not validate that the web page's URL points
    to an actual existing web page accessing on the internet.
    '''

    class Meta:
        interfaces = (Node, )

    comments = graphene.Field(
        CommentsConnection,
        required=True,
        description='''
            Retrieves the comments related to the current web page. The
            returned comments are chronologically sorted from the oldest to the
            newest. The comments are of a single reply-to hierarchy level, any
            replies to the returned comments must be fetched via the replies
            Comment's field or using the repliesStartCursor. The resolver
            returns the beginning of root comments if no cursor is provided.
        ''',
        first=graphene.Int(required=True),
        after=graphene.Argument(Cursor),
    )

    @staticmethod
    def resolve_comments(parent, info, first, after=None):
        db = info.context['db']
        cursor = CommentCursor(after)
        return get_comments_connection(db, parent.id, first, cursor)


class Query(graphene.ObjectType):
    '''Queries supported by this API.'''

    web_page = graphene.Field(
        WebPage,
        required=True,
        description='''
            Returns the web page record at the specified location denoted by
            the provided URL. This is used to retrieve comments related to the
            given web page.
        ''',
        location=graphene.Argument(URL, required=True),
    )

    node = graphene.Field(
        Node,
        description='''
            Retrieves any record by its unique identifier. Only support for
            comments is required. Passing a URL as ID may return a WebPage.
        ''',
        id=graphene.ID(required=True),
    )

    @staticmethod
    def resolve_web_page(parent, info, location):
        return WebPage(id=location)

    @staticmethod
    def resolve_node(parent, info, id):
        if re.match(r'^\d+$', id) is not None:
            db = info.context['db']
            # Note: this could be optimized by doing this in a single DB query
            comment = db.get_comment(id)
            web_page = db.get_webpage_of_comment(id)
            return Comment(
                web_page,
                **comment,
            ) if comment is not None else None

        return Query.resolve_web_page(parent, info, id)


def get_comments_connection(db, webpage, first, after_cursor):
    (comments, has_next_page) = get_comments(db, webpage, first, after_cursor)
    edges = list(map(
        lambda comment: CommentsEdge(
            node=comment,
            parent_comment_id=after_cursor.parent_comment_id,
        ),
        comments,
    ))

    end_cursor = None
    if len(comments) > 0:
        end_cursor = CommentCursor(
            parent_comment_id=after_cursor.parent_comment_id,
            after_id=comments[-1:][0].id,
        )

    return CommentsConnection(
        page_info=PageInfo(
            has_next_page=has_next_page,
            end_cursor=str(end_cursor) if end_cursor is not None else None,
        ),
        edges=edges,
    )


def get_comments(db, webpage, first, after_cursor):
    if first <= 0:
        raise ValueError(
            'The "first" argument must be a positive integer that fits ' +
            'within the range of a 32-bit signed integer (up to ' +
            '2 147 483 647)',
        )

    comments_data = db.get_comments(
        webpage,
        after_cursor.parent_comment_id,
        after_cursor.after_id,
        first + 1,
    )

    has_next_page = len(comments_data) == first + 1
    comments = list(map(
        lambda comment: Comment(webpage, **comment),
        comments_data[:-1] if has_next_page else comments_data,
    ))
    return (comments, has_next_page)
