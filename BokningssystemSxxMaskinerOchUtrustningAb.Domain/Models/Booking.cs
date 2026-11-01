using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Enums;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

public class Booking
{
    public Guid Id { get; private set; }
    public string BookingNumber { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid EquipmentItemId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal AgreedTotalPrice { get; private set; }
    public BookingStatus Status { get; private set; }

    public Booking(Guid id, string bookingNumber, Guid customerId, Guid equipmentItemId,
        DateOnly startDate, DateOnly endDate, decimal agreedTotalPrice)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Bokningens id måste anges.", nameof(id));
        if (customerId == Guid.Empty)
            throw new ArgumentException("Bokningen måste tillhöra en kund.", nameof(customerId));
        if (equipmentItemId == Guid.Empty)
            throw new ArgumentException("Bokningen måste gälla ett exemplar.", nameof(equipmentItemId));
        ArgumentException.ThrowIfNullOrWhiteSpace(bookingNumber);
        if (endDate < startDate)
            throw new ArgumentException("Slutdatum får inte vara före startdatum.", nameof(endDate));
        if (agreedTotalPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(agreedTotalPrice), "Totalpriset får inte vara negativt.");

        Id = id;
        BookingNumber = bookingNumber;
        CustomerId = customerId;
        EquipmentItemId = equipmentItemId;
        StartDate = startDate;
        EndDate = endDate;
        AgreedTotalPrice = agreedTotalPrice;
        Status = BookingStatus.Confirmed;
    }
}
