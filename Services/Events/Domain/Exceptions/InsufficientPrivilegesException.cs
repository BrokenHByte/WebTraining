namespace Domain.Exceptions;

public class InsufficientPrivilegesException(string message) : Exception(message);