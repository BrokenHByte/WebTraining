namespace Domain.Exceptions;

public class LoginAlreadyUseException(string message) : Exception(message);