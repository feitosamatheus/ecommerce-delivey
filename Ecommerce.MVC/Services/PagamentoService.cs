namespace Ecommerce.MVC.Services;

public class PagamentoService
{
    private readonly HttpClient _http;

    public PagamentoService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> CriarPagamentoAsync(object pagamento)
    {
        var response = await _http.PostAsJsonAsync("/v3/payments", pagamento);
        return await response.Content.ReadAsStringAsync();
    }
}
