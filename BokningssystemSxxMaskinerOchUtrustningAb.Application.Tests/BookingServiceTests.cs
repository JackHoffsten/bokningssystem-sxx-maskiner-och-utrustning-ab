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
    public void Creates_and_returns_booking_with_customer_item_period_and_agreed_price()
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item], []));
        var request = Request(1, 1); // Start idag och endagsbokning är giltiga.

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
    public void Invalid_dates_do_not_call_repository(int startDay, int endDay)
    {
        AssertInvalid(service.CreateBooking(customerId, Request(startDay, endDay)));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_customer_or_model_does_not_call_repository(bool missingCustomer)
    {
        var request = missingCustomer ? Request(2, 3) : Request(2, 3) with { ModelId = Guid.Empty };
        AssertInvalid(service.CreateBooking(missingCustomer ? Guid.Empty : customerId, request));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void Negative_agreed_price_does_not_call_repository()
    {
        AssertInvalid(service.CreateBooking(customerId, Request(2, 3) with { AgreedTotalPrice = -1m }));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public void No_items_returns_unavailable_without_saving()
    {
        AssertUnavailable(service.CreateBooking(customerId, Request(2, 3)));
        repository.DidNotReceive().TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Unbookable_or_wrong_model_items_are_not_selected()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([
            Item(isBookable: false), new EquipmentItem(Guid.NewGuid(), "OTHER", Guid.NewGuid())], []));

        AssertUnavailable(service.CreateBooking(customerId, Request(2, 3)));
        repository.DidNotReceive().TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Selects_another_available_item_of_requested_model()
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
    public void Overlapping_booking_blocks_item_without_saving()
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item], [ExistingBooking(item, 2, 3)]));

        AssertUnavailable(service.CreateBooking(customerId, Request(3, 4)));
        repository.DidNotReceive().TrySaveBooking(Arg.Any<Booking>());
    }

    [Theory]
    [InlineData(5, false)] // Lördag efter fredag.
    [InlineData(6, false)] // Söndag efter fredag.
    [InlineData(7, true)]  // Måndag.
    public void Uses_domain_friday_rule_when_selecting_item(int startDay, bool expectedSuccess)
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item], [ExistingBooking(item, 3, 4)]));

        var result = service.CreateBooking(customerId, Request(startDay, startDay));

        Assert.Equal(expectedSuccess, result.Success);
        repository.Received(expectedSuccess ? 1 : 0).TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Cancelled_booking_does_not_prevent_creation()
    {
        var item = Item();
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([item],
            [ExistingBooking(item, 2, 3, BookingStatus.Cancelled)]));

        Assert.True(service.CreateBooking(customerId, Request(2, 3)).Success);
    }

    [Fact]
    public void Failed_final_save_returns_conflict_without_confirmation()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([Item()], []));
        repository.TrySaveBooking(Arg.Any<Booking>()).Returns(false);

        var result = service.CreateBooking(customerId, Request(2, 3));

        AssertUnavailable(result);
        Assert.Contains("hann bli", result.Message);
        repository.Received(1).TrySaveBooking(Arg.Any<Booking>());
    }

    [Fact]
    public void Separate_bookings_receive_different_ids_and_numbers()
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
    public void Storage_errors_are_not_reported_as_unavailable_equipment()
    {
        repository.GetItemsAndBookings(modelId).Returns(new BookingAvailability([Item()], []));
        repository.TrySaveBooking(Arg.Any<Booking>()).Throws(new InvalidOperationException("Storage error"));

        Assert.Throws<InvalidOperationException>(() => service.CreateBooking(customerId, Request(2, 3)));
    }

    [Fact]
    public void Today_uses_the_time_providers_local_calendar_date()
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
