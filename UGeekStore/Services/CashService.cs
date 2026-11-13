using Microsoft.EntityFrameworkCore;
using UGeekStore.Core;
using UGeekStore.Core.Entities;
using UGeekStore.Data;

namespace UGeekStore.Services;

public record ShiftTotals(decimal Income, decimal Expense, decimal Debt)
{
    public decimal Net => Income - Expense;
}

public class CashService
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    private readonly GoogleSheetsService _sheets;
    private readonly IConfiguration _cfg;

    public CashService(AppDbContext db, Clock clock, GoogleSheetsService sheets, IConfiguration cfg)
    { _db = db; _clock = clock; _sheets = sheets; _cfg = cfg; }

    private static readonly object?[] CashHeader =
        { "Ամսաթիվ", "Ժամ", "Դրամարկղ", "Գործարքի տեսակ", "Տեսակ", "Տարբերակ", "Գումար", "Մեկնաբանություն", "Գանձապահ", "Հեռախոսի ժամ" };

    private static readonly object?[] ClosingHeader =
        { "Ամսաթիվ", "Դրամարկղ", "1000", "2000", "5000", "10000", "20000", "50000", "Ընդհանուր (հաշված)", "Մուտք", "Ելք", "Պարտք", "Փակող", "Ժամ", "Մեկնաբանություն" };

    public Task<List<CashRegister>> RegistersAsync() =>
        _db.CashRegisters.Where(r => r.IsActive).OrderBy(r => r.Id).ToListAsync();

    public async Task<CashRegister?> ResolveRegisterAsync(int? id) =>
        id.HasValue
            ? await _db.CashRegisters.FirstOrDefaultAsync(r => r.Id == id && r.IsActive)
            : await _db.CashRegisters.Where(r => r.IsActive).OrderBy(r => r.Id).FirstOrDefaultAsync();

    public Task<CashShift?> OpenShiftAsync(int registerId) =>
        _db.CashShifts.Include(s => s.OpenedBy).Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.CashRegisterId == registerId && s.Status == ShiftStatus.Open);

    /// <summary>Returns the open shift or opens a new one. The shift date is taken from the server clock and is never editable.</summary>
    public async Task<CashShift> GetOrOpenShiftAsync(int registerId, int employeeId)
    {
        var shift = await OpenShiftAsync(registerId);
        if (shift != null) return shift;
        var now = _clock.Now;
        shift = new CashShift { CashRegisterId = registerId, Date = now.Date, OpenedAt = now, OpenedById = employeeId };
        _db.CashShifts.Add(shift);
        await _db.SaveChangesAsync();
        return shift;
    }

    public static ShiftTotals Totals(IEnumerable<CashTransaction> txs)
    {
        var list = txs.Where(t => !t.IsVoided).ToList();
        return new ShiftTotals(
            list.Where(t => t.Type == CashMovementType.Income).Sum(t => t.Amount),
            list.Where(t => t.Type == CashMovementType.Expense).Sum(t => t.Amount),
            list.Where(t => t.Type == CashMovementType.Debt).Sum(t => t.Amount));
    }

    public async Task AddTransactionAsync(CashShift shift, CashTransaction tx)
    {
        tx.CashShiftId = shift.Id;
        tx.CreatedAt = _clock.Now;
        _db.CashTransactions.Add(tx);
        await _db.SaveChangesAsync();

        var register = await _db.CashRegisters.FindAsync(shift.CashRegisterId);
        tx.SyncedToSheet = await _sheets.AppendAsync(_cfg["GoogleSheets:CashSheet"] ?? "Cash", CashHeader,
            shift.Date.ToString("dd.MM.yyyy"), tx.CreatedAt.ToString("HH:mm:ss"), register?.Name,
            "Կանխիկի շարժ", tx.Type.Hy(), tx.Category, tx.Amount, tx.Comment, tx.CashierName,
            tx.DeviceTime?.ToString("dd.MM.yyyy HH:mm:ss"));
        if (tx.SyncedToSheet) await _db.SaveChangesAsync();
    }

    public async Task CloseShiftAsync(CashShift shift, int employeeId, int[] counts, string? comment)
    {
        shift.Count1000 = counts[0]; shift.Count2000 = counts[1]; shift.Count5000 = counts[2];
        shift.Count10000 = counts[3]; shift.Count20000 = counts[4]; shift.Count50000 = counts[5];
        shift.CountedTotal = Banknotes.Denominations.Zip(counts, (d, c) => (decimal)d * c).Sum();
        shift.CloseComment = comment;
        shift.ClosedAt = _clock.Now;
        shift.ClosedById = employeeId;
        shift.Status = ShiftStatus.Closed;
        await _db.SaveChangesAsync();

        var t = Totals(shift.Transactions);
        var register = await _db.CashRegisters.FindAsync(shift.CashRegisterId);
        var closer = await _db.Employees.FindAsync(employeeId);
        var row = new List<object?> { shift.Date.ToString("dd.MM.yyyy"), register?.Name };
        row.AddRange(counts.Cast<object?>());
        row.AddRange(new object?[] { shift.CountedTotal, t.Income, t.Expense, t.Debt, closer?.FullName, shift.ClosedAt?.ToString("HH:mm"), comment });
        await _sheets.AppendAsync(_cfg["GoogleSheets:ClosingSheet"] ?? "Closing", ClosingHeader, row.ToArray());
    }
}
