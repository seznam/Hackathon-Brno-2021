const { is, number, object, optional, string } = require('superstruct');
const validate = require('./utils/validate');
const findCommentsByLocation = require('./utils/findCommentsByLocation2');

module.exports = function ({ db }) {
  return async function get(ctx) {
    const col = db.collection('comments');

    const paramsObject = object({
      location: string(),
    });

    const queryParamsObject = object({
      limit: string(),
      after: optional(string()),
      replies1stLevelLimit: optional(string()),
      replies2ndLevelLimit: optional(string()),
      replies3rdLevelLimit: optional(string()),
    });

    if (
      !validate(ctx.params, paramsObject, ctx) ||
      !validate(ctx.request.query, queryParamsObject, ctx)
    ) {
      ctx.status = 400;
      return;
    }

    const { location } = ctx.params;

    let {
      limit,
      after,
      replies1stLevelLimit,
      replies2ndLevelLimit,
      replies3rdLevelLimit,
    } = ctx.request.query;

    limit = parseInt(limit);
    if (limit <= 0) {
      ctx.status = 400;
      return;
    }

    ctx.body = await findCommentsByLocation(
      ctx,
      db,
      location,
      after,
      limit,
      replies1stLevelLimit,
      replies2ndLevelLimit,
      replies3rdLevelLimit
    );
  };
};
