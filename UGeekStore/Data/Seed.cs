using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;

namespace UGeekStore.Data;

public static class Seed
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Employees.AnyAsync())
        {
            var admin = new Employee
            {
                FullName = "Ադմինիստրատոր",
                Email = cfg["Seed:AdminEmail"] ?? "admin@local",
                Role = UserRole.Admin,
            };
            admin.PasswordHash = new PasswordHasher<Employee>().HashPassword(admin, cfg["Seed:AdminPassword"] ?? "admin123");
            db.Employees.Add(admin);
        }

        if (!await db.CashRegisters.AnyAsync())
            db.CashRegisters.Add(new CashRegister { Name = "Հիմնական դրամարկղ" });

        if (!await db.ChecklistItems.AnyAsync())
        {
            db.ChecklistItems.AddRange(
                new ChecklistItem { Title = "Աշխատանքային տարածքի մաքրում", RequiresMediaReport = true, SortOrder = 1 },
                new ChecklistItem { Title = "Սառնարանների ջերմաստիճանի ստուգում", Role = UserRole.Confectioner, SortOrder = 2 },
                new ChecklistItem { Title = "Ցուցափեղկի դասավորում", Role = UserRole.Cashier, RequiresMediaReport = true, SortOrder = 3 },
                new ChecklistItem { Title = "Բաց թողնված զանգերին հետզանգ", Role = UserRole.Cashier, SortOrder = 4 });
        }

        await db.SaveChangesAsync();
    }
}
