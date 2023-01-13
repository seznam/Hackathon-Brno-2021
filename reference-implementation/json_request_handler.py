import gzip
import http
import http.server
import io
import json
import re

class JsonRequestHandler(http.server.SimpleHTTPRequestHandler):
    def read_json_request_body(self):
        content_length = self.headers.get('Content-Length')
        encoded_body = self.rfile.read(int(content_length))
        if self.headers.get('Content-Encoding') == 'gzip':
            encoded_body = gzip.decompress(encoded_body)
        return json.loads(encoded_body)

    def send_json_response(self, body, pretty_print, allow_gzip, gzip_level, status=http.HTTPStatus.OK):
        if pretty_print:
            serialized_result = json.dumps(body, sort_keys=True, indent=4)
        else:
            serialized_result = json.dumps(body)
        encoded_result = serialized_result.encode(errors='surrogateescape')
        gzipped_result = allow_gzip and self.__accepts_gzip()
        if gzipped_result:
            encoded_result = gzip.compress(encoded_result, gzip_level)

        self.__write_response(encoded_result, gzipped_result, status)

    def __write_response(self, body, is_gzipped, status):
        response_body = io.BytesIO()
        response_body.write(body)
        response_body.seek(0)

        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header("Content-Length", str(len(body)))
        if is_gzipped:
            self.send_header('Content-Encoding', 'gzip')
        self.end_headers()

        self.copyfile(response_body, self.wfile)
        response_body.close()

    def __accepts_gzip(self):
        accepted_encodings = re.split(
            r'(?:;q=\d+(?:\.\d+)?)?\s*,\s*',
            self.headers.get('Accept-Encoding', ''),
        )
        return 'gzip' in accepted_encodings
