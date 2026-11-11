namespace UGeekStore.Core;

public enum UserRole { Admin, Manager, Cashier, Confectioner, Courier }

public enum CashMovementType { Income, Expense, Debt }

public enum ShiftStatus { Open, Closed }

public enum OrderType { Individual, Stock }

public enum OrderStatus { New, InProduction, Ready, OutForDelivery, Completed, Cancelled }

public enum PurchaseStatus { Requested, Approved, Purchased, Rejected }

public enum SmmStatus { Idea, Planned, Ready, Published }

public enum ChecklistFrequency { Daily, Weekly }

public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Manager = nameof(UserRole.Manager);
    public const string Cashier = nameof(UserRole.Cashier);
    public const string Confectioner = nameof(UserRole.Confectioner);
    public const string Courier = nameof(UserRole.Courier);

    public const string Management = Admin + "," + Manager;
    public const string CashDesk = Admin + "," + Manager + "," + Cashier;
    public const string Production = Admin + "," + Manager + "," + Confectioner;
}
