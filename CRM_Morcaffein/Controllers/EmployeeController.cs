using CRM_Morcaffein.api.DTOs;
using CRM_Morcaffein.winforms.Services.Api;

namespace CRM_Morcaffein.Controllers
{
    public class EmployeeController
    {
        private readonly EmployeeApi _api;

        public EmployeeController()
        {
            _api = new EmployeeApi();
        }

        public Task<PagedResponse<EmployeeResponseDto>> SearchAsync(string searchText, int page = 1, int pageSize = 25)
            => _api.GetEmployeesAsync(searchText, page, pageSize);

        public Task<EmployeeResponseDto?> GetByIdAsync(int employeeId)
            => _api.GetEmployeeAsync(employeeId);

        public Task<EmployeeResponseDto> CreateAsync(
            string firstName, string lastName, string phone, string? email,
            string role, string username, string password,
            int? branchId = null)
        {
            var request = new CreateEmployeeRequest
            {
                FirstName = firstName,
                LastName = lastName,
                ContactNumber = phone,
                Email = email,
                Role = role,
                BranchId = branchId,
                Username = username,
                Password = password,
            };
            return _api.CreateEmployeeAsync(request);
        }

        public Task<EmployeeResponseDto> UpdateAsync(
             int employeeId, string firstName, string lastName, string phone,
             string? email, string role, string? username = null,
             int? branchId = null)
        {
            var request = new UpdateEmployeeRequest
            {
                FirstName = firstName,
                LastName = lastName,
                ContactNumber = phone,
                Email = email,
                Role = role,
                BranchId = branchId,
                Username = username,
            };
            return _api.UpdateEmployeeAsync(employeeId, request);
        }

        public Task DeactivateAsync(int employeeId) => _api.DeactivateEmployeeAsync(employeeId);
        public Task ReactivateAsync(int employeeId) => _api.ReactivateEmployeeAsync(employeeId);

        public Task ResetPasswordAsync(int employeeId, string newPassword) =>
            _api.ResetPasswordAsync(employeeId, newPassword);

    }
}