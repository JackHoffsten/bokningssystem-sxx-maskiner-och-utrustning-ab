using BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.Interfaces;

public interface IBookingRepository
{
    // Befintliga metoder från Jack
    BookingAvailability GetItemsAndBookings(Guid modelId);

    bool TrySaveBooking(Booking booking);


    // F05 - Hämta alla bokningar för en specifik kund
    IReadOnlyList<Booking> GetBookingsByCustomerId(Guid customerId);


    // F05 - Hämta en bokning via dess ID
    Booking? GetById(Guid bookingId);


    // F05 - Spara en uppdaterad bokning efter avbokning
    bool UpdateBooking(Booking booking);
}