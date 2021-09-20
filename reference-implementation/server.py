import graphene
import gzip
import http.server
import io
import json
import re

from custom_types import (
    CommentData,
    Connection,
    Cursor,
    Edge,
    MillisecondUnixTimestamp,
    Node,
    URL
)
from database import Database
from mutation import Mutation
from query import Query
from config import config


schema = graphene.Schema(
    query=Query,
    mutation=Mutation,
    types=[
        CommentData,
        Connection,
        Cursor,
        Edge,
        MillisecondUnixTimestamp,
        Node,
        URL,
    ],
)

with open('./operations.graphql', 'r', encoding='utf-8') as operations_file:
    server_defined_operations = operations_file.read()


class RequestHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs, directory='./graphiql')

    def do_POST(self):
        content_length = self.headers.get('Content-Length')
        encoded_body = self.rfile.read(int(content_length))
        accepted_encodings = re.split(
            r'(?:;q=\d+(?:\.\d+)?)?\s*,\s*',
            self.headers.get('Accept-Encoding', ''),
        )
        if self.headers.get('Content-Encoding') == 'gzip':
            encoded_body = gzip.decompress(encoded_body)
            pass
        body = json.loads(encoded_body)
        operation_name = body.get('operationName')
        self.log_message('GraphQL operation name: %s', operation_name)

        query = body['query'] if 'query' in body else server_defined_operations
        db = Database(
            config['DB_STORAGE_PATH'],
            config['DB_CACHED_STATEMENTS'],
            config['DB_ENABLE_AUTO_VACUUM'],
        )
        result = schema.execute(
            query,
            variables=body.get('variables'),
            operation_name=operation_name,
            context={'db': db},
        )
        db.close()

        if result.errors is not None:
            result_dict = {
                'errors': [
                    {
                        'message': getattr(error, 'message', str(error)),
                        'locations': [
                            {
                                'line': location.line,
                                'column': location.column,
                            } for location in ((getattr(error, 'locations', [])) or [])
                        ],
                    } for error in result.errors
                ],
            }
        else:
            result_dict = {'data': result.data}

        if config['JSON_PRETTY_PRINT']:
            serialized_result = json.dumps(
                result_dict,
                sort_keys=True,
                indent=4,
            )
        else:
            serialized_result = json.dumps(result_dict)
        encoded_result = serialized_result.encode(errors='surrogateescape')
        gzipped_result = False
        if config['ENABLE_OUTPUT_GZIP_COMPRESSION'] and 'gzip' in accepted_encodings:
            encoded_result = gzip.compress(encoded_result, 9)
            gzipped_result = True
        response_body = io.BytesIO()
        response_body.write(encoded_result)
        response_body.seek(0)

        self.send_response(http.HTTPStatus.OK)
        self.send_header('Content-Type', 'application/json')
        self.send_header("Content-Length", str(len(encoded_result)))
        if gzipped_result:
            self.send_header('Content-Encoding', 'gzip')
        self.end_headers()

        self.copyfile(response_body, self.wfile)
        response_body.close()


http_server = http.server.ThreadingHTTPServer(('', config['PORT']), RequestHandler)
http_server.serve_forever()
