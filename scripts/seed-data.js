// Seed data script for Multi-Tenant Billing Platform
// Run this script to populate the database with sample data for testing

// Connect to the database
db = db.getSiblingDB('MultiTenantBilling');

print('🌱 Starting seed data insertion...');

// Clear existing data (optional - uncomment if needed)
// db.tenants.deleteMany({});
// db.users.deleteMany({});
// db.subscriptionPlans.deleteMany({});
// db.subscriptions.deleteMany({});
// db.invoices.deleteMany({});

// Insert additional subscription plans
print('📋 Inserting subscription plans...');
db.subscriptionPlans.insertMany([
  {
    name: 'Starter Annual',
    description: 'Perfect for small teams - Annual billing',
    price: NumberDecimal('299.99'),
    billingCycle: 12, // Annually
    isActive: true,
    features: {
      'maxUsers': 5,
      'maxStorageGB': 10,
      'maxApiCallsPerMonth': 10000,
      'support': 'Email',
      'customBranding': false,
      'annualDiscount': true
    },
    maxUsers: 5,
    maxStorageGB: NumberLong(10),
    maxApiCallsPerMonth: 10000,
    trialDays: 30,
    planCode: 'STARTER_ANNUAL',
    sortOrder: 1,
    createdAt: new Date(),
    updatedAt: new Date()
  },
  {
    name: 'Professional Annual',
    description: 'Ideal for growing businesses - Annual billing',
    price: NumberDecimal('799.99'),
    billingCycle: 12, // Annually
    isActive: true,
    features: {
      'maxUsers': 25,
      'maxStorageGB': 100,
      'maxApiCallsPerMonth': 100000,
      'support': 'Email & Chat',
      'customBranding': true,
      'advancedReporting': true,
      'annualDiscount': true
    },
    maxUsers: 25,
    maxStorageGB: NumberLong(100),
    maxApiCallsPerMonth: 100000,
    trialDays: 30,
    planCode: 'PROFESSIONAL_ANNUAL',
    sortOrder: 2,
    createdAt: new Date(),
    updatedAt: new Date()
  }
]);

// Insert additional tenants
print('🏢 Inserting sample tenants...');
db.tenants.insertMany([
  {
    name: 'TechCorp Solutions',
    tenantId: 'TECH-001',
    contactEmail: 'admin@techcorp.com',
    contactPhone: '+1-555-0200',
    address: '456 Tech Avenue, Silicon Valley, CA 94000',
    isActive: true,
    maxRequestsPerMinute: 150,
    settings: {
      'timezone': 'America/Los_Angeles',
      'currency': 'USD',
      'dateFormat': 'MM/dd/yyyy',
      'theme': 'dark'
    },
    createdAt: new Date(),
    updatedAt: new Date(),
    createdBy: 'system',
    updatedBy: 'system'
  },
  {
    name: 'Global Enterprises Ltd',
    tenantId: 'GLOBAL-001',
    contactEmail: 'billing@globalent.com',
    contactPhone: '+44-20-7946-0958',
    address: '789 Business Park, London, UK EC1A 1BB',
    isActive: true,
    maxRequestsPerMinute: 200,
    settings: {
      'timezone': 'Europe/London',
      'currency': 'GBP',
      'dateFormat': 'dd/MM/yyyy',
      'theme': 'light'
    },
    createdAt: new Date(),
    updatedAt: new Date(),
    createdBy: 'system',
    updatedBy: 'system'
  }
]);

