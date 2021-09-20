const cursor = require('./cursor');

const generateQuery = (ids, limit) => {
  let query = [
    {
      $match: {
        parentId: { $in: ids },
      },
    },
    {
      $sort: {
        created: 1,
      },
    },
    {
      $group: {
        _id: '$parentId',
        items: {
          $push: '$$ROOT',
        },
      },
    },
    {
      $project: {
        items: { $slice: ['$items', limit + 1] },
      },
    },
    {
      $unwind: {
        path: '$items',
        preserveNullAndEmptyArrays: true,
      },
    },
    {
      $project: {
        _id: '$items._id',
        parentId: '$items.parentId',
        author: '$items.author',
        text: '$items.text',
        created: '$items.created',
      },
    },
  ];

  return query;
};

module.exports = async function findCommentsByLocation(
  ctx,
  db,
  location,
  after,
  limit,
  limit1 = 0,
  limit2 = 0,
  limit3 = 0
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
    paramsArray.push({
      parentId: afterParams[0] === '0' ? null : afterParams[0],
    });
    paramsArray.push({ created: { $gt: parseInt(afterParams[1]) } });
  }

  if ((limit2 && !limit1) || (limit3 && !limit2)) {
    ctx.status = 400;
    return;
  }

  let maxlimit = 3;

  if (!limit1) {
    maxlimit = 1;
  } else if (!limit2) {
    maxlimit = 2;
  } else if (!limit3) {
    maxlimit = 3;
  }

  let mongoQuery = [
    {
      $match: {
        $and: paramsArray,
      },
    },
    {
      $limit: limit + 1,
    },
    {
      $sort: {
        created: 1,
      },
    },
  ];

  let results = [];

  results.push(
    await col.aggregate(mongoQuery, { allowDiskUse: true }).toArray()
  );

  const mongoQuery2 = generateQuery(
    results[0].map((x) => x._id),
    parseInt(limit1)
  );

  results.push(
    await col.aggregate(mongoQuery2, { allowDiskUse: true }).toArray()
  );

  if (maxlimit > 1) {
    const mongoQuery3 = generateQuery(
      results[1].map((x) => x._id),
      parseInt(limit2)
    );

    results.push(
      await col.aggregate(mongoQuery3, { allowDiskUse: true }).toArray()
    );
    if (maxlimit > 2) {
      const mongoQuery4 = generateQuery(
        results[2].map((x) => x._id),
        parseInt(limit3)
      );

      results.push(
        await col.aggregate(mongoQuery4, { allowDiskUse: true }).toArray()
      );
    }
  }

  let hashTable = {};

  for (let node of results[0]) {
    node.children = [];
    hashTable[node._id] = node;
  }
  for (let node of results[1]) {
    node.children = [];

    hashTable[node._id] = node;

    if (maxlimit > 1) {
      if (hashTable[node.parentId].children.length >= limit1) {
        hashTable[node.parentId].hasMore = true;
      } else {
        hashTable[node.parentId].children.push(node);
      }
    }
  }
  if (maxlimit > 1) {
    for (let node of results[2]) {
      node.children = [];
      hashTable[node._id] = node;

      if (maxlimit > 2) {
        if (hashTable[node.parentId].children.length >= limit2) {
          hashTable[node.parentId].hasMore = true;
        } else {
          hashTable[node.parentId].children.push(node);
        }
      }
    }
    if (maxlimit > 2) {
      for (let node of results[3]) {
        hashTable[node._id] = node;

        if (hashTable[node.parentId].children.length >= limit3) {
          hashTable[node.parentId].hasMore = true;
        } else {
          hashTable[node.parentId].children.push(node);
        }
      }
    }
  }

  let level0HasMore = false;

  if (results[0] && results[0].length > limit) {
    results[0].pop();
    level0HasMore = true;
  }

  return map(level0HasMore, results[0]);
};

function map(hasMore, nodes = []) {
  return {
    pageInfo: {
      hasNextPage: hasMore,
      endCursor:
        nodes.length > 0
          ? cursor.encode(
              nodes[nodes.length - 1]._id,
              nodes[nodes.length - 1].created
            )
          : null,
    },
    edges: nodes.map((item) => ({
      cursor: cursor.encode(item.parentId || 0, item.created),
      node: {
        id: item._id,
        author: item.author,
        text: item.text,
        parent: item.parentId ? { id: item.parentId } : null,
        created: item.created,
        repliesStartCursor: cursor.encode(item._id || 0, item.created),
        replies: map(item.hasMore || false, item.children),
      },
    })),
  };
}
