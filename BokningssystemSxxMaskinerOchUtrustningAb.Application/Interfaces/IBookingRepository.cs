using BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.Interfaces;

public interface IBookingRepository
{
    BookingAvailability GetItemsAndBookings(Guid modelId);

    bool TrySaveBooking(Booking booking);
}
