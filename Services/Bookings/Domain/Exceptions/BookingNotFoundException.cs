namespace Bookings.Domain.Exceptions;

public class BookingNotFoundException(string message) : Exception(message);