const fs = require('fs');
const csvStringify = require('csv-stringify/lib/sync');

const PATH = './tmp';
const FILE_PATH_TXT = `${PATH}/log.txt`;
const FILE_PATH_CSV = `${PATH}/log.csv`;

let counter = 0;

module.exports = async function logger(ctx, next) {
  const time = new Date().toISOString();

  if (!fs.existsSync(PATH)) fs.mkdirSync(PATH);

  //   fs.appendFileSync(FILE_PATH_TXT, `${time}: ${ctx.method} ${ctx.url}\n`);
  //   fs.appendFileSync(
  //     FILE_PATH_CSV,
  //     csvStringify([
  //       {
  //         time,
  //         method: ctx.method,
  //         url: ctx.url,
  //         body: ctx.request.body ? JSON.stringify(ctx.request.body) : '',
  //       },
  //     ])
  //   );

  //   counter++;
  //   if (counter >= 94) {
  //     console.log(`${counter} ${ctx.method} ${ctx.url.substr(0, 50)} `);
  //   }
  //   if (counter === 69) console.log(`${counter} ${ctx.method} ${ctx.url}`);
  //   if (!(counter % 100)) console.log(counter);
  return next();
};
