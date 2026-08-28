namespace Bookings.Domain.Exceptions;

public class BookingBeginEventException(string message) : Exception(message);