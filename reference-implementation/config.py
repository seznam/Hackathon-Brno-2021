import os
import logging

from collections import namedtuple


def _parse_bool(value):
    return value if type(value) is bool else value.lower() in ['true', 'yes', 'y', '1']


ConfigVariable = namedtuple('ConfigVariable', ['name', 'default_value', 'conversion_function'])
default_config = [
    ConfigVariable('PORT', 8000, int),
    ConfigVariable('DB_STORAGE_PATH', './db/comments.sqlite', str),
    ConfigVariable('DB_CACHED_STATEMENTS', 0, int),
    ConfigVariable('DB_ENABLE_AUTO_VACUUM', True, _parse_bool),
    ConfigVariable('JSON_PRETTY_PRINT', True, _parse_bool),
    ConfigVariable('ENABLE_OUTPUT_GZIP_COMPRESSION', False, _parse_bool)
]

config = {}
for variable in default_config:
    try:
        config[variable.name] = variable.conversion_function(os.environ.get(variable.name, variable.default_value))
    except Exception:
        logging.error('Error while parsing variable <%s>, using default value <%s>', variable.name, variable.default_value)
        config[variable.name] = variable.default_value
