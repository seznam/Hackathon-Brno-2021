FROM python:3 as build

WORKDIR /www
COPY requirements.txt /www/

RUN pip install --no-cache-dir -r requirements.txt

COPY *.py operations.graphql /www/
COPY graphiql/ /www/graphiql/
RUN python3 -m compileall .



FROM python:3-alpine

EXPOSE 8000
WORKDIR /www
CMD [ "python", "server.py" ]
ENV PORT=8000 \
    API_TYPE=GraphQL \
    DB_STORAGE_PATH=./db/comments.sqlite \
    DB_CACHED_STATEMENTS=0 \
    DB_ENABLE_AUTO_VACUUM=true \
    JSON_PRETTY_PRINT=true \
    ENABLE_OUTPUT_GZIP_COMPRESSION=false \
    GZIP_COMPRESSION_LEVEL=0

COPY --from=build /www/ /www/

RUN pip install --no-cache-dir -r requirements.txt \
    && mkdir /www/db/ \
    && chmod ugo+rwx /www/db/

USER nobody
