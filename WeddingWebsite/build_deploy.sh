#!/usr/bin/env bash

# Argument count check
if [ "$#" -gt 3 ]; then
	echo "Error: Too many arguments. Usage: $0 [start] [keepon] [clean]"
	exit 1
fi

# Check for 'start' in any argument
if [[ "$1" == "start" || "$2" == "start" || "$3" == "start" ]]; then
	sudo systemctl start docker
fi

if [[ "$1" == "clean" || "$2" == "clean" || "$3" == "clean" ]]; then
    sudo docker rmi -f $(sudo docker images --filter "dangling=true" -q --no-trunc)
fi

sudo docker build -t weddingwebsite .

sudo docker tag weddingwebsite registry.digitalocean.com/octopus-containers/weddingwebsite

sudo docker push registry.digitalocean.com/octopus-containers/weddingwebsite

# Check for 'keepon' in any argument
if [[ "$1" == "keepon" || "$2" == "keepon" || "$3" == "keepon" ]]; then
    :  
else
    sudo systemctl stop docker
fi
