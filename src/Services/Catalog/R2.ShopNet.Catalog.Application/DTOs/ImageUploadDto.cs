namespace R2.ShopNet.Catalog.Application.DTOs;

/// <summary>
/// DTO for image upload containing file data.
/// </summary>
public record ImageUploadDto(
    string FileName,
    byte[] FileData,
    string ContentType,
    long SizeInBytes);
