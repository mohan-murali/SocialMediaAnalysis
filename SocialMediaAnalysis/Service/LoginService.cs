using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using SocialMediaAnalysis.Models;
using SocialMediaAnalysis.Utils;

namespace SocialMediaAnalysis.Service;

public interface ILoginService
{
    Task<(string, string)> RegisterUser(User user);
    Task<(string, string)> LoginUser(LoginRequest request);
}

public class LoginService : ILoginService
{
    private readonly IMongoCollection<User> _user;
    private readonly IConfiguration _configuration;

    public LoginService(IMongoDatabase db, IConfiguration configuration)
    {
        _user = db.GetCollection<User>(typeof(User).Name);
        _configuration = configuration;
    }


    public async Task<(string, string)> RegisterUser(User user)
    {
        var existingUser = await _user.Find(usr => usr.Email == user.Email).FirstOrDefaultAsync();

        if (existingUser != null)
        {
            throw new Exception(ExceptionMessage.USEREXISTS);
        }

        var hashPassword = PasswordUtils.HashPassword(user.Password);

        var newUser = new User(user.Email, user.Name, hashPassword)
        {
            Id = ObjectId.GenerateNewId().ToString()
        };

        await _user.InsertOneAsync(newUser);

        var token = GenerateToken(newUser);

        return (newUser.Name, token);
    }

    public async Task<(string, string)> LoginUser(LoginRequest request)
    {
        var existingUser = await _user.Find(usr => usr.Email == request.Email).FirstOrDefaultAsync();
        if (existingUser == null)
        {
            throw new Exception(ExceptionMessage.USERNOTFOUND);
        }

        var isPasswordValid = PasswordUtils.ValidatePassword(request.Password, existingUser.Password);

        if (!isPasswordValid)
        {
            throw new Exception(ExceptionMessage.PASSWORDMISMATCH);
        }

        var token = GenerateToken(existingUser);

        return (existingUser.Name, token);
    }

    // To generate token
    private string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration.GetSection("Identity:Token").Value));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        var handler = new JwtSecurityTokenHandler();

        var tokenString = handler.WriteToken(token);

        return tokenString;
    }
}