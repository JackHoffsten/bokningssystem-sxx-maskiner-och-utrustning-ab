namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;

public record CreateBookingRequest(
    Guid ModelId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal AgreedTotalPrice
);
