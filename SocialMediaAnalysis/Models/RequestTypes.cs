namespace SocialMediaAnalysis.Models;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Name, string Password);