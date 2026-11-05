namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;

// F05 - Resultatet av ett avbokningsförsök
public record CancellationResult(
    bool Success,
    string Message,
    CancellationError Error = CancellationError.None
);