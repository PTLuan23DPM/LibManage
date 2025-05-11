using LibWeb.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class AuthenticationService
{
    private readonly AppDbContext _context;

    public AuthenticationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> AuthenticateAsync(string username, string password)
    {
        return await _context.Users
                             .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
    }

    public async Task<bool> RegisterAsync(User user)
    {
        if (_context.Users.Any(u => u.Username == user.Username))
        {
            return false;
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return true;
    }
}
