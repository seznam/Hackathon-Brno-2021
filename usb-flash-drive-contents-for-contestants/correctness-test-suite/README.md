# Correctness test suite

Runs a unit test suite against comments API implementation (usually deployed on localhost).

## Usage

### Binary

```sh
./correctness-test-suite_<os>-<arch> <graphql|rest> <api_url> [-nogzip] [-nodefops]
```

Example:

```sh
./correctness-test-suite_linux-amd64 graphql http://localhost:8000 -nogzip -nodefops
```

### Docker image

First, load the image into local docker's image storage:

```sh
docker load -i correctness-test-suite.v0.0.10.tar
```

Run the tests using the following command:

```sh
docker run --rm -it correctness-test-suite:v0.0.10 <graphql|rest> <api_url> [-nogzip] [-nodefops]
```

Example (you need to use host's IP address, because `localhost` will point to the docker container itself):

```sh
docker run --rm -it correctness-test-suite:v0.0.10 graphql http://10.0.6.1:8000 -nogzip -nodefops
```

