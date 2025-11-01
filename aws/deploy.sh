#!/bin/bash

# User Management Service Deployment Script
# This script builds, tags, pushes Docker image to ECR and updates ECS service

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
AWS_REGION="${AWS_REGION:-us-east-1}"
AWS_ACCOUNT_ID="${AWS_ACCOUNT_ID}"
ECR_REPOSITORY="${ECR_REPOSITORY:-ecomm-user-management-service}"
ECS_CLUSTER="${ECS_CLUSTER:-fg-cluster-ecs}"
ECS_SERVICE="${ECS_SERVICE:-ecomm-user-management-service}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

# Validate required environment variables
if [ -z "$AWS_ACCOUNT_ID" ]; then
    echo -e "${RED}Error: AWS_ACCOUNT_ID environment variable is not set${NC}"
    echo "Usage: AWS_ACCOUNT_ID=123456789012 ./deploy.sh"
    exit 1
fi

echo -e "${GREEN}Starting deployment for User Management Service...${NC}"
echo "Region: $AWS_REGION"
echo "Account: $AWS_ACCOUNT_ID"
echo "ECR Repository: $ECR_REPOSITORY"
echo "ECS Cluster: $ECS_CLUSTER"
echo "ECS Service: $ECS_SERVICE"
echo ""

# Step 1: Build Docker image
echo -e "${YELLOW}Step 1: Building Docker image...${NC}"
docker build -t $ECR_REPOSITORY:$IMAGE_TAG -f ../Dockerfile ..

if [ $? -ne 0 ]; then
    echo -e "${RED}Error: Docker build failed${NC}"
    exit 1
fi

echo -e "${GREEN}Docker image built successfully${NC}"
echo ""

# Step 2: Login to ECR
echo -e "${YELLOW}Step 2: Logging in to Amazon ECR...${NC}"
aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com

if [ $? -ne 0 ]; then
    echo -e "${RED}Error: ECR login failed${NC}"
    exit 1
fi

echo -e "${GREEN}Successfully logged in to ECR${NC}"
echo ""

# Step 3: Tag image for ECR
echo -e "${YELLOW}Step 3: Tagging Docker image for ECR...${NC}"
docker tag $ECR_REPOSITORY:$IMAGE_TAG $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$IMAGE_TAG
docker tag $ECR_REPOSITORY:$IMAGE_TAG $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$(git rev-parse --short HEAD)

echo -e "${GREEN}Image tagged successfully${NC}"
echo ""

# Step 4: Push image to ECR
echo -e "${YELLOW}Step 4: Pushing image to ECR...${NC}"
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$IMAGE_TAG
docker push $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$(git rev-parse --short HEAD)

if [ $? -ne 0 ]; then
    echo -e "${RED}Error: Docker push failed${NC}"
    exit 1
fi

echo -e "${GREEN}Image pushed to ECR successfully${NC}"
echo ""

# Step 5: Update ECS service
echo -e "${YELLOW}Step 5: Updating ECS service...${NC}"
aws ecs update-service \
    --cluster $ECS_CLUSTER \
    --service $ECS_SERVICE \
    --force-new-deployment \
    --region $AWS_REGION

if [ $? -ne 0 ]; then
    echo -e "${RED}Error: ECS service update failed${NC}"
    exit 1
fi

echo -e "${GREEN}ECS service update initiated${NC}"
echo ""

# Step 6: Wait for deployment to complete
echo -e "${YELLOW}Step 6: Waiting for deployment to stabilize...${NC}"
aws ecs wait services-stable \
    --cluster $ECS_CLUSTER \
    --services $ECS_SERVICE \
    --region $AWS_REGION

if [ $? -ne 0 ]; then
    echo -e "${RED}Warning: Deployment did not stabilize within expected time${NC}"
    echo "Check ECS console for deployment status"
else
    echo -e "${GREEN}Deployment completed successfully!${NC}"
fi

echo ""
echo -e "${GREEN}=== Deployment Summary ===${NC}"
echo "Image: $AWS_ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$IMAGE_TAG"
echo "Cluster: $ECS_CLUSTER"
echo "Service: $ECS_SERVICE"
echo "Git Commit: $(git rev-parse --short HEAD)"
echo ""
echo -e "${GREEN}Check service status with:${NC}"
echo "aws ecs describe-services --cluster $ECS_CLUSTER --services $ECS_SERVICE --region $AWS_REGION"
