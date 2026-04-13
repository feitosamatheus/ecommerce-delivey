using System;

namespace Ecommerce.Domain.Commons;

public abstract class EntidadeBase
{
    public Guid Id { get; protected set; }
    public DateTime CriadoEm { get; protected set; }
    public DateTime? AtualizadoEm { get; protected set; }

    protected EntidadeBase()
    {
        Id = Guid.NewGuid();
        CriadoEm = DateTime.UtcNow;
    }

    public void RegistrarAtualizacao()
    {
        AtualizadoEm = DateTime.UtcNow;
    }
}