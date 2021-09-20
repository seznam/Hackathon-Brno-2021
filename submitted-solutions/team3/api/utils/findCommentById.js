const cursor = require('./cursor');

module.exports = async function findCommentById(db, id) {
  const collection = db.collection('comments');

  const comment = await collection.findOne({ _id: id });
  if (!comment) {
    return null;
  }

  const replies = await collection.find({ parentId: id }).toArray();

  return {
    id: comment._id,
    author: comment.author,
    text: comment.text,
    parent: comment.parentId ? { id: comment.parentId } : null,
    created: comment.created,
    repliesStartCursor: replies[0] ? replies[0]._id : null,
    replies: {
      pageInfo: {
        hasNextPage: false,
        endCursor: cursor.encode(id || 0, comment.created),
      },
      edges: replies.map((reply) => ({
        cursor: reply._id,
        node: {
          id: reply._id,
          author: reply.author,
          text: reply.text,
          parent: reply.parentId ? { id: reply.parentId } : null,
          created: reply.created,
        },
      })),
    },
  };
};
