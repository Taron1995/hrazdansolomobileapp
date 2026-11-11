using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UGeekStore.Core.Entities;
using UGeekStore.Data;
using UGeekStore.Services;

namespace UGeekStore.Hubs;

/// <summary>General staff chat (section 3: "ընդհանուր չատ").</summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly Clock _clock;
    public ChatHub(AppDbContext db, Clock clock) { _db = db; _clock = clock; }

    public async Task Send(string text)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return;
        if (text.Length > 2000) text = text[..2000];
        var user = Context.User!;
        var msg = new ChatMessage { EmployeeId = user.EmployeeId(), SenderName = user.FullName(), Text = text, SentAt = _clock.Now };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();
        await Clients.All.SendAsync("message", new { msg.Id, msg.SenderName, msg.Text, sentAt = msg.SentAt.ToString("dd.MM HH:mm"), msg.EmployeeId });
    }
}
