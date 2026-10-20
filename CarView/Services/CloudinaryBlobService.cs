using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using CarView.Models;

namespace CarView.Services
{
    public class CloudinaryBlobService : IBlobStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryBlobService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "carview_cars",
                Transformation = new Transformation().Quality("auto").FetchFormat("auto") 
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl?.ToString();
        }

        public async Task<string?> UploadAudioAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "carview_audio"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl?.ToString();
        }
    }
}

