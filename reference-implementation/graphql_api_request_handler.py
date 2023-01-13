import graphene

from custom_types import (
    CommentData,
    Connection,
    Cursor,
    Edge,
    MillisecondUnixTimestamp,
    Node,
    URL
)
from json_request_handler import JsonRequestHandler
from mutation import Mutation
from query import Query

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

class GraphQLRequestHandler(JsonRequestHandler):
    def __init__(self, database_connection_factory, pretty_print, allow_gzip, gzip_level, *args, **kwargs):
        self.__database_connection_factory = database_connection_factory
        self.__pretty_print = pretty_print
        self.__allow_gzip = allow_gzip
        self.__gzip_level = gzip_level
        super().__init__(*args, **kwargs, directory='./graphiql')

    def do_POST(self):
        body = self.read_json_request_body()
        operation_name = body.get('operationName')
        self.log_message('GraphQL operation name: %s', operation_name)

        query = body['query'] if 'query' in body else server_defined_operations
        db = self.__database_connection_factory()
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

        self.send_json_response(result_dict, self.__pretty_print, self.__allow_gzip, self.__gzip_level)
