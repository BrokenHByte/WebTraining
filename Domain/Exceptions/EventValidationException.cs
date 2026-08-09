namespace Domain.Exceptions;

public class EventValidationException(string message) : Exception(message);