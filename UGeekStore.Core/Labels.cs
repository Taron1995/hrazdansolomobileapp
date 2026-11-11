namespace UGeekStore.Core;

/// <summary>Armenian display names for enums and the fixed cash-movement categories.</summary>
public static class Labels
{
    public static string Hy(this UserRole r) => r switch
    {
        UserRole.Admin => "Ադմինիստրատոր",
        UserRole.Manager => "Մենեջեր",
        UserRole.Cashier => "Գանձապահ",
        UserRole.Confectioner => "Հրուշակագործ",
        UserRole.Courier => "Առաքիչ",
        _ => r.ToString()
    };

    public static string Hy(this CashMovementType t) => t switch
    {
        CashMovementType.Income => "Մուտք",
        CashMovementType.Expense => "Ելք",
        CashMovementType.Debt => "Պարտք",
        _ => t.ToString()
    };

    public static string Hy(this OrderType t) => t switch
    {
        OrderType.Individual => "Անհատական",
        OrderType.Stock => "Առկա տեսականի",
        _ => t.ToString()
    };

    public static string Hy(this OrderStatus s) => s switch
    {
        OrderStatus.New => "Նոր",
        OrderStatus.InProduction => "Արտադրության մեջ",
        OrderStatus.Ready => "Պատրաստ",
        OrderStatus.OutForDelivery => "Առաքման մեջ",
        OrderStatus.Completed => "Հանձնված",
        OrderStatus.Cancelled => "Չեղարկված",
        _ => s.ToString()
    };

    public static string Hy(this PurchaseStatus s) => s switch
    {
        PurchaseStatus.Requested => "Հայտ",
        PurchaseStatus.Approved => "Հաստատված",
        PurchaseStatus.Purchased => "Գնված",
        PurchaseStatus.Rejected => "Մերժված",
        _ => s.ToString()
    };

    public static string Hy(this SmmStatus s) => s switch
    {
        SmmStatus.Idea => "Գաղափար",
        SmmStatus.Planned => "Պլանավորված",
        SmmStatus.Ready => "Պատրաստ",
        SmmStatus.Published => "Հրապարակված",
        _ => s.ToString()
    };

    public static string Hy(this ChecklistFrequency f) => f switch
    {
        ChecklistFrequency.Daily => "Ամենօրյա",
        ChecklistFrequency.Weekly => "Շաբաթական",
        _ => f.ToString()
    };
}

/// <summary>Fixed list of cash movement categories (section 2.1.1.3 of the spec).</summary>
public static class CashCategories
{
    public static readonly IReadOnlyDictionary<CashMovementType, string[]> All = new Dictionary<CashMovementType, string[]>
    {
        [CashMovementType.Income] = new[] { "Նախկին պարտքի մարում", "Այլ" },
        [CashMovementType.Expense] = new[] { "Գնում մատակարարից", "Առաքում", "Ապրանքի վերադարձ", "Այլ" },
        [CashMovementType.Debt] = new[] { "Պարտք" },
    };

    public static bool IsValid(CashMovementType type, string? category) =>
        category != null && All[type].Contains(category);
}

/// <summary>Banknotes counted when closing the cash register (section 3).</summary>
public static class Banknotes
{
    public static readonly int[] Denominations = { 1000, 2000, 5000, 10000, 20000, 50000 };
}
