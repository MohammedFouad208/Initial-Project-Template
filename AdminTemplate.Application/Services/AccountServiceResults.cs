using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace AdminTemplate.Application.Services
{
    public enum LoginResult
    {
        Success,
        InvalidCredentials,
        LockedOut,
        Inactive
    }

    public class RegisterResult
    {
        public bool Succeeded { get; init; }
        public IEnumerable<string> Errors { get; init; } = new string[0];

        public static RegisterResult Ok() => new() { Succeeded = true };

        public static RegisterResult Fail(IEnumerable<IdentityError> errors) =>
            new() { Succeeded = false, Errors = errors is null ? new string[0] : System.Linq.Enumerable.Select(errors, e => e.Description) };
    }

    public class ResetPasswordResult
    {
        public bool Succeeded { get; init; }
        public IEnumerable<string> Errors { get; init; } = new string[0];

        public static ResetPasswordResult Ok() => new() { Succeeded = true };

        public static ResetPasswordResult Fail(string message) =>
            new() { Succeeded = false, Errors = new[] { message } };

        public static ResetPasswordResult Fail(IEnumerable<IdentityError> errors) =>
            new() { Succeeded = false, Errors = errors is null ? new string[0] : System.Linq.Enumerable.Select(errors, e => e.Description) };
    }
}
