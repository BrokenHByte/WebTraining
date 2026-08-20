namespace Domain.Exceptions;

public class BookingExceedingLimitException(string message) : Exception(message);