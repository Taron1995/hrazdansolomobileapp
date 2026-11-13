using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace UGeekStore.Services;

/// <summary>
/// Appends rows to a Google Sheet (cash movements, register closings).
/// Configure GoogleSheets:SpreadsheetId and GoogleSheets:ServiceAccountKeyPath; share the sheet with the service account e-mail.
/// When not configured, calls are no-ops and the data stays in the local database only.
/// </summary>
public class GoogleSheetsService
{
    private readonly ILogger<GoogleSheetsService> _log;
    private readonly string? _spreadsheetId;
    private readonly SheetsService? _service;

    public GoogleSheetsService(IConfiguration cfg, ILogger<GoogleSheetsService> log)
    {
        _log = log;
        _spreadsheetId = cfg["GoogleSheets:SpreadsheetId"];
        var keyPath = cfg["GoogleSheets:ServiceAccountKeyPath"];
        if (string.IsNullOrWhiteSpace(_spreadsheetId) || string.IsNullOrWhiteSpace(keyPath) || !File.Exists(keyPath))
            return;
        try
        {
            using var stream = File.OpenRead(keyPath);
            var credential = CredentialFactory.FromStream<ServiceAccountCredential>(stream).ToGoogleCredential().CreateScoped(SheetsService.Scope.Spreadsheets);
            _service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "UGeekStore",
            });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Google Sheets initialization failed");
        }
    }

    public bool IsConfigured => _service != null;

    private readonly HashSet<string> _ready = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>A1 range for a sheet; names with spaces or non-Latin letters must be quoted.</summary>
    private static string Range(string sheetName, string cells) => $"'{sheetName.Replace("'", "''")}'!{cells}";

    /// <summary>Creates the tab if it does not exist and writes the header row if the tab is empty.</summary>
    private async Task EnsureSheetAsync(string sheetName, IList<object?> header)
    {
        if (_ready.Contains(sheetName)) return;
        await _lock.WaitAsync();
        try
        {
            if (_ready.Contains(sheetName)) return;
            var meta = await _service!.Spreadsheets.Get(_spreadsheetId).ExecuteAsync();
            if (!meta.Sheets.Any(sh => sh.Properties.Title == sheetName))
            {
                await _service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
                {
                    Requests = new List<Request> { new() { AddSheet = new AddSheetRequest { Properties = new SheetProperties { Title = sheetName } } } }
                }, _spreadsheetId).ExecuteAsync();
            }
            var first = await _service.Spreadsheets.Values.Get(_spreadsheetId, Range(sheetName, "A1:A1")).ExecuteAsync();
            if (first.Values == null || first.Values.Count == 0)
            {
                var req = _service.Spreadsheets.Values.Update(new ValueRange { Values = new List<IList<object?>> { header } },
                    _spreadsheetId, Range(sheetName, "A1"));
                req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await req.ExecuteAsync();
            }
            _ready.Add(sheetName);
        }
        finally { _lock.Release(); }
    }

    /// <returns>true when the row was written.</returns>
    public async Task<bool> AppendAsync(string sheetName, IList<object?> header, params object?[] values)
    {
        if (_service == null) return false;
        try
        {
            await EnsureSheetAsync(sheetName, header);
            var body = new ValueRange { Values = new List<IList<object?>> { values.ToList() } };
            var req = _service.Spreadsheets.Values.Append(body, _spreadsheetId, Range(sheetName, "A1"));
            req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            await req.ExecuteAsync();
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Google Sheets append failed for {Sheet}", sheetName);
            return false;
        }
    }
}
