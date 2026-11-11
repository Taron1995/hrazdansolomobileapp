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
            var credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);
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

    /// <returns>true when the row was written.</returns>
    public async Task<bool> AppendAsync(string sheetName, params object?[] values)
    {
        if (_service == null) return false;
        try
        {
            var body = new ValueRange { Values = new List<IList<object?>> { values.ToList() } };
            var req = _service.Spreadsheets.Values.Append(body, _spreadsheetId, $"{sheetName}!A1");
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
