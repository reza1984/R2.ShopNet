using System.Text.Json.Serialization;
namespace R2.ShopNet.Identity.Application.DTOs;

public class PasskeyAuthenticationOptions
{
    public string Challenge { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60000;
    public List<PublicKeyCredentialDescriptor> AllowCredentials { get; set; } = new();
    public string UserVerification { get; set; } = "preferred";
}


public class PasskeyAuthenticationResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("rawId")]
    public string RawId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "public-key";

    [JsonPropertyName("response")]
    public AuthenticatorAssertionResponse Response { get; set; } = new();

    [JsonPropertyName("clientExtensionResults")]
    public object? ClientExtensionResults { get; set; }
}

public class AuthenticatorAssertionResponse
{
    [JsonPropertyName("clientDataJSON")]
    public string ClientDataJSON { get; set; } = string.Empty;

    [JsonPropertyName("authenticatorData")]
    public string AuthenticatorData { get; set; } = string.Empty;

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("userHandle")]
    public string? UserHandle { get; set; }
}

public class PasskeyAuthenticationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
}

public class PasskeyCredentialDto
{
    public Guid Id { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
