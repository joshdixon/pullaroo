using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Web;

using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Octokit;

using Pullaroo.Server.Configuration;

namespace Pullaroo.Server.Features.Authentication;

public record ExchangeGitHubOAuthCodeRequest : IRequest<Result<UserTokens>>
{
    public required string Code { get; init; }
}

public class ExchangeGitHubOAuthCodeController : ControllerBase
{
    [HttpPost("/oauth/github")]
    public async Task<ActionResult<UserTokens>> ExchangeGitHubOAuthCode([FromBody] ExchangeGitHubOAuthCodeRequest request,
        [FromServices] IMediator mediator)
    {
        Result<UserTokens> result = await mediator.Send(request);

        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
    }
}

internal class ExchangeGitHubOAuthCodeHandler : CommandHandler<ExchangeGitHubOAuthCodeRequest, Result<UserTokens>>
{
    private readonly IGitHubClient _gitHubClient;
    private readonly IOptions<GitHubApiSettings> _gitHubApiOptions;
    private readonly ILogger<ExchangeGitHubOAuthCodeHandler> _logger;
    private readonly IMediator _mediator;

    public ExchangeGitHubOAuthCodeHandler(IGitHubClient gitHubClient,
        IMediator mediator,
        ILogger<ExchangeGitHubOAuthCodeHandler> logger,
        IOptions<GitHubApiSettings> gitHubApiOptions)
    {
        _gitHubClient = gitHubClient;
        _mediator = mediator;
        _logger = logger;
        _gitHubApiOptions = gitHubApiOptions;
    }

    public override Task<Result<UserTokens>> Handle(ExchangeGitHubOAuthCodeRequest command) => Result
        .Ok()
        .MapTryAsync(() => _gitHubClient.Oauth.CreateAccessToken(new OauthTokenRequest(_gitHubApiOptions.Value.ClientId,
            _gitHubApiOptions.Value.ClientSecret,
            command.Code)))
        .BindAsync(tokens => string.IsNullOrWhiteSpace(tokens?.ErrorDescription)
            ? Result.Ok(new UserTokens { AccessToken = tokens.AccessToken, RefreshToken = tokens.RefreshToken })
            : Result.Fail<UserTokens>(tokens.ErrorDescription));
}
