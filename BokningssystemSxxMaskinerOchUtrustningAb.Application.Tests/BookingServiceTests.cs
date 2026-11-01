using BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;
using BokningssystemSxxMaskinerOchUtrustningAb.Application.Interfaces;
using BokningssystemSxxMaskinerOchUtrustningAb.Application.Services;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Enums;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.Tests;

public class BookingServiceTests
{
    private readonly IBookingRepository repository = Substitute.For<IBookingRepository>();
    private readonly TimeProvider clock = Substitute.For<TimeProvider>();
    private readonly Guid customerId = Guid.NewGuid();
    private readonly Guid modelId = Guid.NewGuid();
    private readonly BookingService service;

    public BookingServiceTests()
    {
        clock.GetUtcNow().Returns(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
        clock.LocalTimeZone.Returns(TimeZoneInfo.Utc);
        service = new BookingService(repository, clock);
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([], []));
        repository.TrySaveBooking(Arg.Any<Booking>()).Returns(true);
    }

    [Fact]
    public void Creates_And_Returns_Booking_With_Customer_Item_Period_And_Agreed_Price()
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item], []));
        var request = Request(1, 1);

        var result = service.CreateBooking(customerId, request);

        Assert.True(result.Success);
        Assert.Null(result.Error);
        var booking = Assert.IsType<Booking>(result.Booking);
        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.StartsWith("SXX-", booking.BookingNumber);
        Assert.Equal(customerId, booking.CustomerId);
        Assert.Equal(item.Id, booking.EquipmentItemId);
        Assert.Equal(request.StartDate, booking.StartDate);
        Assert.Equal(request.EndDate, booking.EndDate);
        Assert.Equal(request.AgreedTotalPrice, booking.AgreedTotalPrice);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Received.InOrder(() =>
        {
            repository.GetItemsAndBookings(modelId);
            repository.TrySaveBooking(booking);
        });
        repository.Received(1).TrySaveBooking(booking);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 2)]
    public void Invalid_Dates_Do_Not_Call_Repository(int startDay, int endDay)
    {
        AssertInvalid(service.CreateBooking(customerId, Request(startDay, endDay)));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_Customer_Or_Model_Does_Not_Call_Repository(bool missingCustomer)
    {
        var request = missingCustomer ? Request(2, 3) : Request(2, 3) with { ModelId = Guid.Empty };
        AssertInvalid(service.CreateBooking(missingCustomer ? Guid.Empty : customerId, request));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void Negative_Agreed_Price_Does_Not_Call_Repository()
    {
        AssertInvalid(service.CreateBooking(customerId, Request(2, 3) with { AgreedTotalPrice = -1m }));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void No_Items_Returns_Unavailable_Without_Saving()
    {
        AssertUnavailable(service.CreateBooking(customerId, Request(2, 3)));
        repository.DidNotReceive().TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Unbookable_Or_Wrong_Model_Items_Are_Not_Selected()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([
            Item(isBookable: false), new EquipmentItem(Guid.NewGuid(), "OTHER", Guid.NewGuid())], []));

        AssertUnavailable(service.CreateBooking(customerId, Request(2, 3)));
        repository.DidNotReceive().TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Selects_Another_Available_Item_Of_Requested_Model()
    {
        var first = Item();
        var second = Item();
        var existing = ExistingBooking(first, 2, 3);
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([first, second], [existing]));

        var result = service.CreateBooking(customerId, Request(2, 3));

        Assert.True(result.Success);
        Assert.Equal(second.Id, result.Booking!.EquipmentItemId);
        repository.Received(1).TrySaveBooking(Arg.Is<Booking>(b => b.EquipmentItemId == second.Id));
    }

    [Fact]
    public void Overlapping_Booking_Blocks_Item_Without_Saving()
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item], [ExistingBooking(item, 2, 3)]));

        AssertUnavailable(service.CreateBooking(customerId, Request(3, 4)));
        repository.DidNotReceive().TrySaveBooking(Arg.Any<Booking>());
    }

    [Theory]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    public void Uses_Domain_Friday_Rule_When_Selecting_Item(int startDay, bool expectedSuccess)
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item], [ExistingBooking(item, 3, 4)]));

        var result = service.CreateBooking(customerId, Request(startDay, startDay));

        Assert.Equal(expectedSuccess, result.Success);
        repository.Received(expectedSuccess ? 1 : 0).TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Cancelled_Booking_Does_Not_Prevent_Creation()
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item],
            [ExistingBooking(item, 2, 3, BookingStatus.Cancelled)]));

        Assert.True(service.CreateBooking(customerId, Request(2, 3)).Success);
    }

    [Fact]
    public void Failed_Final_Save_Returns_Conflict_Without_Confirmation()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([Item()], []));
        repository.TrySaveBooking(Arg.Any<Booking>()).Returns(false);

        var result = service.CreateBooking(customerId, Request(2, 3));

        AssertUnavailable(result);
        Assert.Contains("hann bli", result.Message);
        repository.Received(1).TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Separate_Bookings_Receive_Different_Ids_And_Numbers()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([Item()], []));

        var first = service.CreateBooking(customerId, Request(2, 3));
        var second = service.CreateBooking(customerId, Request(7, 8));

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.NotEqual(first.Booking!.Id, second.Booking!.Id);
        Assert.NotEqual(first.Booking.BookingNumber, second.Booking.BookingNumber);
        repository.Received(2).TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Storage_Errors_Are_Not_Reported_As_Unavailable_Equipment()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([Item()], []));
        repository.TrySaveBooking(Arg.Any<Booking>()).Throws(new InvalidOperationException("Storage error"));

        Assert.Throws<InvalidOperationException>(() => service.CreateBooking(customerId, Request(2, 3)));
    }

    [Fact]
    public void Today_Uses_The_Time_Providers_Local_Calendar_Date()
    {
        clock.GetUtcNow().Returns(new DateTimeOffset(2029, 12, 31, 23, 30, 0, TimeSpan.Zero));
        clock.LocalTimeZone.Returns(TimeZoneInfo.CreateCustomTimeZone("Test UTC+1", TimeSpan.FromHours(1), "Test", "Test"));
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([Item()], []));

        Assert.True(service.CreateBooking(customerId, Request(1, 1)).Success);
        AssertInvalid(service.CreateBooking(customerId, Request(0, 0)));
    }

    private CreateBookingRequest Request(int startDay, int endDay) =>
        new(modelId, new DateOnly(2030, 1, 1).AddDays(startDay - 1), new DateOnly(2030, 1, 1).AddDays(endDay - 1), 750m);

    private EquipmentItem Item(bool isBookable = true) => new(Guid.NewGuid(), "BH-001", modelId, isBookable);

    private static Booking ExistingBooking(EquipmentItem item, int startDay, int endDay,
        BookingStatus status = BookingStatus.Confirmed) =>
        new(Guid.NewGuid(), "EXISTING", Guid.NewGuid(), item.Id, new(2030, 1, startDay), new(2030, 1, endDay), 500m, status);

    private static void AssertInvalid(BookingResult result)
    {
        Assert.False(result.Success);
        Assert.Equal(BookingError.InvalidRequest, result.Error);
        Assert.Null(result.Booking);
        Assert.NotEmpty(result.Message);
    }

    private static void AssertUnavailable(BookingResult result)
    {
        Assert.False(result.Success);
        Assert.Equal(BookingError.Unavailable, result.Error);
        Assert.Null(result.Booking);
        Assert.NotEmpty(result.Message);
    }
}
