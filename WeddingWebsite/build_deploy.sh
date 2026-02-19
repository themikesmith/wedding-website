#!/usr/bin/env bash

sudo docker build -t weddingwebsite .

sudo docker tag weddingwebsite registry.digitalocean.com/octopus-containers/weddingwebsite

sudo docker push registry.digitalocean.com/octopus-containers/weddingwebsite


