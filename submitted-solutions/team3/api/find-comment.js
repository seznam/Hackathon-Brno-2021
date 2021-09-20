const findCommentById = require('./utils/findCommentById');

module.exports = function ({ db }) {
  return async function get(ctx) {
    const { id } = ctx.params;

    const comment = await findCommentById(db, id);
    if (!comment) {
      ctx.status = 404;
      return;
    }

    ctx.body = comment;
  };
};
