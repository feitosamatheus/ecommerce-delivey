namespace Ecommerce.MVC.Helpers;

public class ServiceResult<T>
{
    public bool Success { get; private set; }
    public string Message { get; private set; }
    public T Data { get; private set; }

    public static ServiceResult<T> Ok(T data, string message = null)
        => new ServiceResult<T> { Success = true, Data = data, Message = message };

    public static ServiceResult<T> Fail(string message)
        => new ServiceResult<T> { Success = false, Message = message };
}
