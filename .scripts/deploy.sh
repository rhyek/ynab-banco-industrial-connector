#!/bin/bash
set -e

# build docker image and publish to ecr
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 875667244027.dkr.ecr.us-east-1.amazonaws.com
docker build \
  -t 875667244027.dkr.ecr.us-east-1.amazonaws.com/ynab-banco-industrial-connector:latest \
  -f src/YnabBancoIndustrialConnectorBackend/Programs/HttpApi/Dockerfile \
  src/YnabBancoIndustrialConnectorBackend
docker push 875667244027.dkr.ecr.us-east-1.amazonaws.com/ynab-banco-industrial-connector:latest

