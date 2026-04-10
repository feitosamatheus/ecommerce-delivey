namespace Ecommerce.MVC.Interfaces;

public interface IFileStorage
{
    Task<string> UploadAsync(IFormFile file);
}
