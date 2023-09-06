namespace Pullaroo.Contracts;

public record UserTokens
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
}
