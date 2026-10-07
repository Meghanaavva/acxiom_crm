using AcxiomCRM.Api.Data;
using AcxiomCRM.Api.Helpers;
using AcxiomCRM.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Api.Services;

public interface IPasswordHistoryService
{
    // Throws a validation error if the new password is the current one or one of the recent ones
    Task EnsureNotReusedAsync(ApplicationUser user, string newPassword, string field);

    // Call after a successful change/reset with the hash the user had BEFORE the change
    Task SaveAsync(int userId, string? oldPasswordHash);
}

public class PasswordHistoryService : IPasswordHistoryService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<ApplicationUser> _hasher;
    private readonly int _historyCount;

    public PasswordHistoryService(ApplicationDbContext db, IPasswordHasher<ApplicationUser> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _historyCount = config.GetValue("Security:PasswordHistoryCount", 5);
    }

    public async Task EnsureNotReusedAsync(ApplicationUser user, string newPassword, string field)
    {
        var hashes = await _db.PasswordHistories
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedDate)
            .Take(_historyCount - 1)
            .Select(p => p.PasswordHash)
            .ToListAsync();
        if (!string.IsNullOrEmpty(user.PasswordHash)) hashes.Insert(0, user.PasswordHash);

        foreach (var hash in hashes)
        {
            // Identity compares the password with the stored hash, we never see the old password
            if (_hasher.VerifyHashedPassword(user, hash, newPassword) != PasswordVerificationResult.Failed)
            {
                throw AppException.Field(field, $"You cannot reuse any of your last {_historyCount} passwords.");
            }
        }
    }

    public async Task SaveAsync(int userId, string? oldPasswordHash)
    {
        if (string.IsNullOrEmpty(oldPasswordHash)) return;

        _db.PasswordHistories.Add(new PasswordHistory { UserId = userId, PasswordHash = oldPasswordHash });
        await _db.SaveChangesAsync();

        // keep only the latest entries
        var old = await _db.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedDate)
            .Skip(_historyCount)
            .ToListAsync();
        if (old.Count > 0)
        {
            _db.PasswordHistories.RemoveRange(old);
            await _db.SaveChangesAsync();
        }
    }
}
