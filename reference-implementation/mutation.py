import graphene
from query import Comment, URL


class CommentInput(graphene.InputObjectType):
    '''
        Input data type representing the required comment data for the
        postComment mutation.
    '''

    parent = graphene.ID(
        description='''
            Unique identifier of the comment to which this comment replies. Use
            null for root comments (comments that are not replies to other
            comments).
        '''
    )

    author = graphene.ID(
        required=True,
        description='''
            Unique identifier of the comment's author. This can be any
            non-empty string of up to 36 bytes (UUID textual representation).
        '''
    )

    text = graphene.String(
        required=True,
        description='''
            Comment's actual content. The content must be at least 5 bytes
            long, may contain any valid Unicode characters and can be up to 128
            KiB long.
        '''
    )


class PostComment(graphene.Mutation):
    class Arguments:
        context = graphene.NonNull(URL)
        comment = CommentInput(required=True)

    Output = Comment

    @staticmethod
    def mutate(parent, info, context, comment):
        db = info.context['db']

        parent_id = comment.get('parent')
        if parent_id is not None:
            parent_web_page = db.get_webpage_of_comment(parent_id)
            if parent_web_page is None:
                raise ValueError(
                    'The specified parent comment with the ID ' +
                    f'"{parent_id}" does not exist'
                )
            if parent_web_page != context:
                raise ValueError(
                    'The specified parent comment is related to the web ' +
                    f'page "{parent_web_page}", but the web page ' +
                    f'"{context}" was provided for the new comment to post'
                )

        return Comment(context, **db.post_comment(
            context,
            parent_id,
            comment['author'],
            comment['text'],
        ))


class DeleteAllComments(graphene.Mutation):
    Output = graphene.Boolean

    @staticmethod
    def mutate(parent, info):
        db = info.context['db']
        db.delete_all_comments()
        return True


class Mutation(graphene.ObjectType):
    '''Mutations supported by this API.'''

    post_comment = PostComment.Field(
        required=True,
        description='''
            Persists the provided comment. While the comment's ID must be
            generated synchronously, the comment itself may be persisted at a
            later moment.
        '''
    )

    delete_all_comments = DeleteAllComments.Field(
        required=True,
        description='''
            Deletes all persisted comments and comments scheduled for
            persisting. Returns true iff the operation was successful.
        '''
    )
