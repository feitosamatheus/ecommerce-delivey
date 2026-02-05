namespace Ecommerce.MVC.Models.Horarios;
public class HorarioStatusViewModel
{
    public bool EstaAberto { get; set; }
    public string MensagemStatus => EstaAberto ? "Aberto Agora" : "Fechado no Momento";
    public string ClasseCss => EstaAberto ? "status-open" : "status-closed";
    public DayOfWeek DiaAtual { get; set; }
}
