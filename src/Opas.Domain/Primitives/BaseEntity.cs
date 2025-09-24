using Opas.Domain.Primitives; // IDomainEvent burada artık
namespace Opas.Domain.Primitives;

public abstract class BaseEntity : Entity
{
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }

    public void MarkUpdated() => UpdatedAtUtc = DateTime.UtcNow;

    public void SoftDelete()
    {
        if (!IsDeleted)
        {
            IsDeleted = true;
            MarkUpdated();
        }
    }

    public void Restore()
    {
        if (IsDeleted)
        {
            IsDeleted = false;
            MarkUpdated();
        }
    }
}
