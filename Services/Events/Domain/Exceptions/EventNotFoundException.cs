namespace Events.Domain.Exceptions;

public class EventNotFoundException(string message) : Exception(message);