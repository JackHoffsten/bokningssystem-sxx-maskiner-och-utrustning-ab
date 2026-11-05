namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;

// F05 - Möjliga fel vid avbokning
public enum CancellationError
{
    None,
    InvalidRequest,
    NotFound,
    AlreadyCancelled,
    RentalStarted,
    TooLate,
    SaveFailed
}