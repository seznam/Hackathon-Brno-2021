#!/bin/bash


echo "Restoring packages";
dotnet restore;
echo "Building the project";
dotnet build -c Release --no-restore;
echo "Running the project";
dotnet publish -c Release --no-restore --no-build;
echo "Run";
dotnet ./bin/Release/net6.0/publish/CommentApi.dll --urls=http://0.0.0.0:8000
