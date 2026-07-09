namespace SimpleMapper.Net.Benchmarks.Models;

// Flat pair used by the simple-DTO and map-into scenarios: eight scalar members,
// no nesting. This is the shape where fixed per-call overhead dominates, so it is
// the fairest scenario for exposing the mappers' baseline cost.

public class Customer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal CreditLimit { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int LoyaltyPoints { get; set; }
    public decimal CreditLimit { get; set; }
}
