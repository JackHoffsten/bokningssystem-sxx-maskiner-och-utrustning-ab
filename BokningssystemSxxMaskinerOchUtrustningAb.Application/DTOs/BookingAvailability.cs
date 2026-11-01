using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;

public record BookingAvailability(
    IReadOnlyList<EquipmentItem> Items,
    IReadOnlyList<Booking> Bookings
);
