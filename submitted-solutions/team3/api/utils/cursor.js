function encode(parentId, date) {
  return Buffer.from(`${parentId}:${date}`).toString('base64');
}

function decode(cursor) {
  return Buffer.from(cursor, 'base64').toString().split(':');
}

module.exports = {
  encode,
  decode,
};
