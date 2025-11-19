namespace R2.ShopNet.Identity.Application.DTOs.Passkey;

public class PasskeyRegistrationOptions
{
    public string Challenge { get; set; } = string.Empty;
    public string RpId { get; set; } = string.Empty;
    public string RpName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60000;
    public string Attestation { get; set; } = "none";
    public List<PublicKeyCredentialParameters> PubKeyCredParams { get; set; } = new();
    public AuthenticatorSelectionCriteria AuthenticatorSelection { get; set; } = new();
    public List<PublicKeyCredentialDescriptor> ExcludeCredentials { get; set; } = new();
}

public class PublicKeyCredentialParameters
{
    public string Type { get; set; } = "public-key";
    public int Alg { get; set; } // COSE algorithm identifier
}

public class AuthenticatorSelectionCriteria
{
    public string? AuthenticatorAttachment { get; set; } // "platform" or "cross-platform"
    public string ResidentKey { get; set; } = "preferred";
    public bool RequireResidentKey { get; set; } = false;
    public string UserVerification { get; set; } = "preferred";
}

public class PublicKeyCredentialDescriptor
{
    public string Type { get; set; } = "public-key";
    public string Id { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Transports { get; set; }
}

public class PasskeyRegistrationResponse
{
    public string Id { get; set; } = string.Empty;
    public string RawId { get; set; } = string.Empty;
    public string Type { get; set; } = "public-key";
    public AuthenticatorAttestationResponse Response { get; set; } = new();
}

public class AuthenticatorAttestationResponse
{
    public string ClientDataJSON { get; set; } = string.Empty;
    public string AttestationObject { get; set; } = string.Empty;
}

public class PasskeyRegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CredentialId { get; set; }
}
