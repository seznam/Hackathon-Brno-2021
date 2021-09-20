const findCommentById = require('./utils/findCommentById');
const findCommentsByLocation = require('./utils/findCommentsByLocation');

module.exports = function batch({ db }) {
  return async function (ctx) {
    const promises = [];

    for (let i = ctx.request.body.length - 1; i >= 0; i--) {
      const {
        id,
        location,
        limit,
        after,
        replies1stLevelLimit,
        replies2ndLevelLimit,
        replies3rdLevelLimit,
      } = ctx.request.body[i];

      if (id) {
        promises.push(findCommentById(db, id));
      } else {
        promises.push(findCommentsByLocation(ctx, db, location, after, limit));
      }
    }

    ctx.body = await Promise.all(promises);
  };
};
