module.exports = function ({ db }) {
  return async function remove(ctx) {
    const collection = db.collection('comments');
    await collection.deleteMany();

    ctx.status = 204;
  };
};