// Insert additional users
print('👥 Inserting sample users...');
db.users.insertMany([
  {
    firstName: 'John',
    lastName: 'Smith',
    email: 'john.smith@techcorp.com',
    passwordHash: '$2a$11$8K1p/a0dURXAMcGe71sS1eKkNE.DjPKJZZGOTCqKtpbDQbQhErBSW', // Tech123!
    tenantId: 'TECH-001',
    role: 'TenantAdmin',
    isActive: true,
    phoneNumber: '+1-555-0201',
    preferences: {
      'notifications': true,
      'language': 'en-US'
    },
    createdAt: new Date(),
    updatedAt: new Date(),
    createdBy: 'system',
    updatedBy: 'system'
  },
  {
    firstName: 'Sarah',
    lastName: 'Johnson',
    email: 'sarah.johnson@techcorp.com',
    passwordHash: '$2a$11$8K1p/a0dURXAMcGe71sS1eKkNE.DjPKJZZGOTCqKtpbDQbQhErBSW', // Tech123!
    tenantId: 'TECH-001',
    role: 'BillingUser',
    isActive: true,
    phoneNumber: '+1-555-0202',
    preferences: {
      'notifications': true,
      'language': 'en-US'
    },
    createdAt: new Date(),
    updatedAt: new Date(),
    createdBy: 'system',
    updatedBy: 'system'
  },
  {
    firstName: 'David',
    lastName: 'Wilson',
    email: 'david.wilson@globalent.com',
    passwordHash: '$2a$11$8K1p/a0dURXAMcGe71sS1eKkNE.DjPKJZZGOTCqKtpbDQbQhErBSW', // Global123!
    tenantId: 'GLOBAL-001',
    role: 'TenantAdmin',
    isActive: true,
    phoneNumber: '+44-20-7946-0959',
    preferences: {
      'notifications': true,
      'language': 'en-GB'
    },
    createdAt: new Date(),
    updatedAt: new Date(),
    createdBy: 'system',
    updatedBy: 'system'
  }
]);

// Insert sample subscriptions
print('📊 Inserting sample subscriptions...');
var starterPlan = db.subscriptionPlans.findOne({planCode: 'STARTER_MONTHLY'});
var proPlan = db.subscriptionPlans.findOne({planCode: 'PROFESSIONAL_MONTHLY'});

if (starterPlan && proPlan) {
  db.subscriptions.insertMany([
    {
      tenantId: 'TECH-001',
      subscriptionPlanId: proPlan._id.toString(),
      status: 'Active',
      startDate: new Date('2024-01-01'),
      nextBillingDate: new Date('2024-12-01'),
      currentPrice: proPlan.price,
      isTrialActive: false,
      lastBilledDate: new Date('2024-11-01'),
      billingCycleCount: 11,
      createdAt: new Date(),
      updatedAt: new Date(),
      createdBy: 'system',
      updatedBy: 'system',
      metadata: {
        'source': 'seed_data',
        'notes': 'Sample subscription for TechCorp'
      }
    },
    {
      tenantId: 'GLOBAL-001',
      subscriptionPlanId: starterPlan._id.toString(),
      status: 'Active',
      startDate: new Date('2024-06-01'),
      nextBillingDate: new Date('2024-12-01'),
      currentPrice: starterPlan.price,
      isTrialActive: false,
      lastBilledDate: new Date('2024-11-01'),
      billingCycleCount: 6,
      createdAt: new Date(),
      updatedAt: new Date(),
      createdBy: 'system',
      updatedBy: 'system',
      metadata: {
        'source': 'seed_data',
        'notes': 'Sample subscription for Global Enterprises'
      }
    }
  ]);
}

// Insert sample invoices
print('🧾 Inserting sample invoices...');
var techSubscription = db.subscriptions.findOne({tenantId: 'TECH-001'});
var globalSubscription = db.subscriptions.findOne({tenantId: 'GLOBAL-001'});

