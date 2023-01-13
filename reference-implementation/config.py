import os
import logging

from collections import namedtuple


def _parse_api_type(value):
    if value in ['GraphQL', 'REST']:
        return value
    raise ValueError('The API type must be either "GraphQL" or "REST"')

def _parse_bool(value):
    return value if type(value) is bool else value.lower() in ['true', 'yes', 'y', '1']

def _parse_gzip_compression_level(value):
    intVal = int(value)
    if intVal < 0 or intVal > 9:
        raise TypeError('The compression level must be within range 0-9 (inclusive).')
    return intVal

ConfigVariable = namedtuple('ConfigVariable', ['name', 'default_value', 'conversion_function'])
default_config = [
    ConfigVariable('PORT', 8000, int),
    ConfigVariable('API_TYPE', 'GraphQL', _parse_api_type),
    ConfigVariable('DB_STORAGE_PATH', './db/comments.sqlite', str),
    ConfigVariable('DB_CACHED_STATEMENTS', 0, int),
    ConfigVariable('DB_ENABLE_AUTO_VACUUM', True, _parse_bool),
    ConfigVariable('JSON_PRETTY_PRINT', True, _parse_bool),
    ConfigVariable('ENABLE_OUTPUT_GZIP_COMPRESSION', False, _parse_bool),
    ConfigVariable('GZIP_COMPRESSION_LEVEL', 0, _parse_gzip_compression_level),
]

config = {}
for variable in default_config:
    try:
        config[variable.name] = variable.conversion_function(os.environ.get(variable.name, variable.default_value))
    except Exception as e:
        logging.error(
            'Error while parsing variable <%s>, using default value <%s>: %s',
            variable.name,
            variable.default_value,
            e,
        )
        config[variable.name] = variable.default_value
