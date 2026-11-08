namespace Forma.Api.Dtos.Auth;

public record LoginRequestDto(string UserName, string Password);

public record RefreshRequestDto(string RefreshToken);

public record CurrentUserDto(Guid Id, string UserName, string DisplayName, string? Email);
