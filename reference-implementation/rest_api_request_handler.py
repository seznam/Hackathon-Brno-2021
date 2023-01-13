import http
import re
from urllib.parse import unquote, urlsplit, parse_qs


from comment_cursor import CommentCursor
from json_request_handler import JsonRequestHandler

class RestAPIRequestHandler(JsonRequestHandler):
    def __init__(self, database_connection_factory, pretty_print, allow_gzip, gzip_level, *args, **kwargs):
        self.__database_connection_factory = database_connection_factory
        self.__pretty_print = pretty_print
        self.__allow_gzip = allow_gzip
        self.__gzip_level = gzip_level
        super().__init__(*args, **kwargs, directory='./no-content')

    def do_HEAD(self) -> None:
        self.send_response(http.HTTPStatus.METHOD_NOT_ALLOWED)
        self.end_headers()

    def do_GET(self) -> None:
        listed_comments_location_match = re.fullmatch(r'/webPage/([^/]*)/comment([?].*)?', self.path)
        retrieved_comment_id_match = re.fullmatch(r'/comment/([^/]+)([?].*)?', self.path)
        if listed_comments_location_match != None:
            [listed_comments_location, queryString] = listed_comments_location_match.groups()
            query = parse_qs(queryString[1:]) if queryString is not None else {}
            try:
                parameters = self.__load_comment_listing_input(unquote(listed_comments_location), query)
            except (ValueError, TypeError):
                self.send_response(http.HTTPStatus.BAD_REQUEST)
                self.end_headers()
                raise
            comments = self.__list_comments(parameters)
            self.send_json_response(comments, self.__pretty_print, self.__allow_gzip, self.__gzip_level)
        elif retrieved_comment_id_match != None:
            retrieved_comment_id = retrieved_comment_id_match.group(1)
            comment = self.__get_comment_by_id(retrieved_comment_id)
            if comment is not None:
                self.send_json_response(comment, self.__pretty_print, self.__allow_gzip, self.__gzip_level)
            else:
                self.send_response(http.HTTPStatus.NOT_FOUND)
                self.end_headers()
        else:
            self.send_response(http.HTTPStatus.NOT_FOUND)
            self.end_headers()

    def do_POST(self) -> None:
        posted_comment_location_match = re.fullmatch(r'/webPage/([^/]*)/comment([?].*)?', self.path)
        if posted_comment_location_match != None:
            posted_comment_location = unquote(posted_comment_location_match.group(1))
            try:
                self.__validate_url(posted_comment_location)
            except ValueError:
                self.send_response(http.HTTPStatus.BAD_REQUEST)
                self.end_headers()
                raise
            comment_data = self.read_json_request_body()
            if type(comment_data) is not dict:
                self.send_response(http.HTTPStatus.BAD_REQUEST)
                self.end_headers()
                return
            parent_id = comment_data.get('parent')
            if (
                type(comment_data) is not dict or
                'author' not in comment_data or
                'text' not in comment_data or
                (parent_id != None and (len(parent_id) < 1 or len(parent_id) > 36))
            ):
                self.send_response(http.HTTPStatus.BAD_REQUEST)
                self.end_headers()
                return

            db = self.__database_connection_factory()
            try:
                if parent_id is not None:
                    parent_web_page = db.get_webpage_of_comment(parent_id)
                    if parent_web_page != posted_comment_location:
                        raise ValueError(
                            f'The specified parent with ID {parent_id} is not located at {posted_comment_location}',
                        )
                comment = self.__create_comment_from_data(db.post_comment(
                    posted_comment_location,
                    parent_id,
                    comment_data['author'],
                    comment_data['text'],
                ))
                self.send_json_response(
                    comment,
                    self.__pretty_print,
                    self.__allow_gzip,
                    self.__gzip_level,
                    http.HTTPStatus.CREATED,
                )
            except ValueError:
                self.send_response(http.HTTPStatus.BAD_REQUEST)
                self.end_headers()
                raise
            finally:
                db.close()
        elif self.path == '/batch' or self.path[:7] == '/batch?':
            operations = self.read_json_request_body()
            if type(operations) is not list:
                self.send_response(http.HTTPStatus.BAD_REQUEST)
                self.end_headers()
                return
            results = list(map(
                lambda operation :
                    self.__get_comment_by_id(operation['id']) if 'id' in operation else
                    self.__list_comments(operation),
                operations,
            ))
            self.send_json_response(results, self.__pretty_print, self.__allow_gzip, self.__gzip_level)
        else:
            self.send_response(http.HTTPStatus.NOT_FOUND)
            self.end_headers()

    def do_DELETE(self) -> None:
        if self.path != '/comment' and self.path[:9] != '/comment?':
            self.send_response(http.HTTPStatus.NOT_FOUND)
            self.end_headers()
            return

        db = self.__database_connection_factory()
        db.delete_all_comments()
        db.close()

        self.send_response(http.HTTPStatus.NO_CONTENT)
        self.end_headers()

    def __get_comment_by_id(self, comment_id: str) -> dict:
        db = self.__database_connection_factory()
        try:
            return self.__create_comment_from_data(db.get_comment(comment_id))
        finally:
            db.close()

    def __list_comments(self, parameters: dict) -> list:
        db = self.__database_connection_factory()
        try:
            after_cursor = CommentCursor(parameters.get('after'))
            comments = self.__get_comments_connection(db, parameters['location'], parameters['limit'], after_cursor)
            if 'replies1stLevelLimit' in parameters:
                comments['edges'] = list(map(
                    lambda edge : {
                        **edge,
                        'node': {
                            **edge['node'],
                            'replies': self.__get_comments_connection(
                                db,
                                parameters['location'],
                                parameters['replies1stLevelLimit'],
                                CommentCursor(parent_comment_id=edge['node']['id']),
                            ),
                        },
                    },
                    comments['edges'],
                ))
            if 'replies2ndLevelLimit' in parameters:
                comments['edges'] = list(map(
                    lambda top_edge : {
                        **top_edge,
                        'node': {
                            **top_edge['node'],
                            'replies': {
                                **top_edge['node']['replies'],
                                'edges': list(map(
                                    lambda edge_1st_reply_level : {
                                        **edge_1st_reply_level,
                                        'node': {
                                            **edge_1st_reply_level['node'],
                                            'replies': self.__get_comments_connection(
                                                db,
                                                parameters['location'],
                                                parameters['replies2ndLevelLimit'],
                                                CommentCursor(parent_comment_id=edge_1st_reply_level['node']['id']),
                                            ),
                                        },
                                    },
                                    top_edge['node']['replies']['edges'],
                                )),
                            },
                        },
                    },
                    comments['edges'],
                ))
            if 'replies3rdLevelLimit' in parameters:
                comments['edges'] = list(map(
                    lambda top_edge : {
                        **top_edge,
                        'node': {
                            **top_edge['node'],
                            'replies': {
                                **top_edge['node']['replies'],
                                'edges': list(map(
                                    lambda edge_1st_reply_level : {
                                        **edge_1st_reply_level,
                                        'node': {
                                            **edge_1st_reply_level['node'],
                                            'replies': {
                                                **edge_1st_reply_level['node']['replies'],
                                                'edges': list(map(
                                                    lambda edge_2nd_reply_level : {
                                                        **edge_2nd_reply_level,
                                                        'node': {
                                                            **edge_2nd_reply_level['node'],
                                                            'replies': self.__get_comments_connection(
                                                                db,
                                                                parameters['location'],
                                                                parameters['replies3rdLevelLimit'],
                                                                CommentCursor(
                                                                    parent_comment_id=
                                                                        edge_2nd_reply_level['node']['id'],
                                                                ),
                                                            )
                                                        },
                                                    },
                                                    edge_1st_reply_level['node']['replies']['edges'],
                                                )),
                                            },
                                        },
                                    },
                                    top_edge['node']['replies']['edges'],
                                )),
                            },
                        },
                    },
                    comments['edges'],
                ))
            return comments
        finally:
            db.close()

    def __get_comments_connection(self, db, webpage, first, after_cursor) -> dict:
        (comments, has_next_page) = self.__get_comments(db, webpage, first, after_cursor)
        edges = list(map(
            lambda comment: {
                'node': comment,
                'cursor': str(CommentCursor(parent_comment_id=after_cursor.parent_comment_id, after_id=comment['id'])),
            },
            comments,
        ))
        end_cursor = (
            str(CommentCursor(parent_comment_id=after_cursor.parent_comment_id, after_id=comments[-1:][0]['id']))
            if len(comments) > 0 else None
        )
        return {
            'pageInfo': {
                'hasNextPage': has_next_page,
                'endCursor': end_cursor,
            },
            'edges': edges,
        }

    def __get_comments(self, db, webpage, first, after_cursor):
        comments_data = db.get_comments(
            webpage,
            after_cursor.parent_comment_id,
            after_cursor.after_id,
            first + 1,
        )
        has_next_page = len(comments_data) == first + 1
        comments = list(map(
            lambda comment : self.__create_comment_from_data(comment),
            comments_data[:-1] if has_next_page else comments_data,
        ))
        return (comments, has_next_page)

    def __load_comment_listing_input(self, comments_location: str, query: dict) -> dict:
        parameters = {
            'location': comments_location,
            'limit': int(query['limit'][0]),
        }
        for optional_parameter in ['after', 'replies1stLevelLimit', 'replies2ndLevelLimit', 'replies3rdLevelLimit']:
            if optional_parameter in query:
                if optional_parameter == 'after':
                    parameters[optional_parameter] = query[optional_parameter][0]
                else:
                    parameters[optional_parameter] = int(query[optional_parameter][0])
        self.__validate_comment_listing_input(parameters)
        return parameters
    
    def __validate_comment_listing_input(self, parameters: dict) -> None:
        self.__validate_url(parameters['location'])
        for limit in ['limit', 'replies1stLevelLimit', 'replies2ndLevelLimit', 'replies3rdLevelLimit']:
            if limit in parameters:
                if type(parameters[limit]) is not int or parameters[limit] <= 0 or parameters[limit] > 2_147_483_647:
                    raise ValueError(
                        f'The {limit} parameter is invalid, expected positive 32-bit signed int, got ' +
                        parameters[limit],
                    )
        if 'after' in parameters:
            CommentCursor(parameters['after'])

    def __create_comment_from_data(self, comment_db_data: dict) -> dict:
        if comment_db_data is None:
            return None
        comment = comment_db_data.copy()
        comment['id'] = str(comment['id'])
        if comment['parent'] is None:
            del comment['parent']
        else:
            comment['parent'] = {'id': str(comment['parent'])}
        comment['created'] = int(comment['created'].timestamp() * 1_000)
        comment['repliesStartCursor'] = str(CommentCursor(parent_comment_id=comment['id']))
        return comment

    def __validate_url(self, url: str) -> None:
        [scheme, netloc, *_] = urlsplit(url)
        if scheme != 'http' and scheme != 'https':
            raise ValueError('The provided URL is not and HTTP(S) URL')
        if netloc == '':
            raise ValueError('The provided URL is invalid')
