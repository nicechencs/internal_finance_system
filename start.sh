#!/bin/bash

echo "Starting Finance System..."

# Check if .env exists
if [ ! -f .env ]; then
    echo "Creating .env from .env.example..."
    cp .env.example .env
    echo "Please edit .env file and set your passwords and secrets!"
    exit 1
fi

# Build and start services
docker-compose up -d --build

echo "Waiting for services to be ready..."
sleep 10

echo "Services started successfully!"
echo "Frontend: http://localhost"
echo "API: http://localhost:5000"
echo "Swagger: http://localhost:5000/swagger"
