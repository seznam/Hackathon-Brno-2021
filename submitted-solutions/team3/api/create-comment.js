const { createHash } = require('crypto');
const { size, object, string, nullable } = require('superstruct');
const { v4: uuid } = require('uuid');
const cursor = require('./utils/cursor');
const validate = require('./utils/validate');

const ids = new Map();

const CreateWepPageRequest = object({
  author: size(string(), 1, 36),
  text: string(),
  parent: nullable(size(string(), 1, 36)),
});

module.exports = function ({ db }) {
  const collection = db.collection('comments');

  return async function post(ctx) {
    if (!validate(ctx.request.body, CreateWepPageRequest, ctx)) {
      ctx.status = 400;
      return;
    }

    const bytes = Buffer.byteLength(ctx.request.body.text, 'utf-8');
    if (bytes > 131072 || bytes < 5) {
      ctx.status = 400;
      return;
    }

    const { author, text, parent: parentId } = ctx.request.body;
    const { location } = ctx.params;

    if (parentId) {
      const oldHash = ids.get(parentId);
      ctx.assert(oldHash, 400);

      const newHash = createHash('sha1').update(location).digest('base64');
      ctx.assert(newHash === oldHash, 400);
    }

    const comment = {
      _id: uuid(),
      parentId,
      author,
      text,
      location,
      created: Date.now(),
    };

    await collection.insertOne(comment);

    ids.set(comment._id, createHash('sha1').update(location).digest('base64'));

    ctx.body = {
      id: comment._id,
      author: comment.author,
      text: comment.text,
      parent: comment.parentId ? { id: comment.parentId } : null,
      created: comment.created,
      repliesStartCursor: cursor.encode(comment._id, comment.created),
      replies: {
        pageInfo: {
          hasNextPage: false,
          endCursor: null,
        },
        edges: [],
      },
    };

    ctx.status = 201;
  };
};
