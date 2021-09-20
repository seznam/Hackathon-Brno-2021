const cursor = require('./cursor');

module.exports = async function findCommentsByLocation(
  ctx,
  db,
  location,
  after,
  limit
) {
  const col = db.collection('comments');

  let paramsArray = [{ location }];

  if (after === undefined) paramsArray.push({ parentId: null });
  else {
    var afterParams = cursor.decode(after);
    if (afterParams.length != 2) {
      ctx.status = 400;
      return;
    }
    paramsArray.push({ parentId: afterParams[0] });
    paramsArray.push({ created: { $gt: parseInt(afterParams[1]) } });
  }

  let mongoQuery = [
    {
      $match: {
        $and: paramsArray,
      },
    },
    {
      $graphLookup: {
        from: 'comments',
        startWith: '$_id',
        connectFromField: '_id',
        connectToField: 'parentId',
        as: 'children',
        maxDepth: 2,
        depthField: 'level',
      },
    },
    {
      $limit: limit,
    },
  ];

  let result = await col
    .aggregate(mongoQuery, { allowDiskUse: true })
    .toArray();

  var tree = [];
  for (var item of result) {
    item.children.sort((a, b) => {
      if (a.level > b.level) return 1;
      else if (a.level < b.level) return -1;
      else {
        return a.created > b.created ? 1 : b.created > a.created ? -1 : 0;
      }
    });
    item.children.push({
      _id: item._id,
      author: item.author,
      text: item.text,
      parent: item.parentId,
      created: item.created,
    });
    tree.push(createDataTree(item.children));
  }
  return map(tree);
};

function createDataTree(dataset) {
  const hashTable = Object.create(null);
  dataset.forEach(
    (aData) =>
      (hashTable[aData._id] = {
        cursor: cursor.encode(aData.parentId || 0, aData.created),
        id: aData._id,
        author: aData.author,
        text: aData.text,
        parent: aData.parentId ? { id: aData.parentId } : null,
        created: aData.created,
        repliesStartCursor: cursor.encode(aData._id || 0, aData.created),
        children: [],
      })
  );
  let dataTree = {};
  dataset.forEach((aData) => {
    if (aData.parentId)
      hashTable[aData.parentId].children.push(hashTable[aData._id]);
    else dataTree = hashTable[aData._id];
  });
  return dataTree;
}

function map(nodes = []) {
  return {
    pageInfo: {
      hasNextPage: true,
      endCursor: nodes.length > 0 ? nodes[nodes.length - 1].id : null,
    },
    edges: nodes.map((item) => ({
      cursor: item.cursor,
      node: {
        id: item.id,
        author: item.author,
        text: item.text,
        parent: item.parent,
        created: item.created,
        repliesStartCursor: item.repliesStartCursor,
        replies: map(item.children),
      },
    })),
  };
}
