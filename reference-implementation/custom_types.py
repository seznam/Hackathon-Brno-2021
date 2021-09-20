import base64
import datetime
import math
from urllib.parse import urlparse
from graphene.types import (
    Boolean,
    Field,
    ID,
    Interface,
    ObjectType,
    Scalar,
    String
)
from graphql.language import ast


class MillisecondUnixTimestamp(Scalar):
    '''Int representing a UNIX timestamp with millisecond precision.'''

    @staticmethod
    def serialize(datetime):
        return math.floor(datetime.timestamp() * 1_000)

    @staticmethod
    def parse_literal(node):
        if isinstance(node, ast.IntValue):
            return MillisecondUnixTimestamp.parse_value(node.value)

    @staticmethod
    def parse_value(value):
        return datetime.datetime.fromtimestamp(value / 1_000)


class URL(Scalar):
    '''
        String containing a valid URL to a website (with either https or http
        protocol).
    '''

    @staticmethod
    def serialize(urlstring):
        return urlstring

    @staticmethod
    def parse_literal(node):
        if isinstance(node, ast.StringValue):
            return URL.parse_value(node.value)

    @staticmethod
    def parse_value(value):
        parts = urlparse(value)
        if parts[0] != 'https' and parts[0] != 'http':
            raise TypeError(
                f'The provided URL "{value}" does not use the https or http ' +
                'schema',
            )
        return str(value)


class Cursor(Scalar):
    '''
        String containing information needed for pagination. This is usually
        some info encoded using base64, but the server can use anything it
        needs or prefers.
    '''

    @staticmethod
    def serialize(cursor):
        return base64.standard_b64encode(cursor.encode()).decode('utf-8')

    @staticmethod
    def parse_literal(node):
        if isinstance(node, ast.StringValue):
            return Cursor.parse_value(node.value)

    @staticmethod
    def parse_value(value):
        return base64.standard_b64decode(value.encode()).decode('utf-8')


class Node(Interface):
    '''
        An object with an ID unique across all records available via this API.
        UUID (v1, v2 or v5) is recommended.
    '''

    id = ID(
        required=True,
        description='The universally unique identifier of this record.'
    )


class PageInfo(ObjectType):
    '''Pagination-related information.'''

    has_next_page = Boolean(
        required=True,
        description='''
            True iff there is another page of records available at the moment
            the information was retrieved. The endCursor must be non-null if
            this flag is true.
        '''
    )
    end_cursor = Field(
        Cursor,
        description='''
            The cursor pointing to the end of the current page of records, used
            to access the next page.
        '''
    )


class Connection(Interface):
    '''Properties shared by all record connections.'''

    page_info = Field(
        PageInfo,
        required=True,
        description='Pagination-related information.'
    )


class Edge(Interface):
    '''Properties shared by all connection edges in the graph of records.'''

    cursor = Field(
        Cursor,
        required=True,
        description='''
            Cursor pointing to this edge in the current pagination context.
            Used to retrieve records immediately following this one. The API
            must preserve the current records ordering when requesting
            following records using this cursor.
        '''
    )


class CommentData(Interface):
    '''Common data related to a single comment.'''

    author = ID(
        required=True,
        description='''
            Unique identifier of the comment's author. This can be any
            non-empty string of up to 36 bytes (UUID textual representation).
        '''
    )

    text = String(
        required=True,
        description='''
            Comment's actual content. The content must be at least 5 bytes
            long, may contain any valid Unicode characters and can be up to 128
            KiB long.
        '''
    )
