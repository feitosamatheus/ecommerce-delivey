using Ecommerce.MVC.Models.Horarios;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.MVC.Components;

public class HorarioFuncionamentoViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var agora = DateTime.Now;
        var diaSemana = agora.DayOfWeek;
        var horaAtual = agora.TimeOfDay;
        bool aberto = false;

        // Lógica de horários
        switch (diaSemana)
        {
            case DayOfWeek.Saturday:
                if (horaAtual >= new TimeSpan(11, 0, 0) && horaAtual < new TimeSpan(23, 0, 0))
                    aberto = true;
                break;
            case DayOfWeek.Sunday:
                if (horaAtual >= new TimeSpan(16, 0, 0) && horaAtual < new TimeSpan(22, 0, 0))
                    aberto = true;
                break;
            default: // Segunda a Sexta
                if (horaAtual >= new TimeSpan(11, 0, 0) && horaAtual < new TimeSpan(22, 0, 0))
                    aberto = true;
                break;
        }

        var model = new HorarioStatusViewModel
        {
            EstaAberto = aberto,
            DiaAtual = diaSemana
        };

        return View(model);
    }
}
