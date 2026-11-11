using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Pages.Orders;

[Authorize(Roles = Roles.CashDesk)]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    private readonly WorkloadService _workload;
    public CreateModel(AppDbContext db, Clock clock, WorkloadService workload) { _db = db; _clock = clock; _workload = workload; }

    public class ItemInput
    {
        public int? ProductId { get; set; }
        public string? Name { get; set; }
        public decimal Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class OrderInput
    {
        public OrderType Type { get; set; } = OrderType.Individual;
        [Required(ErrorMessage = "Հեռախոսը պարտադիր է")] public string Phone { get; set; } = "";
        [Required(ErrorMessage = "Անունը պարտադիր է")] public string CustomerName { get; set; } = "";
        public string? Description { get; set; }
        public List<ItemInput> Items { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public decimal Prepayment { get; set; }
        [Required] public DateTime DueDate { get; set; }
        [Required] public TimeSpan DueTime { get; set; } = new(12, 0, 0);
        public int? ExecutorId { get; set; }
        public bool IsDelivery { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryDistrict { get; set; }
        public decimal DeliveryFee { get; set; }
        public int? CourierId { get; set; }
    }

    [BindProperty] public OrderInput Input { get; set; } = new();
    public List<ExecutorLoad> Executors { get; set; } = new();
    public List<Employee> Couriers { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<string> Districts { get; set; } = new();

    private async Task LoadAsync()
    {
        Executors = await _workload.ForDayAsync(Input.DueDate);
        Couriers = await _db.Employees.Where(e => e.IsActive && (e.Role == UserRole.Courier || e.Role == UserRole.Manager || e.Role == UserRole.Cashier))
            .OrderBy(e => e.Role).ThenBy(e => e.FullName).ToListAsync();
        Products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        Districts = await _db.Customers.Where(c => c.District != null).Select(c => c.District!).Distinct().OrderBy(d => d).ToListAsync();
    }

    public async Task OnGetAsync(string? phone)
    {
        Input.DueDate = _clock.Today.AddDays(1);
        if (phone != null) Input.Phone = phone;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Items = Input.Items.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
        var phone = Phone.Normalize(Input.Phone);
        if (phone.Length < 8) ModelState.AddModelError("Input.Phone", "Սխալ հեռախոսահամար");
        if (Input.Items.Count == 0 && string.IsNullOrWhiteSpace(Input.Description))
            ModelState.AddModelError("", "Ավելացրեք ապրանք կամ նկարագրություն");
        if (Input.IsDelivery && string.IsNullOrWhiteSpace(Input.DeliveryAddress))
            ModelState.AddModelError("Input.DeliveryAddress", "Առաքման հասցեն պարտադիր է");
        if (!ModelState.IsValid) { await LoadAsync(); return Page(); }

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
        if (customer == null)
        {
            customer = new Customer { Phone = phone, Name = Input.CustomerName.Trim(), CreatedAt = _clock.Now };
            _db.Customers.Add(customer);
        }
        if (Input.IsDelivery)
        {
            customer.Address ??= Input.DeliveryAddress?.Trim();
            customer.District ??= Input.DeliveryDistrict?.Trim();
        }

        var order = new Order
        {
            Type = Input.Type,
            Customer = customer,
            CreatedAt = _clock.Now,
            CreatedById = User.EmployeeId(),
            DueAt = Input.DueDate.Date + Input.DueTime,
            ExecutorId = Input.ExecutorId,
            Description = Input.Description?.Trim(),
            TotalPrice = Input.TotalPrice > 0 ? Input.TotalPrice : Input.Items.Sum(i => i.Quantity * i.UnitPrice),
            Prepayment = Input.Prepayment,
            IsDelivery = Input.IsDelivery,
            DeliveryAddress = Input.IsDelivery ? Input.DeliveryAddress?.Trim() : null,
            DeliveryDistrict = Input.IsDelivery ? Input.DeliveryDistrict?.Trim() : null,
            DeliveryFee = Input.IsDelivery ? Input.DeliveryFee : 0,
            CourierId = Input.IsDelivery ? Input.CourierId : null,
            Items = Input.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId, Name = i.Name!.Trim(), Quantity = i.Quantity <= 0 ? 1 : i.Quantity, UnitPrice = i.UnitPrice
            }).ToList(),
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        TempData["ok"] = $"Պատվեր #{order.Id} գրանցված է";
        return Redirect($"/Orders/Details/{order.Id}");
    }
}
