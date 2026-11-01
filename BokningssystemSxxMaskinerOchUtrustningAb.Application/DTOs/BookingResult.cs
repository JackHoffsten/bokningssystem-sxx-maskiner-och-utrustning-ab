using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;

public enum BookingError
{
    InvalidRequest,
    Unavailable
}

public record BookingResult(
    bool Success,
    string Message,
    Booking? Booking,
    BookingError? Error = null
);
