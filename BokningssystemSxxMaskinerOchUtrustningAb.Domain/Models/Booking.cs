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
        DateOnly startDate, DateOnly endDate, decimal agreedTotalPrice,
        BookingStatus status = BookingStatus.Confirmed)
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
            throw new ArgumentOutOfRangeException(nameof(agreedTotalPrice),
                "Totalpriset får inte vara negativt.");

        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status),
                "Bokningsstatusen är ogiltig.");

        Id = id;
        BookingNumber = bookingNumber;
        CustomerId = customerId;
        EquipmentItemId = equipmentItemId;
        StartDate = startDate;
        EndDate = endDate;
        AgreedTotalPrice = agreedTotalPrice;
        Status = status;
    }

    // F05 - Avboka en bekräftad bokning
    public bool Cancel()
    {
        // Endast bekräftade bokningar får avbokas.
        if (Status != BookingStatus.Confirmed)
            return false;

        // Ändra bokningens status.
        Status = BookingStatus.Cancelled;

        return true;
    }

    public bool BlocksPeriodFor(Guid customerId, DateOnly startDate, DateOnly endDate)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Kundens id måste anges.", nameof(customerId));

        if (endDate < startDate)
            throw new ArgumentException("Slutdatum får inte vara före startdatum.", nameof(endDate));

        // Avbokade bokningar blockerar inte utrustningen.
        if (Status == BookingStatus.Cancelled)
            return false;

        if (StartDate <= endDate && startDate <= EndDate)
            return true;

        if (CustomerId == customerId)
            return false;

        return BlocksWeekendAfterFriday(EndDate, startDate)
            || BlocksWeekendAfterFriday(endDate, StartDate);
    }

    private static bool BlocksWeekendAfterFriday(DateOnly earlierEnd, DateOnly laterStart) =>
        earlierEnd.DayOfWeek == DayOfWeek.Friday
        && laterStart.DayNumber > earlierEnd.DayNumber
        && laterStart.DayNumber - earlierEnd.DayNumber < 3;
}