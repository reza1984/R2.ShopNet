namespace R2.ShopNet.Identity.Domain.Entities;

public class PasskeyCredential
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public byte[] CredentialId { get; set; } = null!; // Raw credential ID from WebAuthn
    public byte[] PublicKey { get; set; } = null!; // COSE-encoded public key
    public uint SignCount { get; set; } // Counter for replay protection
    public Guid? AaGuid { get; set; } // Authenticator attestation GUID
    public string? DeviceName { get; set; } // User-friendly name
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}
