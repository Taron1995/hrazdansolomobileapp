using System.Security.Claims;

namespace UGeekStore.Services;

public static class UserExtensions
{
    public static int EmployeeId(this ClaimsPrincipal u) =>
        int.TryParse(u.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static string FullName(this ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.Name) ?? "";

    public static bool IsAny(this ClaimsPrincipal u, string rolesCsv) =>
        rolesCsv.Split(',').Any(u.IsInRole);
}
