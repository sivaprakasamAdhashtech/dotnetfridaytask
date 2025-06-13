# Multi-Tenant SaaS Billing Platform

A comprehensive, secure, and scalable multi-tenant backend system built with ASP.NET Core 8 and MongoDB that supports tenant and subscription management, billing, audit logging, and integrations.

## 🎯 Features

### Core Functionality
- **Multi-Tenant Architecture** with complete tenant isolation
- **JWT Authentication** with tenant and role-based claims
- **Subscription Management** with multiple billing cycles
- **Automated Invoice Generation** with Hangfire background jobs
- **Audit Logging** for all system activities
- **Rate Limiting** per tenant with configurable limits

### Advanced Features
- **Stripe Webhook Simulation** for payment processing
- **Role-Based Authorization** (SuperAdmin, TenantAdmin, BillingUser)
- **Comprehensive API Documentation** with Swagger/OpenAPI
- **Background Job Processing** with Hangfire dashboard
- **Structured Logging** with Serilog
- **Docker Support** for easy deployment

## 🛠️ Technology Stack

- **.NET 8** - ASP.NET Core Web API
- **MongoDB** - Document database with MongoDB.Driver
- **JWT Authentication** - Secure token-based authentication
- **Serilog** - Structured logging
- **Hangfire** - Background job processing
- **Swagger/OpenAPI** - API documentation
- **xUnit** - Unit testing framework
- **Docker** - Containerization

## 📐 Architecture

### Domain Entities
- **Tenants** - Multi-tenant organizations
- **Users** - System users with role-based access
- **SubscriptionPlans** - Available subscription tiers
- **Subscriptions** - Active tenant subscriptions
- **Invoices** - Billing invoices with line items
- **AuditLogs** - System activity tracking

### Security & Authorization
- **JWT Tokens** with embedded TenantId and Role claims
- **Tenant Isolation Middleware** for data security
- **Rate Limiting Middleware** per tenant
- **Role-Based Access Control** across all endpoints

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Docker and Docker Compose
- MongoDB (or use Docker)

### Using Docker (Recommended)

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd dotnetfridaytask
   ```

2. **Start the application with Docker Compose**
   ```bash
   docker-compose up -d
   ```

3. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000
   - Hangfire Dashboard: http://localhost:5000/hangfire
   - MongoDB Admin: http://localhost:8081 (admin/admin123)

### Manual Setup

1. **Install MongoDB**
   ```bash
   # Using Docker
   docker run -d -p 27017:27017 --name mongodb mongo:7.0
   ```

2. **Update connection string**
   ```json
   // appsettings.Development.json
   {
     "MongoDbSettings": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "MultiTenantBilling_Dev"
     }
   }
   ```

3. **Run the application**
   ```bash
   cd src/MultiTenantBilling.Api
   dotnet run
   ```

## 📋 API Endpoints

### Authentication
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/auth/login` | POST | Authenticate and return JWT | Public |
| `/api/auth/logout` | POST | Logout user | Authenticated |

### Tenants
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/tenants` | POST | Create new tenant | SuperAdmin |
| `/api/tenants` | GET | List all tenants | SuperAdmin |
| `/api/tenants/{id}` | GET | Get tenant details | TenantAdmin+ |
| `/api/tenants/{id}` | PUT | Update tenant | SuperAdmin/TenantAdmin |

### Users
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/users` | POST | Create user in tenant | TenantAdmin |
| `/api/users` | GET | List tenant users | TenantAdmin+ |
| `/api/users/{id}` | GET | Get user details | TenantAdmin+ |
| `/api/users/{id}` | PUT | Update user | TenantAdmin |
| `/api/users/{id}/change-password` | POST | Change password | User/TenantAdmin |

### Subscription Plans
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/subscription-plans` | GET | List available plans | Authenticated |

### Subscriptions
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/subscriptions` | POST | Create subscription | SuperAdmin |
| `/api/subscriptions` | GET | List tenant subscriptions | TenantAdmin+ |
| `/api/subscriptions/{id}` | GET | Get subscription details | TenantAdmin+ |
| `/api/subscriptions/{id}` | PUT | Update subscription | SuperAdmin/TenantAdmin |
| `/api/subscriptions/active` | GET | Get active subscription | TenantAdmin+ |

### Invoices
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/invoices` | GET | List tenant invoices | TenantAdmin+ |
| `/api/invoices/{id}` | GET | Get invoice details | TenantAdmin+ |
| `/api/invoices` | POST | Create manual invoice | SuperAdmin/TenantAdmin |
| `/api/invoices/{id}/status` | PUT | Update invoice status | SuperAdmin/TenantAdmin |

### Audit Logs
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/audit-logs` | GET | Get audit logs (filtered) | SuperAdmin (all), Others (tenant) |
| `/api/audit-logs/count` | GET | Get audit log count | TenantAdmin+ |
| `/api/audit-logs/cleanup` | POST | Cleanup old logs | SuperAdmin |

