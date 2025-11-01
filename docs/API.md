# API Documentation

This document provides comprehensive documentation for all API endpoints in the User Management Service.

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Error Handling](#error-handling)
- [Pagination](#pagination)
- [API Endpoints](#api-endpoints)
  - [Users](#users)
  - [Companies](#companies)
  - [Commercial Conditions](#commercial-conditions)
  - [Integration](#integration)
  - [Health](#health)

## Overview

The User Management API is a RESTful service that provides endpoints for managing users, companies, and commercial conditions. All endpoints accept and return JSON unless otherwise specified.

**Base URL**: `http://localhost:8080/api` (development) or your production domain

**API Version**: v1

**Content Type**: `application/json`

## Authentication

**Note**: Authentication is currently not implemented in this version of the API. All endpoints are publicly accessible. Authentication will be added in a future release using JWT tokens.

### Planned Authentication (Future)

```
Authorization: Bearer <jwt-token>
```

## Error Handling

### Error Response Format

All errors follow a consistent format:

```json
{
  "error": "Error message describing what went wrong"
}
```

### HTTP Status Codes

| Status Code | Description |
|------------|-------------|
| `200 OK` | Request successful |
| `201 Created` | Resource created successfully |
| `204 No Content` | Request successful, no content to return |
| `400 Bad Request` | Invalid request data or business rule violation |
| `404 Not Found` | Requested resource not found |
| `500 Internal Server Error` | Server error (logged for investigation) |

### Common Error Scenarios

#### Resource Not Found
```json
{
  "error": "User with ID 550e8400-e29b-41d4-a716-446655440000 not found."
}
```

#### Validation Error
```json
{
  "error": "Page and pageSize must be greater than 0."
}
```

#### Business Rule Violation
```json
{
  "error": "Email already exists in the system."
}
```

## Pagination

Endpoints that return lists support pagination through query parameters.

### Pagination Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | integer | 1 | Page number to retrieve (1-based) |
| `pageSize` | integer | 10 | Number of items per page |

### Pagination Response Format

```json
{
  "data": [...],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 45,
    "totalPages": 5,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

### Example Request

```http
GET /api/users?page=2&pageSize=20
```

## API Endpoints

---

## Users

Endpoints for managing user accounts and profiles.

### List Users

Retrieve a paginated list of all users.

```http
GET /api/users
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | integer | No | 1 | Page number |
| pageSize | integer | No | 10 | Items per page |

#### Response

**Status**: `200 OK`

```json
{
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "name": "John Doe",
      "email": "john.doe@example.com",
      "userType": "Internal",
      "role": "Customer",
      "isActive": true
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 25,
    "totalPages": 3,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

### Get User by ID

Retrieve detailed information about a specific user.

```http
GET /api/users/{id}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | User unique identifier |

#### Response

**Status**: `200 OK`

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "John Doe",
  "email": "john.doe@example.com",
  "phone": "+5511999999999",
  "document": "12345678901",
  "userType": "Internal",
  "role": "Customer",
  "isActive": true,
  "emailVerified": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-20T14:45:00Z",
  "addresses": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440000",
      "type": "Billing",
      "postalCode": "01234567",
      "street": "Main Street",
      "number": "123",
      "complement": "Apt 45",
      "neighborhood": "Downtown",
      "city": "São Paulo",
      "state": "SP",
      "isDefault": true,
      "isActive": true
    }
  ],
  "companies": [
    {
      "companyId": "770e8400-e29b-41d4-a716-446655440000",
      "companyName": "ABC Corporation",
      "isActive": true
    }
  ]
}
```

---

### Get User by Email

Retrieve a user by their email address.

```http
GET /api/users/email/{email}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| email | string | Yes | User email address |

#### Response

**Status**: `200 OK` - Same structure as Get User by ID

---

### Create User

Create a new user account.

```http
POST /api/users
```

#### Request Body

```json
{
  "email": "user@example.com",
  "name": "Jane Smith",
  "phone": "+5511888888888",
  "document": "98765432100",
  "userType": "SelfRegistered",
  "role": "Customer",
  "addresses": [
    {
      "type": "Both",
      "postalCode": "01234567",
      "street": "Main Street",
      "number": "456",
      "complement": "Suite 10",
      "neighborhood": "Centro",
      "city": "São Paulo",
      "state": "SP",
      "isDefault": true
    }
  ]
}
```

#### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| email | string | Yes | Unique email address |
| name | string | Yes | Full name |
| phone | string | No | Contact phone number |
| document | string | No | CPF or CNPJ |
| userType | enum | Yes | `SelfRegistered` or `Internal` |
| role | enum | Yes | `Customer`, `Admin`, `Manager`, etc. |
| addresses | array | No | List of addresses |

#### Response

**Status**: `201 Created`

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Jane Smith",
  "email": "user@example.com",
  "userType": "SelfRegistered",
  "role": "Customer",
  "isActive": true
}
```

---

### Update User

Update an existing user's information.

```http
PUT /api/users/{id}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | User unique identifier |

#### Request Body

```json
{
  "name": "Jane Updated Smith",
  "phone": "+5511777777777",
  "role": "Manager"
}
```

**Note**: Only provided fields will be updated. Email cannot be changed through this endpoint.

#### Response

**Status**: `200 OK` - Returns updated user data

---

### Delete User

Soft delete a user account.

```http
DELETE /api/users/{id}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | User unique identifier |

#### Response

**Status**: `204 No Content`

**Note**: This performs a soft delete. User data is retained for audit purposes.

---

### Activate User

Activate a user account.

```http
POST /api/users/{id}/activate
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | User unique identifier |

#### Response

**Status**: `204 No Content`

---

### Deactivate User

Deactivate a user account.

```http
POST /api/users/{id}/deactivate
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | User unique identifier |

#### Response

**Status**: `204 No Content`

---

## Companies

Endpoints for managing company accounts and user associations.

### List Companies

Retrieve a paginated list of all companies.

```http
GET /api/companies
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | integer | No | 1 | Page number |
| pageSize | integer | No | 10 | Items per page |

#### Response

**Status**: `200 OK`

```json
{
  "data": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440000",
      "cnpj": "12345678000190",
      "corporateName": "ABC Corporation Ltd",
      "tradeName": "ABC Corp",
      "isActive": true
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 15,
    "totalPages": 2,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

### Get Company by ID

Retrieve detailed information about a specific company.

```http
GET /api/companies/{id}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | Company unique identifier |

#### Response

**Status**: `200 OK`

```json
{
  "id": "770e8400-e29b-41d4-a716-446655440000",
  "cnpj": "12345678000190",
  "corporateName": "ABC Corporation Ltd",
  "tradeName": "ABC Corp",
  "stateRegistration": "123456789",
  "municipalRegistration": "987654321",
  "isActive": true,
  "createdAt": "2024-01-10T09:00:00Z",
  "addresses": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440000",
      "type": "Commercial",
      "postalCode": "01234567",
      "street": "Business Avenue",
      "number": "1000",
      "neighborhood": "Business District",
      "city": "São Paulo",
      "state": "SP",
      "isDefault": true
    }
  ],
  "users": [
    {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "userName": "John Doe",
      "userEmail": "john.doe@example.com",
      "isActive": true
    }
  ],
  "commercialConditions": [
    {
      "conditionId": "990e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Premium Discount",
      "priority": 10,
      "isActive": true
    }
  ]
}
```

---

### Get Company by CNPJ

Retrieve a company by its CNPJ.

```http
GET /api/companies/cnpj/{cnpj}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| cnpj | string | Yes | Company CNPJ (with or without formatting) |

#### Response

**Status**: `200 OK` - Same structure as List Companies item

---

### Create Company

Create a new company account.

```http
POST /api/companies
```

#### Request Body

```json
{
  "cnpj": "12345678000190",
  "corporateName": "ABC Corporation Ltd",
  "tradeName": "ABC Corp",
  "stateRegistration": "123456789",
  "municipalRegistration": "987654321",
  "addresses": [
    {
      "type": "Commercial",
      "postalCode": "01234567",
      "street": "Business Avenue",
      "number": "1000",
      "neighborhood": "Business District",
      "city": "São Paulo",
      "state": "SP",
      "isDefault": true
    }
  ]
}
```

#### Response

**Status**: `201 Created` - Returns created company data

---

### Update Company

Update an existing company's information.

```http
PUT /api/companies/{id}
```

#### Request Body

```json
{
  "tradeName": "ABC Corporation Updated",
  "stateRegistration": "987654321"
}
```

**Note**: Only provided fields will be updated. CNPJ cannot be changed.

#### Response

**Status**: `200 OK` - Returns updated company data

---

### Delete Company

Soft delete a company account.

```http
DELETE /api/companies/{id}
```

#### Response

**Status**: `204 No Content`

---

### Activate Company

```http
POST /api/companies/{id}/activate
```

#### Response

**Status**: `204 No Content`

---

### Deactivate Company

```http
POST /api/companies/{id}/deactivate
```

#### Response

**Status**: `204 No Content`

---

### Associate User with Company

Associate a user with a company.

```http
POST /api/companies/{id}/users
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | Company unique identifier |

#### Request Body

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### Response

**Status**: `204 No Content`

---

### Disassociate User from Company

Remove a user's association with a company.

```http
DELETE /api/companies/{id}/users/{userId}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | guid | Yes | Company unique identifier |
| userId | guid | Yes | User unique identifier |

#### Response

**Status**: `204 No Content`

---

## Commercial Conditions

Endpoints for managing commercial conditions and their associated rules.

### List Commercial Conditions

Retrieve a paginated list of all commercial conditions.

```http
GET /api/commercialconditions
```

#### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | integer | No | 1 | Page number |
| pageSize | integer | No | 10 | Items per page |

#### Response

**Status**: `200 OK`

```json
{
  "data": [
    {
      "id": "990e8400-e29b-41d4-a716-446655440000",
      "name": "Premium Customer Discount",
      "description": "10% discount for premium customers",
      "priority": 10,
      "validFrom": "2024-01-01T00:00:00Z",
      "validUntil": "2024-12-31T23:59:59Z",
      "isActive": true
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalCount": 8,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

---

### Get Commercial Condition by ID

Retrieve detailed information about a specific commercial condition.

```http
GET /api/commercialconditions/{id}
```

#### Response

**Status**: `200 OK`

```json
{
  "id": "990e8400-e29b-41d4-a716-446655440000",
  "name": "Premium Customer Discount",
  "description": "10% discount for premium customers",
  "priority": 10,
  "validFrom": "2024-01-01T00:00:00Z",
  "validUntil": "2024-12-31T23:59:59Z",
  "isActive": true,
  "createdAt": "2023-12-01T10:00:00Z",
  "rules": [
    {
      "id": "aa0e8400-e29b-41d4-a716-446655440000",
      "ruleType": "Discount",
      "expression": "product.category == 'Electronics'",
      "discountType": "Percentage",
      "discountValue": 10.00,
      "description": "10% off electronics",
      "priority": 10,
      "isActive": true
    },
    {
      "id": "bb0e8400-e29b-41d4-a716-446655440000",
      "ruleType": "Visibility",
      "expression": "product.isPremium == true",
      "discountType": null,
      "discountValue": null,
      "description": "Show only premium products",
      "priority": 5,
      "isActive": true
    }
  ],
  "assignedCompanies": [
    {
      "companyId": "770e8400-e29b-41d4-a716-446655440000",
      "companyName": "ABC Corp",
      "isActive": true
    }
  ]
}
```

---

### Create Commercial Condition

Create a new commercial condition.

```http
POST /api/commercialconditions
```

#### Request Body

```json
{
  "name": "Summer Promotion",
  "description": "Special summer discount",
  "priority": 15,
  "validFrom": "2024-06-01T00:00:00Z",
  "validUntil": "2024-08-31T23:59:59Z"
}
```

#### Response

**Status**: `201 Created`

---

### Update Commercial Condition

```http
PUT /api/commercialconditions/{id}
```

#### Request Body

```json
{
  "name": "Updated Summer Promotion",
  "priority": 20
}
```

#### Response

**Status**: `200 OK`

---

### Delete Commercial Condition

```http
DELETE /api/commercialconditions/{id}
```

#### Response

**Status**: `204 No Content`

---

### Activate Commercial Condition

```http
POST /api/commercialconditions/{id}/activate
```

#### Response

**Status**: `204 No Content`

---

### Deactivate Commercial Condition

```http
POST /api/commercialconditions/{id}/deactivate
```

#### Response

**Status**: `204 No Content`

---

### Get Rules for Commercial Condition

Retrieve all rules associated with a commercial condition.

```http
GET /api/commercialconditions/{id}/rules
```

#### Response

**Status**: `200 OK`

```json
[
  {
    "id": "aa0e8400-e29b-41d4-a716-446655440000",
    "ruleType": "Discount",
    "expression": "product.category == 'Electronics'",
    "discountType": "Percentage",
    "discountValue": 10.00,
    "description": "10% off electronics",
    "priority": 10,
    "isActive": true
  }
]
```

---

### Create Rule for Commercial Condition

Create a new rule within a commercial condition.

```http
POST /api/commercialconditions/{id}/rules
```

#### Request Body (Visibility Rule)

```json
{
  "ruleType": "Visibility",
  "expression": "product.category == 'Premium'",
  "description": "Show only premium products",
  "priority": 10
}
```

#### Request Body (Discount Rule)

```json
{
  "ruleType": "Discount",
  "expression": "product.price > 1000",
  "discountType": "Percentage",
  "discountValue": 15.00,
  "description": "15% off products over $1000",
  "priority": 10
}
```

#### Field Descriptions

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| ruleType | enum | Yes | `Visibility` or `Discount` |
| expression | string | Yes | Expression to evaluate (max 2000 chars) |
| discountType | enum | Conditional | `Percentage` or `Fixed` (required if ruleType is Discount) |
| discountValue | decimal | Conditional | Discount amount (required if ruleType is Discount) |
| description | string | No | Rule description |
| priority | integer | No | Priority order (default: 0) |

#### Response

**Status**: `201 Created`

---

### Update Rule

```http
PUT /api/commercialconditions/{id}/rules/{ruleId}
```

#### Request Body

```json
{
  "expression": "product.category == 'Premium' && product.price > 500",
  "priority": 15
}
```

#### Response

**Status**: `200 OK`

---

### Delete Rule

```http
DELETE /api/commercialconditions/{id}/rules/{ruleId}
```

#### Response

**Status**: `204 No Content`

---

### Assign Commercial Condition to Company

```http
POST /api/commercialconditions/companies/{companyId}/conditions/{conditionId}
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| companyId | guid | Yes | Company unique identifier |
| conditionId | guid | Yes | Commercial condition unique identifier |

#### Response

**Status**: `204 No Content`

---

### Unassign Commercial Condition from Company

```http
DELETE /api/commercialconditions/companies/{companyId}/conditions/{conditionId}
```

#### Response

**Status**: `204 No Content`

---

## Integration

Service-to-service integration endpoints for other microservices.

### Get User Commercial Conditions

Retrieve all commercial conditions applicable to a specific user.

```http
GET /api/integration/users/{userId}/commercial-conditions
```

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | guid | Yes | User unique identifier |

#### Response

**Status**: `200 OK`

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "userName": "John Doe",
  "userEmail": "john.doe@example.com",
  "conditions": [
    {
      "id": "990e8400-e29b-41d4-a716-446655440000",
      "name": "Premium Discount",
      "priority": 10,
      "validFrom": "2024-01-01T00:00:00Z",
      "validUntil": "2024-12-31T23:59:59Z",
      "isActive": true,
      "rules": [
        {
          "id": "aa0e8400-e29b-41d4-a716-446655440000",
          "ruleType": "Discount",
          "expression": "product.category == 'Electronics'",
          "discountType": "Percentage",
          "discountValue": 10.00,
          "priority": 10
        }
      ]
    }
  ]
}
```

---

### Get Company Commercial Conditions

Retrieve all commercial conditions for a specific company.

```http
GET /api/integration/companies/{companyId}/commercial-conditions
```

#### Response

**Status**: `200 OK` - Same structure as Get User Commercial Conditions

---

### Get Visibility Rules

Retrieve visibility rules applicable to a user context.

```http
GET /api/integration/visibility-rules?userId={userId}&companyId={companyId}
```

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | guid | Yes | User unique identifier |
| companyId | guid | No | Company context (optional filter) |

#### Response

**Status**: `200 OK`

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "companyId": "770e8400-e29b-41d4-a716-446655440000",
  "rules": [
    {
      "ruleId": "bb0e8400-e29b-41d4-a716-446655440000",
      "conditionId": "990e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Premium Access",
      "expression": "product.isPremium == true",
      "priority": 10
    }
  ]
}
```

---

### Get Discount Rules

Retrieve discount rules applicable to a user context.

```http
GET /api/integration/discount-rules?userId={userId}&companyId={companyId}
```

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| userId | guid | Yes | User unique identifier |
| companyId | guid | No | Company context (optional filter) |

#### Response

**Status**: `200 OK`

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "companyId": "770e8400-e29b-41d4-a716-446655440000",
  "rules": [
    {
      "ruleId": "aa0e8400-e29b-41d4-a716-446655440000",
      "conditionId": "990e8400-e29b-41d4-a716-446655440000",
      "conditionName": "Premium Discount",
      "expression": "product.category == 'Electronics'",
      "discountType": "Percentage",
      "discountValue": 10.00,
      "priority": 10
    }
  ]
}
```

---

### Get User Expression Context

Retrieve user context data for expression evaluation.

```http
GET /api/integration/users/{userId}/expression-context
```

#### Response

**Status**: `200 OK`

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "userType": "Internal",
  "userRole": "Customer",
  "isActive": true,
  "emailVerified": true,
  "companies": [
    {
      "companyId": "770e8400-e29b-41d4-a716-446655440000",
      "cnpj": "12345678000190",
      "isActive": true
    }
  ],
  "metadata": {
    "createdAt": "2024-01-15T10:30:00Z",
    "hasAddresses": true,
    "addressCount": 2
  }
}
```

---

### User Access Check

Verify user access status.

```http
GET /api/integration/users/{userId}/access-check
```

#### Response

**Status**: `200 OK`

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "hasAccess": true,
  "isActive": true,
  "isDeleted": false,
  "emailVerified": true,
  "userType": "Internal",
  "userRole": "Customer",
  "message": "User has full access"
}
```

---

## Health

Health check endpoint for monitoring and load balancer health checks.

### Health Check

```http
GET /health
```

#### Response

**Status**: `200 OK`

```
Healthy
```

When the database is unavailable:

**Status**: `503 Service Unavailable`

```
Unhealthy
```

---

## Data Models

### Enumerations

#### UserType
- `SelfRegistered`: User created through self-registration
- `Internal`: User created by administrators

#### UserRole
- `Customer`: Standard customer
- `Admin`: System administrator
- `Manager`: Account manager
- `Sales`: Sales representative

#### AddressType
- `Billing`: Billing address
- `Shipping`: Shipping address
- `Both`: Can be used for both purposes
- `Commercial`: Commercial address

#### RuleType
- `Visibility`: Controls product visibility
- `Discount`: Defines pricing discounts

#### DiscountType
- `Percentage`: Percentage-based discount
- `Fixed`: Fixed value discount

---

## Best Practices

### When Using the API

1. **Always handle errors**: Check HTTP status codes and parse error messages
2. **Use pagination**: Don't fetch all records at once, use pagination parameters
3. **Validate input**: Validate data on the client side before sending requests
4. **Check active status**: Verify `isActive` flags when consuming data
5. **Handle soft deletes**: Be aware that DELETE operations are soft deletes

### For Integration Services

1. **Cache commercial conditions**: Commercial conditions don't change frequently, consider caching
2. **Use batch operations**: When checking multiple users, make parallel requests
3. **Respect priority ordering**: Always apply rules in priority order (highest first)
4. **Check validity periods**: Verify `validFrom` and `validUntil` dates for conditions
5. **Handle company context**: Pass `companyId` when user is operating in company context

### Expression Evaluation

When evaluating expressions in visibility and discount rules:

1. **Parse expressions safely**: Use a proper expression evaluator library
2. **Provide full context**: Include all product and user data in evaluation context
3. **Handle evaluation errors**: Expressions may contain syntax errors, handle gracefully
4. **Log evaluation results**: For debugging and audit purposes

---

## Rate Limiting

**Note**: Rate limiting is not currently implemented but is planned for future releases.

---

## Versioning

The API currently supports version 1 (v1). Future versions will be introduced as needed with backward compatibility considerations.

---

## Support

For API questions or issues:
- Review this documentation
- Check the Swagger UI for interactive testing
- Contact: github@fgbrind.es
