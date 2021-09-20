const Koa = require('koa');
const app = new Koa();
const bodyParser = require('koa-bodyparser');
const Router = require('koa-router');
const { MongoClient } = require('mongodb');

const batch = require('./batch');
const clearComments = require('./clear-comments');
const createComment = require('./create-comment');
const findComment = require('./find-comment');
const listComments = require('./list-comments');
const logger = require('./logger');

const port = 8000;
(async () => {
  const dbUrl = process.env.APP_DB_URL || 'mongodb://localhost:27017';

  const db = await MongoClient.connect(dbUrl, {
    useNewUrlParser: true,
    useUnifiedTopology: true,
  }).then((client) => client.db());

  const col = db.collection('comments');

  try {
    await col.createIndex({ parentId: 1, location: 1 });
    await col.createIndex({ created: 1 });
    await col.createIndex({ parentId: 1 });
  } catch (err) {
    console.error('Indexes already created');
  }
  const state = { db };

  const routes = new Router()
    .get('/comment/:id', findComment(state))
    .get('/webpage//comment', (ctx) => (ctx.status = 400))
    .get('/webpage/:location/comment', listComments(state))
    .post('/webpage//comment', (ctx) => (ctx.status = 400))
    .post('/webpage/:location/comment', createComment(state))
    .post('/batch', batch(state))
    .delete('/comment', clearComments(state))
    .routes();

  app
    .use(bodyParser({ strict: false }))
    .use(logger)
    .use(routes);

  app.listen(port);
})()
  .then(() => console.log(`Listening on ${port} ...`))
  .catch((err) => console.log(`Err: ${err}`));
