using System;
using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.API.DTOs
{
    public record RegisterRequest(
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [MinLength(3, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự")]
        string Username,

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        string Password,

        string? FullName
    );

    public record LoginRequest(
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        string Username,

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        string Password
    );

    public record ChangePasswordRequest(
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        string OldPassword,

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải từ 6 ký tự trở lên")]
        string NewPassword
    );

    public record UserResponse(
        Guid Id,
        string Username,
        string FullName,
        DateTime CreatedAt
    );
}
