namespace Domain.Entities;

public class User
{
    public enum Roles
    {
        User, Admin
    }
    
    public Guid Id { get; set; }
    public required string Login { get; set; }
    public required string HashPass { get; set; }
    public Roles Role { get; set; } = Roles.User;
    
    public List<Booking> Bookings { get; set; }
}