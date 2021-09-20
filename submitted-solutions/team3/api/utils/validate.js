const { validate } = require('superstruct');

module.exports = (data, schema, ctx) => {
  const [error, result] = validate(data, schema);

  if (!result && process.env.NODE_ENV !== 'production') {
    // console.error(`${ctx.method} ${ctx.url} ${error.message}`);
    return;
  }

  return result;
};