if (techSubscription && globalSubscription) {
  db.invoices.insertMany([
    {
      invoiceNumber: 'INV-202411-0001',
      tenantId: 'TECH-001',
      subscriptionId: techSubscription._id.toString(),
      status: 'Paid',
      amount: NumberDecimal('79.99'),
      taxAmount: NumberDecimal('8.00'),
      totalAmount: NumberDecimal('87.99'),
      currency: 'USD',
      issueDate: new Date('2024-11-01'),
      dueDate: new Date('2024-11-30'),
      paidDate: new Date('2024-11-05'),
      paymentMethod: 'stripe',
      paymentTransactionId: 'txn_sample_001',
      lineItems: [
        {
          description: 'Professional - Monthly Subscription',
          quantity: 1,
          unitPrice: NumberDecimal('79.99'),
          totalPrice: NumberDecimal('79.99'),
          periodStart: new Date('2024-11-01'),
          periodEnd: new Date('2024-12-01')
        }
      ],
      notes: 'Sample paid invoice',
      paymentRetryCount: 0,
      createdAt: new Date('2024-11-01'),
      updatedAt: new Date('2024-11-05'),
      createdBy: 'system',
      updatedBy: 'system'
    },
    {
      invoiceNumber: 'INV-202411-0002',
      tenantId: 'GLOBAL-001',
      subscriptionId: globalSubscription._id.toString(),
      status: 'Pending',
      amount: NumberDecimal('29.99'),
      taxAmount: NumberDecimal('3.00'),
      totalAmount: NumberDecimal('32.99'),
      currency: 'USD',
      issueDate: new Date('2024-11-01'),
      dueDate: new Date('2024-11-30'),
      lineItems: [
        {
          description: 'Starter - Monthly Subscription',
          quantity: 1,
          unitPrice: NumberDecimal('29.99'),
          totalPrice: NumberDecimal('29.99'),
          periodStart: new Date('2024-11-01'),
          periodEnd: new Date('2024-12-01')
        }
      ],
      notes: 'Sample pending invoice',
      paymentRetryCount: 0,
      createdAt: new Date('2024-11-01'),
      updatedAt: new Date('2024-11-01'),
      createdBy: 'system',
      updatedBy: 'system'
    }
  ]);
}

// Insert sample audit logs
print('📝 Inserting sample audit logs...');
db.auditLogs.insertMany([
  {
    tenantId: 'TECH-001',
    userId: 'system',
    action: 'CREATE_SUBSCRIPTION',
    entityType: 'Subscription',
    entityId: techSubscription ? techSubscription._id.toString() : 'sample-id',
    timestamp: new Date('2024-01-01T10:00:00Z'),
    description: 'Created Professional subscription for TechCorp',
    severity: 'Info',
    metadata: {
      'source': 'seed_data'
    }
  },
  {
    tenantId: 'GLOBAL-001',
    userId: 'system',
    action: 'CREATE_SUBSCRIPTION',
    entityType: 'Subscription',
    entityId: globalSubscription ? globalSubscription._id.toString() : 'sample-id',
    timestamp: new Date('2024-06-01T14:30:00Z'),
    description: 'Created Starter subscription for Global Enterprises',
    severity: 'Info',
    metadata: {
      'source': 'seed_data'
    }
  },
  {
    tenantId: 'TECH-001',
    userId: 'system',
    action: 'INVOICE_PAID',
    entityType: 'Invoice',
    entityId: 'INV-202411-0001',
    timestamp: new Date('2024-11-05T09:15:00Z'),
    description: 'Invoice INV-202411-0001 marked as paid',
    severity: 'Info',
    metadata: {
      'paymentMethod': 'stripe',
      'amount': 87.99
    }
  }
]);

print('✅ Seed data insertion completed successfully!');
print('');
print('📊 Summary of inserted data:');
print('- Subscription Plans: ' + db.subscriptionPlans.countDocuments());
print('- Tenants: ' + db.tenants.countDocuments());
print('- Users: ' + db.users.countDocuments());
print('- Subscriptions: ' + db.subscriptions.countDocuments());
print('- Invoices: ' + db.invoices.countDocuments());
print('- Audit Logs: ' + db.auditLogs.countDocuments());
print('');
print('🔑 Test Credentials:');
print('Super Admin: superadmin@system.com / Admin123!');
print('Demo Admin: admin@democompany.com / Demo123!');
print('TechCorp Admin: john.smith@techcorp.com / Tech123!');
print('Global Admin: david.wilson@globalent.com / Global123!');
print('');
print('🚀 You can now test the API endpoints!');
