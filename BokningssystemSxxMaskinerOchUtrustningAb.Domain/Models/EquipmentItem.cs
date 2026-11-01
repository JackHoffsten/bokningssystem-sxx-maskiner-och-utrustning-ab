namespace BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

public class EquipmentItem
{
    public Guid Id { get; private set; }
    public string InventoryNumber { get; private set; }
    public Guid EquipmentModelId { get; private set; }
    public bool IsBookable { get; private set; }

    public EquipmentItem(Guid id, string inventoryNumber, Guid equipmentModelId, bool isBookable = true)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Exemplarets id måste anges.", nameof(id));
        if (equipmentModelId == Guid.Empty)
            throw new ArgumentException("Exemplaret måste tillhöra en modell.", nameof(equipmentModelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryNumber);

        Id = id;
        InventoryNumber = inventoryNumber;
        EquipmentModelId = equipmentModelId;
        IsBookable = isBookable;
    }
}