### Webhooks
| Endpoint | Method | Description | Access |
|----------|--------|-------------|--------|
| `/api/webhooks/stripe` | POST | Stripe webhook endpoint | Public (with signature) |
| `/api/webhooks/test` | POST | Test webhook endpoint | Public |

## 🔐 Authentication & Authorization

### Sample Users
The system comes with pre-configured sample users:

**Super Admin**
- Email: `superadmin@system.com`
- Password: `Admin123!`
- Role: SuperAdmin
- Access: All tenants and system functions

**Demo Tenant Admin**
- Email: `admin@democompany.com`
- Password: `Demo123!`
- Role: TenantAdmin
- Tenant: DEMO-001

### JWT Token Example
```bash
# Login to get JWT token
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "superadmin@system.com",
    "password": "Admin123!"
  }'

# Use token in subsequent requests
curl -X GET "http://localhost:5000/api/tenants" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 🔄 Background Jobs

The system includes automated background jobs powered by Hangfire:

### Scheduled Jobs
- **Daily Billing** (2 AM UTC) - Process subscription billing
- **Overdue Invoices** (Every 6 hours) - Mark overdue invoices
- **Payment Retries** (8 AM & 8 PM UTC) - Retry failed payments
- **Audit Cleanup** (Weekly, Sunday 3 AM) - Remove old audit logs

### Hangfire Dashboard
Access the Hangfire dashboard at `/hangfire` to monitor:
- Job execution status
- Failed job details
- Recurring job schedules
- Server statistics

## 🎣 Webhook Integration

### Stripe Webhook Simulation
```bash
# Simulate successful payment
curl -X POST "http://localhost:5000/api/webhooks/stripe" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: sha256=test_signature" \
  -d '{
    "event": "invoice.paid",
    "invoiceId": "INV-202412-0001",
    "amountPaid": 29.99,
    "tenantId": "DEMO-001",
    "paymentMethod": "card",
    "transactionId": "txn_test123"
  }'

# Simulate failed payment
curl -X POST "http://localhost:5000/api/webhooks/stripe" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: sha256=test_signature" \
  -d '{
    "event": "payment_failed",
    "invoiceId": "INV-202412-0001",
    "amountPaid": 0,
    "tenantId": "DEMO-001"
  }'
```

## 🧪 Testing

### Run Unit Tests
```bash
cd tests/MultiTenantBilling.Tests
dotnet test
```

### Run Integration Tests
```bash
# Requires Docker for test containers
dotnet test --filter "Category=Integration"
```

### Test Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 MongoDB Collections & Indexing

### Collections
- `tenants` - Tenant information
- `users` - User accounts
- `subscriptionPlans` - Available plans
- `subscriptions` - Active subscriptions
- `invoices` - Billing invoices
- `auditLogs` - System audit trail

### Key Indexes
- `users`: `{ email: 1, tenantId: 1 }` (unique)
- `auditLogs`: `{ tenantId: 1, timestamp: -1 }`
- `invoices`: `{ tenantId: 1, dueDate: 1 }`
- `subscriptions`: `{ tenantId: 1 }`, `{ nextBillingDate: 1 }`

## 🐳 Docker Configuration

### Services
- **API** - ASP.NET Core application (port 5000/5001)
- **MongoDB** - Database server (port 27017)
- **Mongo Express** - Database admin UI (port 8081)

### Environment Variables
```bash
# MongoDB
MONGO_INITDB_ROOT_USERNAME=admin
MONGO_INITDB_ROOT_PASSWORD=password123

# API
ASPNETCORE_ENVIRONMENT=Development
MongoDbSettings__ConnectionString=mongodb://admin:password123@mongodb:27017/MultiTenantBilling?authSource=admin
```

## 📈 Monitoring & Logging

### Structured Logging
- Console output for development
- File logging with daily rotation
- Correlation IDs for request tracking
- Audit trail for all operations

### Log Levels
- **Information** - Normal operations
- **Warning** - Rate limits, validation failures
- **Error** - System errors, webhook failures
- **Critical** - System-wide issues

## 🔧 Configuration

### Key Settings
```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "MultiTenantBilling"
  },
  "JwtSettings": {
    "SecretKey": "YourSecretKey",
    "ExpirationHours": 24
  },
  "StripeSettings": {
    "WebhookSecret": "whsec_your_secret"
  }
}
```

## 🚀 Deployment

### Production Checklist
- [ ] Update JWT secret key
- [ ] Configure MongoDB connection string
- [ ] Set up Stripe webhook endpoints
- [ ] Configure logging destinations
- [ ] Set up monitoring and alerting
- [ ] Configure HTTPS certificates
- [ ] Set up backup strategies

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📞 Support

For support and questions:
- Create an issue in the repository
- Check the API documentation at `/swagger`
- Review the Hangfire dashboard at `/hangfire`
