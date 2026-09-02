using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CardMaker.Application.Identity;
using CardMaker.Domain.Identity;
using CardMaker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardMaker.Infrastructure.Identity;

public sealed class InvitationService(CardMakerDbContext db) : IInvitationService
{
    public static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return "inv_" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<InvitationDto> CreateInvitationAsync(
        CreateInvitationRequest request,
        string? invitedByUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("L'indirizzo email dell'invitato è obbligatorio.", nameof(request));
        }

        var token = GenerateSecureToken();
        var tokenHash = HashToken(token);
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(Math.Clamp(request.ExpiresInDays, 1, 90));

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            TokenHash = tokenHash,
            Role = AppRoles.User,
            CreatedByUserId = invitedByUserId,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsRevoked = false,
        };

        db.Invitations.Add(invitation);

        db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = invitedByUserId,
            Action = "CreateInvitation",
            EntityName = nameof(Invitation),
            EntityId = invitation.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(new { invitation.Email, invitation.ExpiresAtUtc }),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new InvitationDto(
            invitation.Id,
            invitation.Email,
            token,
            invitation.CreatedByUserId,
            invitation.ExpiresAtUtc,
            invitation.RedeemedAtUtc,
            invitation.RedeemedByUserId,
            invitation.IsRevoked,
            invitation.CreatedAtUtc);
    }

    public async Task<ValidateInvitationResult> ValidateInvitationAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ValidateInvitationResult(false, "Token di invito non specificato.", null);
        }

        var hash = HashToken(token);
        var now = DateTimeOffset.UtcNow;

        var invitation = await db.Invitations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);

        if (invitation is null)
        {
            return new ValidateInvitationResult(false, "Token di invito non valido o inesistente.", null);
        }

        if (invitation.IsRevoked)
        {
            return new ValidateInvitationResult(false, "Questo invito è stato revocato dall'amministratore.", null);
        }

        if (invitation.RedeemedAtUtc.HasValue)
        {
            return new ValidateInvitationResult(false, "Questo invito è già stato utilizzato.", null);
        }

        if (invitation.ExpiresAtUtc <= now)
        {
            return new ValidateInvitationResult(false, "Questo invito è scaduto.", null);
        }

        var dto = new InvitationDto(
            invitation.Id,
            invitation.Email,
            token,
            invitation.CreatedByUserId,
            invitation.ExpiresAtUtc,
            invitation.RedeemedAtUtc,
            invitation.RedeemedByUserId,
            invitation.IsRevoked,
            invitation.CreatedAtUtc);

        return new ValidateInvitationResult(true, null, dto);
    }

    public async Task<bool> ConsumeInvitationAsync(
        string token,
        string registeredUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(registeredUserId))
        {
            return false;
        }

        var hash = HashToken(token);
        var now = DateTimeOffset.UtcNow;

        var invitation = await db.Invitations
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);

        if (invitation is null || invitation.IsRevoked || invitation.RedeemedAtUtc.HasValue || invitation.ExpiresAtUtc <= now)
        {
            return false;
        }

        invitation.RedeemedAtUtc = now;
        invitation.RedeemedByUserId = registeredUserId;
        invitation.UpdatedAtUtc = now;

        db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = registeredUserId,
            Action = "ConsumeInvitation",
            EntityName = nameof(Invitation),
            EntityId = invitation.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(new { RegisteredUserId = registeredUserId, invitation.Email }),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<InvitationDto>> ListInvitationsAsync(CancellationToken cancellationToken = default)
    {
        var list = await db.Invitations
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return list.Select(x => new InvitationDto(
            x.Id,
            x.Email,
            string.Empty,
            x.CreatedByUserId,
            x.ExpiresAtUtc,
            x.RedeemedAtUtc,
            x.RedeemedByUserId,
            x.IsRevoked,
            x.CreatedAtUtc)).ToList();
    }

    public async Task<bool> RevokeInvitationAsync(
        Guid invitationId,
        string? revokedByUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await db.Invitations.FindAsync([invitationId], cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.IsRevoked)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        invitation.IsRevoked = true;
        invitation.UpdatedAtUtc = now;

        db.AuditLog.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            UserId = revokedByUserId,
            Action = "RevokeInvitation",
            EntityName = nameof(Invitation),
            EntityId = invitation.Id.ToString(),
            DetailsJson = JsonSerializer.Serialize(new { invitation.Email }),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}

