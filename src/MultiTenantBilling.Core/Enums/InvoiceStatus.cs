namespace MultiTenantBilling.Core.Enums;

public enum InvoiceStatus
{
    Draft = 1,
    Pending = 2,
    Paid = 3,
    Failed = 4,
    Overdue = 5,
    Cancelled = 6
}
