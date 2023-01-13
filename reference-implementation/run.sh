#!/bin/sh

docker run --rm -p8000:8000 --env-file env/performance-level-3 --volume="`pwd`/db:/www/db" -it reference-implementation-1
