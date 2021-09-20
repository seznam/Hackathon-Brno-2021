docker build -f api.Dockerfile -t api .
docker save api:latest | sshpass -p hackathon ssh hackathon@10.6.32.113 "docker load"
docker image rm api:latest

sshpass -p hackathon ssh hackathon@10.6.32.113 "docker-compose down"
cat docker-compose.yml | sshpass -p hackathon ssh hackathon@10.6.32.113 "cat - > docker-compose.yml"
sshpass -p hackathon ssh hackathon@10.6.32.113 "docker-compose up -d"

