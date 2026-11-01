using BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;
using BokningssystemSxxMaskinerOchUtrustningAb.Application.Interfaces;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.Services;

public class BookingService(IBookingRepository bookingRepository, TimeProvider timeProvider)
{
    public BookingResult CreateBooking(Guid customerId, CreateBookingRequest request)
    {
        if (customerId == Guid.Empty || request.ModelId == Guid.Empty)
            return new(false, "Kund och utrustningsmodell måste anges.", null, BookingError.InvalidRequest);

        if (!ValidateBookingPeriod(request.StartDate, request.EndDate))
            return new(false, "Startdatum får inte ligga bakåt i tiden och slutdatum får inte vara före startdatum.",
                null, BookingError.InvalidRequest);

        if (request.AgreedTotalPrice < 0)
            return new(false, "Det överenskomna priset får inte vara negativt.", null, BookingError.InvalidRequest);

        var availability = bookingRepository.GetItemsAndBookings(request.ModelId);
        var item = FindAvailableItem(availability, customerId, request);
        if (item is null)
            return new(false, "Ingen utrustning är tillgänglig under den valda perioden.", null, BookingError.Unavailable);

        var booking = new Booking(Guid.NewGuid(), GenerateBookingNumber(), customerId, item.Id,
            request.StartDate, request.EndDate, request.AgreedTotalPrice);

        if (!bookingRepository.TrySaveBooking(booking))
            return new(false, "Exemplaret hann bli otillgängligt. Försök boka igen.", null, BookingError.Unavailable);

        return new(true, "Bokning skapad.", booking);
    }

    private bool ValidateBookingPeriod(DateOnly startDate, DateOnly endDate) =>
        startDate >= DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime) && endDate >= startDate;

    private static EquipmentItem? FindAvailableItem(BookingAvailability availability, Guid customerId,
        CreateBookingRequest request) =>
        availability.Items.FirstOrDefault(item => item.EquipmentModelId == request.ModelId && item.IsBookable
            && !availability.Bookings.Any(booking => booking.EquipmentItemId == item.Id
                && booking.BlocksPeriodFor(customerId, request.StartDate, request.EndDate)));

    private static string GenerateBookingNumber() => $"SXX-{Guid.NewGuid():N}";
}
