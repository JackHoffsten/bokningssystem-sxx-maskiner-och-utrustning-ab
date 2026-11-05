using BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;
using BokningssystemSxxMaskinerOchUtrustningAb.Application.Interfaces;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Enums;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.Services;

public class BookingService(IBookingRepository bookingRepository, TimeProvider timeProvider)
{
    // JACK - Skapa en bokning
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
            return new(false, "Ingen utrustning är tillgänglig under den valda perioden.",
                null, BookingError.Unavailable);

        var booking = new Booking(
            Guid.NewGuid(),
            GenerateBookingNumber(),
            customerId,
            item.Id,
            request.StartDate,
            request.EndDate,
            request.AgreedTotalPrice);

        if (!bookingRepository.TrySaveBooking(booking))
            return new(false, "Exemplaret hann bli otillgängligt. Försök boka igen.",
                null, BookingError.Unavailable);

        return new(true, "Bokning skapad.", booking);
    }

    // F05 - Hämta kundens bokningar
    public IReadOnlyList<Booking> GetCustomerBookings(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return [];

        return bookingRepository.GetBookingsByCustomerId(customerId)
            .Where(booking => booking.CustomerId == customerId)
            .OrderByDescending(booking => booking.StartDate)
            .ToList();
    }

    // F05 - Avboka en bokning
    public CancellationResult CancelBooking(Guid customerId, Guid bookingId)
    {
        if (customerId == Guid.Empty || bookingId == Guid.Empty)
        {
            return new CancellationResult(
                false,
                "Kund-ID och boknings-ID måste anges.",
                CancellationError.InvalidRequest);
        }

        var booking = bookingRepository.GetById(bookingId);

        if (booking is null || booking.CustomerId != customerId)
        {
            return new CancellationResult(
                false,
                "Bokningen hittades inte.",
                CancellationError.NotFound);
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return new CancellationResult(
                false,
                "Bokningen är redan avbokad.",
                CancellationError.AlreadyCancelled);
        }

        if (booking.Status is BookingStatus.CheckedOut or BookingStatus.Returned)
        {
            return new CancellationResult(
                false,
                "Uthyrningen har redan påbörjats och kan inte avbokas.",
                CancellationError.RentalStarted);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        if (today >= booking.StartDate)
        {
            return new CancellationResult(
                false,
                "Det är för sent att avboka. Kontakta personalen.",
                CancellationError.TooLate);
        }

        // F05 - Skapa en separat kopia av bokningen.
        // Originalbokningen ska inte ändras om sparningen misslyckas.
        var bookingToCancel = new Booking(
            booking.Id,
            booking.BookingNumber,
            booking.CustomerId,
            booking.EquipmentItemId,
            booking.StartDate,
            booking.EndDate,
            booking.AgreedTotalPrice,
            booking.Status);

        if (!bookingToCancel.Cancel())
        {
            return new CancellationResult(
                false,
                "Bokningen kunde inte avbokas.",
                CancellationError.SaveFailed);
        }

        // Försök spara den avbokade kopian.
        if (!bookingRepository.UpdateBooking(bookingToCancel))
        {
            return new CancellationResult(
                false,
                "Avbokningen kunde inte sparas. Försök igen.",
                CancellationError.SaveFailed);
        }

        return new CancellationResult(
            true,
            "Bokningen har avbokats.");
    }

    // JACK - Befintliga hjälpmetoder
    private bool ValidateBookingPeriod(DateOnly startDate, DateOnly endDate) =>
        startDate >= DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime)
        && endDate >= startDate;

    private static EquipmentItem? FindAvailableItem(
        BookingAvailability availability,
        Guid customerId,
        CreateBookingRequest request) =>
        availability.Items.FirstOrDefault(item =>
            item.EquipmentModelId == request.ModelId
            && item.IsBookable
            && !availability.Bookings.Any(booking =>
                booking.EquipmentItemId == item.Id
                && booking.BlocksPeriodFor(customerId, request.StartDate, request.EndDate)));

    private static string GenerateBookingNumber() =>
        $"SXX-{Guid.NewGuid():N}";
}