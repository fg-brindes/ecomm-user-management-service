# GitHub Actions Workflows

This directory contains CI/CD workflows for the User Management Service.

## deploy-to-aws.yml

Automated deployment workflow that builds, tests, and deploys the application to AWS ECS.

### Triggers

- **Automatic**: Pushes to `main` branch
- **Manual**: Workflow dispatch (can select environment)

### Prerequisites

**Before running this workflow for the first time**, you must:

1. **Create AWS Infrastructure** using the setup script:
   ```bash
   cd aws
   ./create-service.sh
   ```
   This creates:
   - ECS Cluster (`fg-cluster-ecs`)
   - Task Definition (`ecomm-user-management-service-task`)
   - ECS Service (`ecomm-user-management-service`)
   - Application Load Balancer
   - Target Groups
   - CloudWatch Log Groups

2. **Configure GitHub Secrets** in your repository:
   - `AWS_ACCESS_KEY_ID` - AWS access key with ECS/ECR permissions
   - `AWS_SECRET_ACCESS_KEY` - AWS secret key
   - `AWS_REGION` - (Optional) AWS region, defaults to `us-east-1`

### Workflow Steps

1. **Checkout code** - Clones the repository
2. **Configure AWS credentials** - Sets up AWS CLI access
3. **Login to Amazon ECR** - Authenticates to container registry
4. **Create ECR repository if it doesn't exist** - Auto-creates ECR repo if missing
5. **Extract git metadata** - Gets commit SHA, branch, timestamp
6. **Build Docker image** - Creates production container image
7. **Scan image for vulnerabilities** - Trivy security scan (CRITICAL/HIGH)
8. **Push image to Amazon ECR** - Uploads with SHA and `latest` tags
9. **Check if ECS infrastructure exists** - Validates all AWS resources exist
10. **Download task definition** - Gets current ECS task configuration
11. **Update task definition with new image** - Injects new Docker image reference
12. **Deploy to Amazon ECS** - Deploys with rollback on failure enabled
13. **Verify deployment** - Health check with retries (5 attempts)
14. **Create deployment summary** - GitHub Actions summary with details
15. **Notify deployment status** - Alerts on failure

### Common Issues and Solutions

#### ❌ "The repository with name 'ecomm-user-management-service' does not exist in the registry"

**Cause**: ECR repository doesn't exist in your AWS account.

**Solution**: The workflow now automatically creates the ECR repository if it doesn't exist. This step runs after ECR login and before building the Docker image.

If you prefer to create it manually:
```bash
aws ecr create-repository \
  --repository-name ecomm-user-management-service \
  --region us-east-1 \
  --image-scanning-configuration scanOnPush=true \
  --encryption-configuration encryptionType=AES256
```

**Note**: This is now handled automatically by the workflow, so you don't need to worry about it.

#### ❌ "ECS Cluster not found or not active"

**Cause**: AWS infrastructure hasn't been created yet.

**Solution**: Run the infrastructure creation script:
```bash
cd aws

# Set required environment variables
export AWS_ACCOUNT_ID="your-aws-account-id"
export VPC_ID="vpc-xxxxxxxxx"
export SUBNET_IDS="subnet-xxx,subnet-yyy"
export SECURITY_GROUP_ID="sg-xxxxxxxxx"

# Create infrastructure
./create-service.sh
```

#### ❌ "Task definition not found"

**Cause**: ECS task definition doesn't exist in AWS.

**Solution**: Same as above - run `./create-service.sh` to create all infrastructure.

#### ❌ "Error downloading task definition - Invalid query"

**Cause**: The `aws ecs describe-task-definition` command returns extra metadata fields that are incompatible with the `amazon-ecs-render-task-definition` action.

**Solution**: This has been fixed in the workflow by filtering only required fields:
```yaml
--query 'taskDefinition | {family: family, networkMode: networkMode, ...}'
```

#### ❌ "ALB not found - cannot verify health"

**Cause**: Application Load Balancer doesn't exist (common on first run before infrastructure is created).

**Solution**: The workflow now handles this gracefully:
- If ALB doesn't exist, it skips health check verification
- Logs a warning message
- Doesn't fail the deployment

