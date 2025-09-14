using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Core.DTOs
{
    public record RegisterRequest(string Username, string Email, string Password);
    public record LoginRequest(string EmailOrUsername, string Password);
    public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid UserId, string Username, string Email);
    public record RefreshRequest(string RefreshToken);
}