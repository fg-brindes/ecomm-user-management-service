#!/bin/bash

# User Management Service - Create ECS Service Script
# This script creates the initial ECS service, target group, and ALB listener

set -e

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configuration
AWS_REGION="${AWS_REGION:-us-east-1}"
AWS_ACCOUNT_ID="${AWS_ACCOUNT_ID}"
ECS_CLUSTER="${ECS_CLUSTER:-fg-cluster-ecs}"
SERVICE_NAME="${SERVICE_NAME:-ecomm-user-management-service}"
VPC_ID="${VPC_ID}"
SUBNET_IDS="${SUBNET_IDS}" # Comma-separated
SECURITY_GROUP_ID="${SECURITY_GROUP_ID}"

# Validate required variables
if [ -z "$AWS_ACCOUNT_ID" ] || [ -z "$VPC_ID" ] || [ -z "$SUBNET_IDS" ] || [ -z "$SECURITY_GROUP_ID" ]; then
    echo -e "${RED}Error: Missing required environment variables${NC}"
    echo "Required variables:"
    echo "  AWS_ACCOUNT_ID"
    echo "  VPC_ID"
    echo "  SUBNET_IDS (comma-separated)"
    echo "  SECURITY_GROUP_ID"
    exit 1
fi

echo -e "${GREEN}Creating ECS Service for User Management API...${NC}"
echo ""

# Step 1: Create CloudWatch Log Group
echo -e "${YELLOW}Step 1: Creating CloudWatch Log Group...${NC}"
aws logs create-log-group \
    --log-group-name /ecs/ecomm-user-management-service \
    --region $AWS_REGION 2>/dev/null || echo "Log group already exists"

echo -e "${GREEN}Log group ready${NC}"
echo ""

# Step 2: Create Target Group
echo -e "${YELLOW}Step 2: Creating Target Group...${NC}"
TARGET_GROUP_ARN=$(aws elbv2 create-target-group \
    --name user-management-tg \
    --protocol HTTP \
    --port 8080 \
    --vpc-id $VPC_ID \
    --target-type ip \
    --health-check-enabled \
    --health-check-path /health \
    --health-check-interval-seconds 30 \
    --health-check-timeout-seconds 5 \
    --healthy-threshold-count 2 \
    --unhealthy-threshold-count 3 \
    --matcher HttpCode=200 \
    --region $AWS_REGION \
    --query 'TargetGroups[0].TargetGroupArn' \
    --output text 2>/dev/null || \
aws elbv2 describe-target-groups \
    --names user-management-tg \
    --region $AWS_REGION \
    --query 'TargetGroups[0].TargetGroupArn' \
    --output text)

echo -e "${GREEN}Target Group ARN: $TARGET_GROUP_ARN${NC}"
echo ""

# Step 3: Get or Create ALB
echo -e "${YELLOW}Step 3: Setting up Application Load Balancer...${NC}"
ALB_ARN=$(aws elbv2 describe-load-balancers \
    --names user-management-alb \
    --region $AWS_REGION \
    --query 'LoadBalancers[0].LoadBalancerArn' \
    --output text 2>/dev/null || echo "")

if [ -z "$ALB_ARN" ] || [ "$ALB_ARN" == "None" ]; then
    echo "Creating new ALB..."
    IFS=',' read -ra SUBNET_ARRAY <<< "$SUBNET_IDS"
    ALB_ARN=$(aws elbv2 create-load-balancer \
        --name user-management-alb \
        --subnets ${SUBNET_ARRAY[@]} \
        --security-groups $SECURITY_GROUP_ID \
        --scheme internet-facing \
        --type application \
        --ip-address-type ipv4 \
        --region $AWS_REGION \
        --query 'LoadBalancers[0].LoadBalancerArn' \
        --output text)
    echo "Waiting for ALB to become active..."
    aws elbv2 wait load-balancer-available --load-balancer-arns $ALB_ARN --region $AWS_REGION
fi

ALB_DNS=$(aws elbv2 describe-load-balancers \
    --load-balancer-arns $ALB_ARN \
    --region $AWS_REGION \
    --query 'LoadBalancers[0].DNSName' \
    --output text)

echo -e "${GREEN}ALB DNS: $ALB_DNS${NC}"
echo ""

# Step 4: Create ALB Listener
echo -e "${YELLOW}Step 4: Creating ALB Listener...${NC}"
LISTENER_ARN=$(aws elbv2 create-listener \
    --load-balancer-arn $ALB_ARN \
    --protocol HTTP \
    --port 80 \
    --default-actions Type=forward,TargetGroupArn=$TARGET_GROUP_ARN \
    --region $AWS_REGION \
    --query 'Listeners[0].ListenerArn' \
    --output text 2>/dev/null || \
aws elbv2 describe-listeners \
    --load-balancer-arn $ALB_ARN \
    --region $AWS_REGION \
    --query 'Listeners[0].ListenerArn' \
    --output text)

echo -e "${GREEN}Listener created${NC}"
echo ""

# Step 5: Register Task Definition
echo -e "${YELLOW}Step 5: Registering Task Definition...${NC}"

# Update task definition with account ID
sed "s/AWS_ACCOUNT_ID/$AWS_ACCOUNT_ID/g" task-definition.json > task-definition-temp.json

aws ecs register-task-definition \
    --cli-input-json file://task-definition-temp.json \
    --region $AWS_REGION

rm task-definition-temp.json

echo -e "${GREEN}Task definition registered${NC}"
echo ""

# Step 6: Create ECS Service
echo -e "${YELLOW}Step 6: Creating ECS Service...${NC}"

IFS=',' read -ra SUBNET_ARRAY <<< "$SUBNET_IDS"
SUBNET_JSON=$(printf '"%s",' "${SUBNET_ARRAY[@]}")
SUBNET_JSON="[${SUBNET_JSON%,}]"

aws ecs create-service \
    --cluster $ECS_CLUSTER \
    --service-name $SERVICE_NAME \
    --task-definition ecomm-user-management-service-task \
    --desired-count 1 \
    --launch-type FARGATE \
    --platform-version LATEST \
    --network-configuration "awsvpcConfiguration={subnets=$SUBNET_JSON,securityGroups=[$SECURITY_GROUP_ID],assignPublicIp=ENABLED}" \
    --load-balancers "targetGroupArn=$TARGET_GROUP_ARN,containerName=user-management-api,containerPort=8080" \
    --health-check-grace-period-seconds 120 \
    --deployment-configuration "maximumPercent=200,minimumHealthyPercent=100,deploymentCircuitBreaker={enable=true,rollback=true}" \
    --region $AWS_REGION

echo -e "${GREEN}ECS Service created${NC}"
echo ""

# Step 7: Wait for service to stabilize
echo -e "${YELLOW}Step 7: Waiting for service to become stable...${NC}"
aws ecs wait services-stable \
    --cluster $ECS_CLUSTER \
    --services $SERVICE_NAME \
    --region $AWS_REGION

echo -e "${GREEN}Service is stable${NC}"
echo ""

echo -e "${GREEN}=== Service Creation Complete ===${NC}"
echo "Cluster: $ECS_CLUSTER"
echo "Service: $SERVICE_NAME"
echo "ALB DNS: $ALB_DNS"
echo "Target Group: $TARGET_GROUP_ARN"
echo ""
echo -e "${GREEN}Your API is available at:${NC}"
echo "http://$ALB_DNS/api/users"
echo "http://$ALB_DNS/health"
echo ""
echo -e "${YELLOW}Note: Update your DNS records to point to the ALB${NC}"
