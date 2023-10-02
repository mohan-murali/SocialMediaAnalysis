namespace SocialMediaAnalysis.Models;

public record RegisterResponse(string Status, string Message, string Token, string Name);

public record LoginResponse(string Status, string Message, string Token, string Name);