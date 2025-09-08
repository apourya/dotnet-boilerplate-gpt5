namespace EnterpriseBoilerplate.Application.Users
{
    public sealed record AuthResultDto(string AccessToken, string TokenType, int ExpiresInSeconds);
}
