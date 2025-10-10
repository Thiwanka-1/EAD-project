// =============================================
// File: AuthDtos.cs
// Description: DTO class for transferring user data between client and server.
// Author: Gamithu / IT22295224
// Date: 10/10/2025
// =============================================

namespace EvCharge.Api.DTOs
{
    // For system users (Backoffice/Operator)
    public class SystemLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // EV Owner register
    public class OwnerRegisterRequest
    {
        public string NIC { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // EV Owner login (by email)
    public class OwnerLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Standard auth response
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
