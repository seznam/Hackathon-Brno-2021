import re

class CommentCursor:
    parent_comment_id = None
    after_id = 0

    def __init__(self, cursor=None, parent_comment_id=None, after_id=None):
        if cursor is not None:
            if re.match(r'^\d*:\d+$', cursor) is None:
                raise ValueError('The provided cursor is invalid')
            (parsed_parent_comment_id, _, after_id) = cursor.partition(':')
            if parsed_parent_comment_id != '':
                self.parent_comment_id = int(parsed_parent_comment_id)
            self.after_id = int(after_id)
        if parent_comment_id is not None:
            self.parent_comment_id = parent_comment_id
        if after_id is not None:
            self.after_id = after_id

    def __str__(self):
        serialized_parent_comment_id = ''
        if self.parent_comment_id is not None:
            serialized_parent_comment_id = str(self.parent_comment_id)
        return f'{serialized_parent_comment_id}:{self.after_id}'
