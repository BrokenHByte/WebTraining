namespace Bookings.Domain.Exceptions;

public class InvalidCredentialsException(string message) : Exception(message);