namespace BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

public class Customer
{
    public Guid Id { get; private set; }

    public Customer(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Kundens id måste anges.", nameof(id));

        Id = id;
    }
}
