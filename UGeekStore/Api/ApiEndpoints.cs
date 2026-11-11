using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Api;

public static class ApiEndpoints
{
    public record MissedCallDto(string Phone, DateTimeOffset? CalledAt, string? Source);

    public static void MapApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // Confectioner workload for a given day (order form executor picker).
        api.MapGet("/workload", async (DateTime? date, WorkloadService w, Clock clock) =>
        {
            var list = await w.ForDayAsync(date ?? clock.Today);
            return Results.Ok(list.Select(l => new { l.Id, l.Label, l.Level, l.ActiveOrders, l.OrdersOnDay }));
        }).RequireAuthorization();

        // Customer lookup by phone: shows past issues when taking a new order.
        api.MapGet("/customers/lookup", async (string phone, AppDbContext db) =>
        {
            var p = Phone.Normalize(phone);
            var c = await db.Customers.Include(x => x.Issues).FirstOrDefaultAsync(x => x.Phone == p);
            if (c == null) return Results.Ok(new { found = false });
            var orders = await db.Orders.CountAsync(o => o.CustomerId == c.Id);
            return Results.Ok(new
            {
                found = true, c.Id, c.Name, c.Address, c.District, c.Notes, orders,
                issues = c.Issues.OrderByDescending(i => i.CreatedAt)
                    .Select(i => new { date = i.CreatedAt.ToString("dd.MM.yyyy"), i.Description, i.Resolved })
            });
        }).RequireAuthorization();

        // Missed-call webhook for the telephony provider (Vivacell / PBX). Header: X-Api-Key.
        api.MapPost("/missed-calls", async (HttpRequest req, MissedCallDto dto, AppDbContext db, Clock clock, IConfiguration cfg) =>
        {
            var key = cfg["Telephony:ApiKey"];
            if (string.IsNullOrEmpty(key) || req.Headers["X-Api-Key"] != key) return Results.Unauthorized();
            var phone = Phone.Normalize(dto.Phone);
            if (phone.Length < 6) return Results.BadRequest("phone");
            db.MissedCalls.Add(new MissedCall
            {
                Phone = phone,
                CalledAt = dto.CalledAt.HasValue ? clock.ToLocal(dto.CalledAt.Value) : clock.Now,
                Source = string.IsNullOrWhiteSpace(dto.Source) ? "telephony" : dto.Source!,
            });
            await db.SaveChangesAsync();
            return Results.Ok();
        }).AllowAnonymous();
    }
}
