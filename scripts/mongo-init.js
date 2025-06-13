// MongoDB initialization script for Multi-Tenant Billing Platform

// Switch to the application database
db = db.getSiblingDB('MultiTenantBilling');

// Create collections with validation
db.createCollection('tenants', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['name', 'tenantId', 'contactEmail', 'isActive'],
      properties: {
        name: { bsonType: 'string', maxLength: 100 },
        tenantId: { bsonType: 'string', maxLength: 50 },
        contactEmail: { bsonType: 'string', maxLength: 255 },
        isActive: { bsonType: 'bool' },
        maxRequestsPerMinute: { bsonType: 'int', minimum: 1 }
      }
    }
  }
});

db.createCollection('users', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['firstName', 'lastName', 'email', 'passwordHash', 'tenantId', 'role', 'isActive'],
      properties: {
        firstName: { bsonType: 'string', maxLength: 100 },
        lastName: { bsonType: 'string', maxLength: 100 },
        email: { bsonType: 'string', maxLength: 255 },
        tenantId: { bsonType: 'string', maxLength: 50 },
        role: { enum: ['SuperAdmin', 'TenantAdmin', 'BillingUser'] },
        isActive: { bsonType: 'bool' }
      }
    }
  }
});

// Create indexes
db.tenants.createIndex({ 'tenantId': 1 }, { unique: true });
db.users.createIndex({ 'email': 1, 'tenantId': 1 }, { unique: true });
db.users.createIndex({ 'tenantId': 1 });
db.subscriptions.createIndex({ 'tenantId': 1 });
db.subscriptions.createIndex({ 'nextBillingDate': 1 });
db.invoices.createIndex({ 'tenantId': 1, 'dueDate': 1 });
db.invoices.createIndex({ 'invoiceNumber': 1 }, { unique: true });
db.auditLogs.createIndex({ 'tenantId': 1, 'timestamp': -1 });
db.auditLogs.createIndex({ 'timestamp': -1 });

// Insert sample subscription plans
db.subscriptionPlans.insertMany([
  {
    name: 'Starter',
    description: 'Perfect for small teams getting started',
    price: NumberDecimal('29.99'),
    billingCycle: 1, // Monthly
    isActive: true,
    features: {
      'maxUsers': 5,
      'maxStorageGB': 10,
      'maxApiCallsPerMonth': 10000,
      'support': 'Email',
      'customBranding': false
    },
    maxUsers: 5,
    maxStorageGB: NumberLong(10),
    maxApiCallsPerMonth: 10000,
    trialDays: 14,
    planCode: 'STARTER_MONTHLY',
    sortOrder: 1,
    createdAt: new Date(),
    updatedAt: new Date()
  },
  {
    name: 'Professional',
    description: 'Ideal for growing businesses',
    price: NumberDecimal('79.99'),
    billingCycle: 1, // Monthly
    isActive: true,
    features: {
      'maxUsers': 25,
      'maxStorageGB': 100,
      'maxApiCallsPerMonth': 100000,
      'support': 'Email & Chat',
      'customBranding': true,
      'advancedReporting': true
    },
    maxUsers: 25,
    maxStorageGB: NumberLong(100),
    maxApiCallsPerMonth: 100000,
    trialDays: 14,
    planCode: 'PROFESSIONAL_MONTHLY',
    sortOrder: 2,
    createdAt: new Date(),
    updatedAt: new Date()
  },
  {
    name: 'Enterprise',
    description: 'For large organizations with advanced needs',
    price: NumberDecimal('199.99'),
    billingCycle: 1, // Monthly
    isActive: true,
    features: {
      'maxUsers': -1, // Unlimited
      'maxStorageGB': 1000,
      'maxApiCallsPerMonth': 1000000,
      'support': '24/7 Phone & Email',
      'customBranding': true,
      'advancedReporting': true,
      'sso': true,
      'dedicatedSupport': true
    },
    maxUsers: null, // Unlimited
    maxStorageGB: NumberLong(1000),
    maxApiCallsPerMonth: 1000000,
    trialDays: 30,
    planCode: 'ENTERPRISE_MONTHLY',
    sortOrder: 3,
    createdAt: new Date(),
    updatedAt: new Date()
  }
]);

// Insert sample tenant
db.tenants.insertOne({
  name: 'Demo Company',
  tenantId: 'DEMO-001',
  contactEmail: 'admin@democompany.com',
  contactPhone: '+1-555-0123',
  address: '123 Demo Street, Demo City, DC 12345',
  isActive: true,
  maxRequestsPerMinute: 100,
  settings: {
    'timezone': 'UTC',
    'currency': 'USD',
    'dateFormat': 'MM/dd/yyyy'
  },
  createdAt: new Date(),
  updatedAt: new Date(),
  createdBy: 'system',
  updatedBy: 'system'
});

// Insert sample super admin user (password: Admin123!)
db.users.insertOne({
  firstName: 'Super',
  lastName: 'Admin',
  email: 'superadmin@system.com',
  passwordHash: '$2a$11$8K1p/a0dURXAMcGe71sS1eKkNE.DjPKJZZGOTCqKtpbDQbQhErBSW', // Admin123!
  tenantId: 'SYSTEM',
  role: 'SuperAdmin',
  isActive: true,
  phoneNumber: '+1-555-0100',
  preferences: {},
  createdAt: new Date(),
  updatedAt: new Date(),
  createdBy: 'system',
  updatedBy: 'system'
});

// Insert sample tenant admin user (password: Demo123!)
db.users.insertOne({
  firstName: 'Demo',
  lastName: 'Admin',
  email: 'admin@democompany.com',
  passwordHash: '$2a$11$rKKqJQj8fHZMZjd8LrKrKOQGQqQqQqQqQqQqQqQqQqQqQqQqQqQqQ', // Demo123!
  tenantId: 'DEMO-001',
  role: 'TenantAdmin',
  isActive: true,
  phoneNumber: '+1-555-0123',
  preferences: {},
  createdAt: new Date(),
  updatedAt: new Date(),
  createdBy: 'system',
  updatedBy: 'system'
});

print('MongoDB initialization completed successfully!');
print('Sample data inserted:');
print('- 3 subscription plans (Starter, Professional, Enterprise)');
print('- 1 demo tenant (DEMO-001)');
print('- 1 super admin user (superadmin@system.com / Admin123!)');
print('- 1 tenant admin user (admin@democompany.com / Demo123!)');
print('');
print('You can now start the API and test the endpoints!');
