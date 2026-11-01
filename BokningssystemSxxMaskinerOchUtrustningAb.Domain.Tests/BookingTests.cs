using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Enums;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;
using Xunit;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Domain.Tests;

public class BookingTests
{
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherCustomerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // Januari 2030: tisdag 1, fredag 4, lördag 5, söndag 6, måndag 7.
    private static DateOnly Day(int day) => new(2030, 1, day);

    [Theory]
    [InlineData(1, 2)] // Delar första dagen.
    [InlineData(3, 4)] // Delar sista dagen.
    [InlineData(2, 3)] // Samma period.
    [InlineData(2, 2)] // Endagsbokning inuti perioden.
    [InlineData(1, 4)] // Omsluter befintlig period.
    public void Overlapping_periods_are_blocked_including_boundary_days(int startDay, int endDay)
    {
        var booking = CreateBooking(2, 3);

        Assert.True(booking.BlocksPeriodFor(OtherCustomerId, Day(startDay), Day(endDay)));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    public void Adjacent_periods_are_available_when_friday_rule_does_not_apply(int startDay, int endDay)
    {
        var booking = CreateBooking(2, 3);

        Assert.False(booking.BlocksPeriodFor(OtherCustomerId, Day(startDay), Day(endDay)));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(12, false)] // Inte varje efterföljande helg.
    public void Booking_ending_friday_blocks_another_customer_until_monday(int nextStart, bool expected)
    {
        var booking = CreateBooking(3, 4);

        Assert.Equal(expected, booking.BlocksPeriodFor(OtherCustomerId, Day(nextStart), Day(nextStart)));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    public void Friday_rule_also_applies_when_weekend_booking_already_exists(int existingStart, bool expected)
    {
        var booking = CreateBooking(existingStart, existingStart);

        Assert.Equal(expected, booking.BlocksPeriodFor(OtherCustomerId, Day(3), Day(4)));
    }

    [Fact]
    public void Friday_rule_allows_same_customer_to_book_a_separate_weekend_period()
    {
        var booking = CreateBooking(3, 4);

        Assert.False(booking.BlocksPeriodFor(CustomerId, Day(5), Day(6)));
        Assert.True(booking.BlocksPeriodFor(CustomerId, Day(4), Day(5)));
    }

    [Theory]
    [InlineData(3, 4)] // Överlapp.
    [InlineData(5, 6)] // Helgregel.
    public void Cancelled_booking_never_blocks_a_valid_period(int startDay, int endDay)
    {
        var booking = CreateBooking(3, 4, BookingStatus.Cancelled);

        Assert.False(booking.BlocksPeriodFor(OtherCustomerId, Day(startDay), Day(endDay)));
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Returned)]
    public void Non_cancelled_booking_preserves_its_agreed_period(BookingStatus status)
    {
        var booking = CreateBooking(2, 3, status);

        Assert.True(booking.BlocksPeriodFor(OtherCustomerId, Day(2), Day(3)));
    }

    [Fact]
    public void Single_day_booking_blocks_the_same_calendar_day()
    {
        var booking = CreateBooking(2, 2);

        Assert.True(booking.BlocksPeriodFor(OtherCustomerId, Day(2), Day(2)));
        Assert.False(booking.BlocksPeriodFor(OtherCustomerId, Day(3), Day(3)));
    }

    [Fact]
    public void Invalid_requested_period_is_rejected()
    {
        var booking = CreateBooking(2, 3);

        Assert.Throws<ArgumentException>(() => booking.BlocksPeriodFor(OtherCustomerId, Day(3), Day(2)));
    }

    [Fact]
    public void Empty_customer_identity_is_rejected()
    {
        var booking = CreateBooking(2, 3);

        Assert.Throws<ArgumentException>(() => booking.BlocksPeriodFor(Guid.Empty, Day(2), Day(3)));
    }

    [Fact]
    public void New_booking_is_confirmed_by_default()
    {
        Assert.Equal(BookingStatus.Confirmed, CreateBooking(2, 3).Status);
    }

    [Fact]
    public void Undefined_status_cannot_be_used_for_existing_booking()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateBooking(2, 3, (BookingStatus)999));
    }

    private static Booking CreateBooking(int startDay, int endDay, BookingStatus status = BookingStatus.Confirmed) =>
        new(Guid.NewGuid(), "SXX-TEST-001", CustomerId, Guid.NewGuid(), Day(startDay), Day(endDay), 500m, status);
}
