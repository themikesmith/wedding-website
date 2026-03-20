namespace WeddingWebsite.Core;

public class PrivateMediaOptions
{
    public string RootPath { get; set; } = "PrivatePhotos";
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
    public long? MaxFileSizeBytes { get; set; }
}
