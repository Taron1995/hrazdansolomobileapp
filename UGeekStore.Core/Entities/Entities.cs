using System.ComponentModel.DataAnnotations;

namespace UGeekStore.Core.Entities;

public class Employee
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string FullName { get; set; } = "";
    /// <summary>Gmail / Google account used to sign in.</summary>
    [Required, MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(30)] public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CashRegister
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

/// <summary>A working day of one cash register. Its date is fixed when the shift is opened and cannot be changed.</summary>
public class CashShift
{
    public int Id { get; set; }
    public int CashRegisterId { get; set; }
    public CashRegister? CashRegister { get; set; }
    /// <summary>Business day, fixed at opening (server clock, local time zone).</summary>
    public DateTime Date { get; set; }
    public DateTime OpenedAt { get; set; }
    public int OpenedById { get; set; }
    public Employee? OpenedBy { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;

    public DateTime? ClosedAt { get; set; }
    public int? ClosedById { get; set; }
    public Employee? ClosedBy { get; set; }
    public int Count1000 { get; set; }
    public int Count2000 { get; set; }
    public int Count5000 { get; set; }
    public int Count10000 { get; set; }
    public int Count20000 { get; set; }
    public int Count50000 { get; set; }
    public decimal CountedTotal { get; set; }
    public string? CloseComment { get; set; }

    public List<CashTransaction> Transactions { get; set; } = new();
}

public class CashTransaction
{
    public int Id { get; set; }
    public int CashShiftId { get; set; }
    public CashShift? CashShift { get; set; }
    /// <summary>Server time when the record was received.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Time reported by the phone at the moment of entry.</summary>
    public DateTime? DeviceTime { get; set; }
    public CashMovementType Type { get; set; }
    [Required, MaxLength(100)] public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
    [Required, MaxLength(120)] public string CashierName { get; set; } = "";
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsVoided { get; set; }
    [MaxLength(500)] public string? VoidReason { get; set; }
    public bool SyncedToSheet { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    /// <summary>Normalized phone, e.g. 37491123456.</summary>
    [Required, MaxLength(30)] public string Phone { get; set; } = "";
    [MaxLength(300)] public string? Address { get; set; }
    [MaxLength(100)] public string? District { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    /// <summary>Customer agreed to receive new-product messages (SMS / WhatsApp / push).</summary>
    public bool MarketingConsent { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<CustomerIssue> Issues { get; set; } = new();
}

/// <summary>A problem that happened with a customer; shown as a warning on their future orders.</summary>
public class CustomerIssue
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    [Required, MaxLength(1000)] public string Description { get; set; } = "";
    public int CreatedById { get; set; }
    public Employee? CreatedBy { get; set; }
    public bool Resolved { get; set; }
}

public class Product
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(100)] public string? Category { get; set; }
    public decimal Price { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public OrderType Type { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedById { get; set; }
    public Employee? CreatedBy { get; set; }
    /// <summary>When the order must be ready / handed over.</summary>
    public DateTime DueAt { get; set; }
    /// <summary>Confectioner who makes the order.</summary>
    public int? ExecutorId { get; set; }
    public Employee? Executor { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Prepayment { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;

    public bool IsDelivery { get; set; }
    [MaxLength(300)] public string? DeliveryAddress { get; set; }
    [MaxLength(100)] public string? DeliveryDistrict { get; set; }
    public int? CourierId { get; set; }
    public Employee? Courier { get; set; }
    public decimal DeliveryFee { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public class Ingredient
{
    public int Id { get; set; }
    [Required, MaxLength(120)] public string Name { get; set; } = "";
    [Required, MaxLength(20)] public string Unit { get; set; } = "կգ";
    public decimal StockQty { get; set; }
    public decimal MinQty { get; set; }
}

public class PurchaseRequest
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedById { get; set; }
    public Employee? CreatedBy { get; set; }
    public int? IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    [Required, MaxLength(150)] public string ItemName { get; set; } = "";
    public decimal Quantity { get; set; }
    [MaxLength(20)] public string? Unit { get; set; }
    [MaxLength(150)] public string? Supplier { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Requested;
    public DateTime? PurchasedAt { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class SmmPost
{
    public int Id { get; set; }
    public DateTime PlannedDate { get; set; }
    [Required, MaxLength(50)] public string Platform { get; set; } = "Instagram";
    [Required, MaxLength(200)] public string Topic { get; set; } = "";
    [MaxLength(50)] public string? ContentType { get; set; }
    [MaxLength(2000)] public string? Caption { get; set; }
    public SmmStatus Status { get; set; } = SmmStatus.Idea;
    public int? ResponsibleId { get; set; }
    public Employee? Responsible { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class ChecklistItem
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = "";
    /// <summary>Null = for every role.</summary>
    public UserRole? Role { get; set; }
    public ChecklistFrequency Frequency { get; set; } = ChecklistFrequency.Daily;
    /// <summary>A photo/video report must be captured on the spot (e.g. after cleaning).</summary>
    public bool RequiresMediaReport { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ChecklistEntry
{
    public int Id { get; set; }
    public int ChecklistItemId { get; set; }
    public ChecklistItem? ChecklistItem { get; set; }
    public DateTime Date { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? ReportFilePath { get; set; }
    [MaxLength(1000)] public string? Comment { get; set; }
}

public class MissedCall
{
    public int Id { get; set; }
    [Required, MaxLength(30)] public string Phone { get; set; } = "";
    public DateTime CalledAt { get; set; }
    [MaxLength(50)] public string Source { get; set; } = "manual";
    public bool CalledBack { get; set; }
    public DateTime? CalledBackAt { get; set; }
    public int? HandledById { get; set; }
    public Employee? HandledBy { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class ChatMessage
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    [Required, MaxLength(120)] public string SenderName { get; set; } = "";
    [Required, MaxLength(2000)] public string Text { get; set; } = "";
    public DateTime SentAt { get; set; }
}
