using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Enums;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;
using Xunit;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Domain.Tests;

public class BookingTests
{
    private static readonly Guid CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherCustomerId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // January 2030: Tuesday 1, Friday 4, Saturday 5, Sunday 6, Monday 7.
    private static DateOnly Day(int day) => new(2030, 1, day);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    [InlineData(2, 3)]
    [InlineData(2, 2)]
    [InlineData(1, 4)]
    public void Overlapping_Periods_Are_Blocked_Including_Boundary_Days(int startDay, int endDay)
    {
        var booking = CreateBooking(2, 3);

        Assert.True(booking.BlocksPeriodFor(OtherCustomerId, Day(startDay), Day(endDay)));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(4, 4)]
    public void Adjacent_Periods_Are_Available_When_Friday_Rule_Does_Not_Apply(int startDay, int endDay)
    {
        var booking = CreateBooking(2, 3);

        Assert.False(booking.BlocksPeriodFor(OtherCustomerId, Day(startDay), Day(endDay)));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    [InlineData(12, false)]
    public void Booking_Ending_Friday_Blocks_Another_Customer_Until_Monday(int nextStart, bool expected)
    {
        var booking = CreateBooking(3, 4);

        Assert.Equal(expected, booking.BlocksPeriodFor(OtherCustomerId, Day(nextStart), Day(nextStart)));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(7, false)]
    public void Friday_Rule_Also_Applies_When_Weekend_Booking_Already_Exists(int existingStart, bool expected)
    {
        var booking = CreateBooking(existingStart, existingStart);

        Assert.Equal(expected, booking.BlocksPeriodFor(OtherCustomerId, Day(3), Day(4)));
    }

    [Fact]
    public void Friday_Rule_Allows_Same_Customer_To_Book_A_Separate_Weekend_Period()
    {
        var booking = CreateBooking(3, 4);

        Assert.False(booking.BlocksPeriodFor(CustomerId, Day(5), Day(6)));
        Assert.True(booking.BlocksPeriodFor(CustomerId, Day(4), Day(5)));
    }

    [Theory]
    [InlineData(3, 4)]
    [InlineData(5, 6)]
    public void Cancelled_Booking_Never_Blocks_A_Valid_Period(int startDay, int endDay)
    {
        var booking = CreateBooking(3, 4, BookingStatus.Cancelled);

        Assert.False(booking.BlocksPeriodFor(OtherCustomerId, Day(startDay), Day(endDay)));
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.CheckedOut)]
    [InlineData(BookingStatus.Returned)]
    public void Non_Cancelled_Booking_Preserves_Its_Agreed_Period(BookingStatus status)
    {
        var booking = CreateBooking(2, 3, status);

        Assert.True(booking.BlocksPeriodFor(OtherCustomerId, Day(2), Day(3)));
    }

    [Fact]
    public void Single_Day_Booking_Blocks_The_Same_Calendar_Day()
    {
        var booking = CreateBooking(2, 2);

        Assert.True(booking.BlocksPeriodFor(OtherCustomerId, Day(2), Day(2)));
        Assert.False(booking.BlocksPeriodFor(OtherCustomerId, Day(3), Day(3)));
    }

    [Fact]
    public void Invalid_Requested_Period_Is_Rejected()
    {
        var booking = CreateBooking(2, 3);

        Assert.Throws<ArgumentException>(() => booking.BlocksPeriodFor(OtherCustomerId, Day(3), Day(2)));
    }

    [Fact]
    public void Empty_Customer_Identity_Is_Rejected()
    {
        var booking = CreateBooking(2, 3);

        Assert.Throws<ArgumentException>(() => booking.BlocksPeriodFor(Guid.Empty, Day(2), Day(3)));
    }

    [Fact]
    public void New_Booking_Is_Confirmed_By_Default()
    {
        Assert.Equal(BookingStatus.Confirmed, CreateBooking(2, 3).Status);
    }

    [Fact]
    public void Undefined_Status_Cannot_Be_Used_For_Existing_Booking()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateBooking(2, 3, (BookingStatus)999));
    }

    private static Booking CreateBooking(int startDay, int endDay, BookingStatus status = BookingStatus.Confirmed) =>
        new(Guid.NewGuid(), "SXX-TEST-001", CustomerId, Guid.NewGuid(), Day(startDay), Day(endDay), 500m, status);
}
