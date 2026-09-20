using CRM_Morcaffein.domain.entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRM_Morcaffein.api.Services
{
    /// <summary>
    /// Builds and signs JWTs for authenticated employees. Reads the Jwt:Secret/Issuer/
    /// Audience/ExpiryHours values from appsettings.json.
    /// </summary>
    public class JwtTokenService
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryHours;

        public JwtTokenService(IConfiguration configuration)
        {
            var section = configuration.GetSection("Jwt");
            _secret = section["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
            _issuer = section["Issuer"] ?? "MorcaffeinApi";
            _audience = section["Audience"] ?? "MorcaffeinClient";
            _expiryHours = int.TryParse(section["ExpiryHours"], out var h) ? h : 8;
        }

        public DateTime GetExpiryUtc() => DateTime.UtcNow.AddHours(_expiryHours);

        public string CreateToken(Employee employee, int companyId)
        {
            var expiresAt = GetExpiryUtc();

            var claims = new[]
            {
                new Claim("employeeId", employee.EmployeeId.ToString()),
                new Claim("companyId", companyId.ToString()),
                new Claim(ClaimTypes.Role, employee.Role),
                new Claim(ClaimTypes.Name, employee.FullName),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}