This is expected behavior if you haven't run `create-service.sh` yet.

#### ⚠️ "Health check did not return 200 after 5 attempts"

**Cause**: Service may still be starting up, or there's an application error.

**Solution**:
1. Check CloudWatch Logs:
   ```bash
   aws logs tail /ecs/ecomm-user-management-service --follow
   ```

2. Check ECS Console for task status:
   ```bash
   aws ecs describe-services \
     --cluster fg-cluster-ecs \
     --services ecomm-user-management-service
   ```

3. Verify database connection:
   - Ensure `ConnectionStrings__DefaultConnection` secret exists in AWS Secrets Manager
   - Verify RDS security group allows connections from ECS tasks

4. Check target group health:
   ```bash
   aws elbv2 describe-target-health \
     --target-group-arn $(aws elbv2 describe-target-groups \
       --names user-management-tg \
       --query 'TargetGroups[0].TargetGroupArn' \
       --output text)
   ```

#### ❌ "Access Denied" errors

**Cause**: GitHub Actions AWS credentials don't have sufficient permissions.

**Solution**: Ensure the IAM user has the following permissions:
- `ecr:*` - ECR access
- `ecs:*` - ECS full access
- `elbv2:Describe*` - Load balancer read access
- `iam:PassRole` - For task/execution roles
- `logs:*` - CloudWatch Logs

Sample IAM policy:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "ecr:GetAuthorizationToken",
        "ecr:BatchCheckLayerAvailability",
        "ecr:GetDownloadUrlForLayer",
        "ecr:PutImage",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecs:*",
        "elbv2:Describe*",
        "logs:*",
        "iam:PassRole"
      ],
      "Resource": "*"
    }
  ]
}
```

### Manual Workflow Dispatch

To trigger a deployment manually:

1. Go to **Actions** tab in GitHub
2. Select **Deploy to AWS ECS** workflow
3. Click **Run workflow**
4. Select environment (production/staging)
5. Click **Run workflow**

### Viewing Deployment Logs

**In GitHub**:
- Go to Actions → Select the workflow run
- View each step's output

**In AWS CloudWatch**:
```bash
# Tail logs in real-time
aws logs tail /ecs/ecomm-user-management-service --follow

# View recent logs
aws logs tail /ecs/ecomm-user-management-service --since 1h
```

### Rollback

The workflow has **automatic rollback** enabled via ECS deployment circuit breaker.

**Manual rollback**:
```bash
# List recent task definitions
aws ecs list-task-definitions \
  --family-prefix ecomm-user-management-service-task \
  --sort DESC

# Update service to previous version
aws ecs update-service \
  --cluster fg-cluster-ecs \
  --service ecomm-user-management-service \
  --task-definition ecomm-user-management-service-task:PREVIOUS_REVISION
```

### Monitoring Deployments

**ECS Service Status**:
```bash
aws ecs describe-services \
  --cluster fg-cluster-ecs \
  --services ecomm-user-management-service \
  --query 'services[0].{Status:status,Running:runningCount,Desired:desiredCount,Deployments:deployments[*].{Status:status,Count:runningCount,Image:taskDefinition}}'
```

**Recent Deployments**:
```bash
aws ecs list-tasks \
  --cluster fg-cluster-ecs \
  --service-name ecomm-user-management-service
```

### Security Scanning

The workflow uses **Trivy** to scan Docker images for vulnerabilities.

- **Scan level**: CRITICAL and HIGH severity
- **Exit code**: 0 (informational, doesn't fail build)
- **Output**: Table format in workflow logs

To scan locally:
```bash
docker build -t user-management-api:test .
trivy image user-management-api:test --severity CRITICAL,HIGH
```

### Performance Optimization

The workflow is optimized for speed:
- **Multi-stage Docker build** reduces final image size
- **Layer caching** in ECR for faster builds
- **Parallel security scanning** doesn't block push
- **Service stability check** ensures deployment completes

Average deployment time: **5-8 minutes**

### Support

For issues with the workflow:
1. Check the "Common Issues" section above
2. Review CloudWatch logs for application errors
3. Check ECS console for service status
4. Open an issue in the repository
