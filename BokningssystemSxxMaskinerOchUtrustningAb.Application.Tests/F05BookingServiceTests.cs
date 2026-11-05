using BokningssystemSxxMaskinerOchUtrustningAb.Application.Interfaces;
using BokningssystemSxxMaskinerOchUtrustningAb.Application.Services;
using BokningssystemSxxMaskinerOchUtrustningAb.Application.DTOs;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Enums;
using BokningssystemSxxMaskinerOchUtrustningAb.Domain.Models;
using NSubstitute;
using Xunit;

namespace BokningssystemSxxMaskinerOchUtrustningAb.Application.Tests;

public class F05BookingServiceTests
{
    private readonly IBookingRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly BookingService _service;

    private readonly Guid _customerId = Guid.NewGuid();

    public F05BookingServiceTests()
    {
        _repository = Substitute.For<IBookingRepository>();
        _timeProvider = Substitute.For<TimeProvider>();

        // Använd UTC som tidszon i testerna.
        _timeProvider.LocalTimeZone.Returns(TimeZoneInfo.Utc);

        // Bestäm dagens datum: 4 november 2026.
        _timeProvider.GetUtcNow()
            .Returns(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));

        _service = new BookingService(_repository, _timeProvider);
    }

    // Hjälpmetod för att skapa testbokningar.
    private Booking CreateBooking(
        Guid? customerId = null,
        DateOnly? startDate = null,
        BookingStatus status = BookingStatus.Confirmed)
    {
        return new Booking(
            Guid.NewGuid(),
            "SXX-TEST-001",
            customerId ?? _customerId,
            Guid.NewGuid(),
            startDate ?? new DateOnly(2026, 11, 10),
            new DateOnly(2026, 11, 15),
            1500m,
            status
        );
    }

    // TEST 1 - Kunden kan se sina egna bokningar.
    [Fact]
    public void GetCustomerBookings_ReturnsCustomersOwnBookings()
    {
        // Arrange
        var booking = CreateBooking();

        _repository.GetBookingsByCustomerId(_customerId)
            .Returns(new List<Booking> { booking });

        // Act
        var result = _service.GetCustomerBookings(_customerId);

        // Assert
        Assert.Single(result);
        Assert.Equal(_customerId, result[0].CustomerId);
    }

    // TEST 2 - Kunden får inte se andra kunders bokningar.
    [Fact]
    public void GetCustomerBookings_DoesNotReturnOtherCustomersBookings()
    {
        // Arrange
        var otherCustomerId = Guid.NewGuid();

        var ownBooking = CreateBooking();
        var otherBooking = CreateBooking(customerId: otherCustomerId);

        _repository.GetBookingsByCustomerId(_customerId)
            .Returns(new List<Booking> { ownBooking, otherBooking });

        // Act
        var result = _service.GetCustomerBookings(_customerId);

        // Assert
        Assert.Single(result);
        Assert.Equal(ownBooking.Id, result[0].Id);
    }

    // TEST 3 - Kunden får avboka dagen före uthyrningen.
    [Fact]
    public void CancelBooking_DayBeforeStart_Succeeds()
    {
        // Arrange
        var booking = CreateBooking(
            startDate: new DateOnly(2026, 11, 5));

        _repository.GetById(booking.Id).Returns(booking);
        _repository.UpdateBooking(Arg.Any<Booking>()).Returns(true);

        // Act
        var result = _service.CancelBooking(_customerId, booking.Id);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(CancellationError.None, result.Error);

        // Originalbokningen ska inte ändras.
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        // Repositoryt ska få en avbokad kopia.
        _repository.Received(1).UpdateBooking(
            Arg.Is<Booking>(b =>
                b.Id == booking.Id &&
                b.Status == BookingStatus.Cancelled));
    }

    // TEST 4 - Kunden får inte avboka på startdagen.
    [Fact]
    public void CancelBooking_OnStartDate_ReturnsTooLate()
    {
        // Arrange
        var booking = CreateBooking(
            startDate: new DateOnly(2026, 11, 4));

        _repository.GetById(booking.Id).Returns(booking);

        // Act
        var result = _service.CancelBooking(_customerId, booking.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CancellationError.TooLate, result.Error);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        _repository.DidNotReceive()
            .UpdateBooking(Arg.Any<Booking>());
    }

    // TEST 5 - Kunden får inte avboka någon annans bokning.
    [Fact]
    public void CancelBooking_OtherCustomersBooking_ReturnsNotFound()
    {
        // Arrange
        var booking = CreateBooking(customerId: Guid.NewGuid());

        _repository.GetById(booking.Id).Returns(booking);

        // Act
        var result = _service.CancelBooking(_customerId, booking.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CancellationError.NotFound, result.Error);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        _repository.DidNotReceive()
            .UpdateBooking(Arg.Any<Booking>());
    }

    // TEST 6 - En redan avbokad bokning får inte avbokas igen.
    [Fact]
    public void CancelBooking_AlreadyCancelled_ReturnsError()
    {
        // Arrange
        var booking = CreateBooking(
            status: BookingStatus.Cancelled);

        _repository.GetById(booking.Id).Returns(booking);

        // Act
        var result = _service.CancelBooking(_customerId, booking.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CancellationError.AlreadyCancelled, result.Error);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);

        _repository.DidNotReceive()
            .UpdateBooking(Arg.Any<Booking>());
    }

    // TEST 7 - En påbörjad uthyrning får inte avbokas.
    [Fact]
    public void CancelBooking_CheckedOut_ReturnsRentalStarted()
    {
        // Arrange
        var booking = CreateBooking(
            status: BookingStatus.CheckedOut);

        _repository.GetById(booking.Id).Returns(booking);

        // Act
        var result = _service.CancelBooking(_customerId, booking.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CancellationError.RentalStarted, result.Error);
        Assert.Equal(BookingStatus.CheckedOut, booking.Status);

        _repository.DidNotReceive()
            .UpdateBooking(Arg.Any<Booking>());
    }

    // TEST 8 - Misslyckad sparning får inte ändra originalbokningen.
    [Fact]
    public void CancelBooking_SaveFails_OriginalBookingRemainsConfirmed()
    {
        // Arrange
        var booking = CreateBooking();

        _repository.GetById(booking.Id).Returns(booking);

        // Simulera att repositoryt inte lyckas spara.
        _repository.UpdateBooking(Arg.Any<Booking>())
            .Returns(false);

        // Act
        var result = _service.CancelBooking(_customerId, booking.Id);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(CancellationError.SaveFailed, result.Error);

        // Originalbokningen ska fortfarande vara bekräftad.
        Assert.Equal(BookingStatus.Confirmed, booking.Status);

        // Kontrollera att repositoryt fick en avbokad kopia.
        _repository.Received(1).UpdateBooking(
            Arg.Is<Booking>(b =>
                b.Id == booking.Id &&
                b.Status == BookingStatus.Cancelled));
    }
}