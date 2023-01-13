import http.server

from config import config
from database import Database
import graphql_api_request_handler
import rest_api_request_handler


def database_connection_factory():
    return Database(
        config['DB_STORAGE_PATH'],
        config['DB_CACHED_STATEMENTS'],
        config['DB_ENABLE_AUTO_VACUUM'],
    )

class RestApiRequestHandler(rest_api_request_handler.RestAPIRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(
            database_connection_factory,
            config['JSON_PRETTY_PRINT'],
            config['ENABLE_OUTPUT_GZIP_COMPRESSION'],
            config['GZIP_COMPRESSION_LEVEL'],
            *args,
            **kwargs,
        )

class GraphQLApiRequestHandler(graphql_api_request_handler.GraphQLRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(
            database_connection_factory,
            config['JSON_PRETTY_PRINT'],
            config['ENABLE_OUTPUT_GZIP_COMPRESSION'],
            config['GZIP_COMPRESSION_LEVEL'],
            *args,
            **kwargs,
        )

request_handler = GraphQLApiRequestHandler if config['API_TYPE'] == 'GraphQL' else RestApiRequestHandler
http_server = http.server.ThreadingHTTPServer(('', config['PORT']), request_handler)
http_server.serve_forever()
