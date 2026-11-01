namespace BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

public class EquipmentModel
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public decimal DailyPrice { get; private set; }

    public EquipmentModel(Guid id, string name, string description, string category, decimal dailyPrice)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Modellens id måste anges.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        if (dailyPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(dailyPrice), "Dagspriset får inte vara negativt.");

        Id = id;
        Name = name;
        Description = description;
        Category = category;
        DailyPrice = dailyPrice;
    }
}
