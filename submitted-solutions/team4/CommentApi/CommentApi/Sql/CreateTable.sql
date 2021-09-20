DROP TABLE IF EXISTS Comment;

CREATE UNLOGGED TABLE Comment (
  Id uuid PRIMARY KEY,
  Author varchar(36) not null,
  Text text not null,
  Created bigint not null,
  ParentId uuid null,
  Cursor varchar(2048) not null,
  LocationHash char(64) not null,
  Level integer not null,
  Parent0 integer null,
  Parent1 integer null,
  Parent2 integer null,
  OrderInDirectParent integer not null,
  IsLastInDirectParent boolean not null
);


CREATE INDEX search_index 
  ON Comment(Id, ParentId, LocationHash, Cursor, Level, OrderInDirectParent);

CREATE INDEX Cursor_index
    ON Comment(Cursor text_pattern_ops);

CREATE INDEX Id_index
    ON Comment(Id);

CREATE INDEX FindLastInParent_index ON Comment(ParentId) INCLUDE ( OrderInDirectParent );

CREATE INDEX FindLastInDirectParent_index ON Comment(Level, LocationHash) INCLUDE ( OrderInDirectParent );