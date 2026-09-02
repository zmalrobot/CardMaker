namespace CardMaker.Application.Identity;

public sealed record InvitationDto(
    Guid Id,
    string Email,
    string Token,
    string? InvitedByUserId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? UsedAtUtc,
    string? UsedByUserId,
    bool IsRevoked,
    DateTimeOffset CreatedAtUtc)
{
    public bool IsValid => !IsRevoked && !UsedAtUtc.HasValue && ExpiresAtUtc > DateTimeOffset.UtcNow;
    public string Status => IsRevoked ? "Revocato" : UsedAtUtc.HasValue ? "Utilizzato" : ExpiresAtUtc <= DateTimeOffset.UtcNow ? "Scaduto" : "Attivo";
}

public sealed class CreateInvitationRequest
{
    public required string Email { get; set; }
    public int ExpiresInDays { get; set; } = 7;
}

public sealed record ValidateInvitationResult(
    bool IsValid,
    string? ErrorMessage,
    InvitationDto? Invitation);

public interface IInvitationService
{
    Task<InvitationDto> CreateInvitationAsync(CreateInvitationRequest request, string? invitedByUserId, CancellationToken cancellationToken = default);
    Task<ValidateInvitationResult> ValidateInvitationAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ConsumeInvitationAsync(string token, string registeredUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvitationDto>> ListInvitationsAsync(CancellationToken cancellationToken = default);
    Task<bool> RevokeInvitationAsync(Guid invitationId, string? revokedByUserId, CancellationToken cancellationToken = default);
}

