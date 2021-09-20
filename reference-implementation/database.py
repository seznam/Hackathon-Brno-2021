import datetime
import sqlite3

MAX_AUTHOR_LENGTH = 36
CONTENT_MIN_LENGTH = 5
CONTENT_MAX_LENGTH = 128 * 1024


class Database:
    def __init__(self, db_file_path, cached_statements, enable_auto_vacuum):
        self.connection = sqlite3.connect(
            db_file_path,
            cached_statements=cached_statements,
        )
        self.connection.row_factory = sqlite3.Row
        self.connection.execute('PRAGMA journal_mode=WAL')
        self.connection.execute('PRAGMA foreign_keys = ON')
        if enable_auto_vacuum:
            self.connection.execute('PRAGMA auto_vacuum = FULL')
        # Note: we cannot use rowid as the primary key, because foreing keys
        # referencing the rowid column are not allowed - see
        # https://www.sqlite.org/foreignkeys.html.
        self.connection.execute('''
            CREATE TABLE IF NOT EXISTS comment (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                webpage TEXT NOT NULL,
                parent INTEGER REFERENCES comment(id)
                    ON DELETE CASCADE
                    ON UPDATE CASCADE,
                author TEXT NOT NULL,
                content TEXT NOT NULL,
                created TEXT DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        self.connection.commit()

    def get_comment(self, comment_id):
        cursor = self.connection.execute(
            '''
                SELECT parent, author, content, created
                FROM comment
                WHERE id = :id
                LIMIT 1
            ''',
            {'id': comment_id},
        )
        data = cursor.fetchone()
        return self.__create_comment(comment_id, data)

    def get_webpage_of_comment(self, comment_id):
        cursor = self.connection.execute(
            'SELECT webpage FROM comment WHERE id = :id LIMIT 1',
            {'id': comment_id},
        )
        data = cursor.fetchone()
        return data['webpage'] if data is not None else None

    def get_comments(self, webpage, parent, after_id, limit):
        cursor = self.connection.execute(
            '''
                SELECT id, parent, author, content, created
                FROM comment
                WHERE
                    webpage = :webpage AND
                    (
                        (parent IS NULL AND :parent IS NULL) OR
                        parent = :parent
                    ) AND
                    id > :after_id
                LIMIT :limit
            ''',
            {
                'webpage': webpage,
                'parent': parent,
                'after_id': after_id,
                'limit': limit,
            },
        )
        rows = cursor.fetchall()
        return list(map(
            lambda row: self.__create_comment(row['id'], row),
            rows,
        ))

    def post_comment(self, webpage, parent, author, text):
        if len(author.encode()) == 0:
            raise ValueError('The author cannot be an empty string')
        if len(author.encode()) > MAX_AUTHOR_LENGTH:
            raise ValueError(
                'The author can be only up to 36 bytes long, ' +
                f'"{author}" was provided'
            )
        if len(text.encode()) < CONTENT_MIN_LENGTH:
            raise ValueError(
                f'The text must be at least {CONTENT_MIN_LENGTH} bytes long,' +
                f'"{text}" was provided'
            )
        if len(text.encode()) > CONTENT_MAX_LENGTH:
            raise ValueError(
                f'The text must be only up to {CONTENT_MAX_LENGTH} bytes ' +
                f'long, "{text}" was provided'
            )

        cursor = self.connection.execute(
            '''
                INSERT INTO comment (webpage, parent, author, content)
                VALUES (:webpage, :parent, :author, :content)
            ''',
            {
                'webpage': webpage,
                'parent': parent,
                'author': author,
                'content': text,
            },
        )
        comment_id = cursor.lastrowid
        self.connection.commit()
        return self.get_comment(comment_id)

    def delete_all_comments(self):
        self.connection.execute('DELETE FROM comment')
        self.connection.commit()

    def close(self):
        self.connection.close()

    def __create_comment(self, comment_id, data):
        if data is None:
            return None

        return {
            'id': comment_id,
            'parent': data['parent'],
            'author': data['author'],
            'text': data['content'],
            'created': datetime.datetime.fromisoformat(data['created']),
        }
