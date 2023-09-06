namespace Pullaroo.Server.Configuration;

internal class JwtSettings
{
    /*  "JwtSettings": {
    "SecurityKey": "this-is-the-private-key-that-is-used-to-sign-tokens-this-should-be-a-random-string-and-not-be-shared",
    "Issuer": "https://localhost",
    "Audience": "https://localhost",
    "TokenExpiryInMinutes": 10
  }*/
    public string PrivateKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int TokenExpiryInMinutes { get; set; }
}
