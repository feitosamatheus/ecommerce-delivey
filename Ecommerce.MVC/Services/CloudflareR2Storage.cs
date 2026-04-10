
using Amazon.S3;
using Amazon.S3.Model;
using Ecommerce.MVC.Interfaces;

namespace Ecommerce.MVC.Services;

public class CloudflareR2Storage : IFileStorage
{
    private readonly IConfiguration _config;

    public CloudflareR2Storage(IConfiguration config)
    {
        _config = config;
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        var accessKey = _config["CloudflareR2:AccessKey"];
        var secretKey = _config["CloudflareR2:SecretKey"];
        var bucket = _config["CloudflareR2:Bucket"];
        var accountId = _config["CloudflareR2:AccountId"];

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com"
        };

        using var client = new AmazonS3Client(accessKey, secretKey, config);
        var extensao = Path.GetExtension(file.FileName);

        var fileName = $"{Guid.NewGuid()}{extensao}";

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = fileName,
            InputStream = file.OpenReadStream(),
            ContentType = file.ContentType,
            DisablePayloadSigning = true
        };

        await client.PutObjectAsync(request);

        return $"{fileName}";
    }
}
