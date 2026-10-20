namespace CarView.Services
{
    public interface IBlobStorageService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        Task<string?> UploadAudioAsync(IFormFile file);
    }
}
