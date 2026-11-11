namespace UGeekStore.Services;

/// <summary>Server-side clock in the business time zone (Asia/Yerevan by default). The phone cannot change it.</summary>
public class Clock
{
    private readonly TimeZoneInfo _tz;

    public Clock(IConfiguration cfg)
    {
        var id = cfg["App:TimeZone"] ?? "Asia/Yerevan";
        try { _tz = TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch { _tz = TimeZoneInfo.CreateCustomTimeZone("AMT", TimeSpan.FromHours(4), "Yerevan", "Yerevan"); }
    }

    public DateTime Now => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz), DateTimeKind.Unspecified);
    public DateTime Today => Now.Date;

    public DateTime ToLocal(DateTimeOffset dto) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(dto.UtcDateTime, _tz), DateTimeKind.Unspecified);
}
