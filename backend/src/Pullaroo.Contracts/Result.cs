// using System.Diagnostics;
//
// using Microsoft.Extensions.Logging;
//
// namespace Pullaroo.Contracts;
//
// public record Result
// {
//     /// <summary>
//     ///     Result status
//     /// </summary>
//     public ResultStatus StatusCode { get; set; } = ResultStatus.GenericError;
//
//     /// <summary>
//     ///     If the status code indicates that the request succeeded
//     /// </summary>
//     public bool HasSucceeded => StatusCode == ResultStatus.Success
//         || StatusCode == ResultStatus.Created
//         || StatusCode == ResultStatus.SuccessNoContent;
//
//     /// <summary>
//     ///     Return error messages (suitable for end user)
//     /// </summary>
//     public IEnumerable<string> Errors { get; init; } = Enumerable.Empty<string>();
//
//     #region Successful results
//
//     /// <summary>
//     ///     204 OK response, doesn't contain data. Indicates that the request succeeded.
//     /// </summary>
//     public static Result Succeed() =>
//         new() { StatusCode = ResultStatus.SuccessNoContent };
//
//     /// <summary>
//     ///     201 Created response, doesn't contain data. Indicates that the request succeeded. And a resource was created.
//     /// </summary>
//     public static Result Created() =>
//         new() { StatusCode = ResultStatus.Created };
//
//     #endregion
//
//     #region Unsuccessful results
//
//     /// <summary>
//     ///     Generic error. Usually indicates that something to do with the request data might be wrong.
//     /// </summary>
//     public static Result Fail(string errorMessage, ILogger? logger = null) => Fail(new[] { errorMessage }, logger);
//
//     /// <summary>
//     ///     Generic error. Usually indicates that something to do with the request data might be wrong.
//     /// </summary>
//     public static Result Fail(IEnumerable<string> errorMessages, ILogger? logger = null) =>
//         InternalFailResult(ResultStatus.GenericError, errorMessages, logger);
//
//
//     /// <summary>
//     ///     User permissions error. The authenticated user is not allowed to access/modify the resource.
//     /// </summary>
//     public static Result Forbidden(string errorMessage) => Forbidden(new[] { errorMessage });
//
//     /// <summary>
//     ///     User permissions error. The authenticated user is not allowed to access/modify the resource.
//     /// </summary>
//     public static Result Forbidden(IEnumerable<string> errorMessages) => InternalFailResult(ResultStatus.Forbidden, errorMessages);
//
//
//     /// <summary>
//     ///     Not found error. A resource needed to complete the request could not be found.
//     /// </summary>
//     public static Result NotFound(string errorMessage, ILogger? logger = null) => NotFound(new[] { errorMessage }, logger);
//
//     /// <summary>
//     ///     Not found error. A resource needed to complete the request could not be found.
//     /// </summary>
//     public static Result NotFound(IEnumerable<string> errorMessages, ILogger? logger = null) =>
//         InternalFailResult(ResultStatus.NotFound, errorMessages, logger);
//
//
//     /// <summary>
//     ///     Conflict error. Indicates that the failed as it conflicted with the current state of a resource (concurrency
//     ///     exception maybe).
//     /// </summary>
//     public static Result Conflict(string errorMessage) => Conflict(new[] { errorMessage });
//
//     /// <summary>
//     ///     Conflict error. Indicates that the failed as it conflicted with the current state of a resource (concurrency
//     ///     exception maybe).
//     /// </summary>
//     public static Result Conflict(IEnumerable<string> errorMessages) => InternalFailResult(ResultStatus.Conflict, errorMessages);
//
//
//     /// <summary>
//     ///     Gone error. A resource needed to complete this request was initially available, but is no longer available or
//     ///     removed.
//     /// </summary>
//     public static Result Gone(string errorMessage) => Gone(new[] { errorMessage });
//
//     /// <summary>
//     ///     Gone error. A resource needed to complete this request was initially available, but is no longer available or
//     ///     removed.
//     /// </summary>
//     public static Result Gone(IEnumerable<string> errorMessages) => InternalFailResult(ResultStatus.Gone, errorMessages);
//
//     /// <summary>
//     ///     Internal server error. Indicates that something has gone wrong on the backend, such as an exception being thrown.
//     /// </summary>
//     public static Result InternalServerError(Exception ex, string errorMessage, ILogger? logger = null) =>
//         InternalFailResult(ResultStatus.InternalServerError, new[] { errorMessage }, logger, ex);
//
//
//     private static Result InternalFailResult(ResultStatus statusCode, IEnumerable<string> errorMessages, ILogger? logger = null, Exception? ex = null)
//     {
//         SetOpenTelemetryError();
//
//         if (ex != null)
//         {
//             logger?.LogError(ex, string.Join(", ", errorMessages));
//         }
//         else
//         {
//             logger?.LogWarning(string.Join(", ", errorMessages));
//         }
//
//         return new Result
//         {
//             StatusCode = statusCode,
//             Errors = errorMessages
//         };
//     }
//
//     private static void SetOpenTelemetryError() => Activity.Current?.SetTag("otel.status_code", "ERROR");
//
//     #endregion
// }
//
// public record Result<T> : Result
// {
//     public Result()
//     {
//     }
//
//     public Result(Result baseResult) : base(baseResult)
//     {
//     }
//
//     /// <summary>
//     ///     The return value of the stated type
//     /// </summary>
//     public T? Value { get; init; }
//
//     public new bool HasSucceeded => base.HasSucceeded && Value is not null;
//
//     #region Successful results
//
//     /// <summary>
//     ///     200 OK response, contains data. Indicates that the request succeeded.
//     /// </summary>
//     public static Result<T> Succeed(T value) =>
//         new()
//         {
//             StatusCode = ResultStatus.Success,
//             Value = value
//         };
//
//     /// <summary>
//     ///     201 Created response, contains data. Indicates that the request succeeded and a resource was created.
//     /// </summary>
//     public static Result<T> Created(T value) =>
//         new()
//         {
//             StatusCode = ResultStatus.Created,
//             Value = value
//         };
//
//     #endregion
//
//
//     #region Unsuccessful results
//
//     /// <summary>
//     ///     Generic error. Usually indicates that something to do with the request data might be wrong.
//     /// </summary>
//     public static new Result<T> Fail(string errorMessage, ILogger? logger = null) => Fail(new[] { errorMessage });
//
//     /// <summary>
//     ///     Generic error. Usually indicates that something to do with the request data might be wrong.
//     /// </summary>
//     public static new Result<T> Fail(IEnumerable<string> errorMessages, ILogger? logger = null) =>
//         InternalFailResult(ResultStatus.GenericError, errorMessages, logger);
//
//
//     /// <summary>
//     ///     User permissions error. The authenticated user is not allowed to access/modify the resource.
//     /// </summary>
//     public static new Result<T> Forbidden(string errorMessage) => Forbidden(new[] { errorMessage });
//
//     /// <summary>
//     ///     User permissions error. The authenticated user is not allowed to access/modify the resource.
//     /// </summary>
//     public static new Result<T> Forbidden(IEnumerable<string> errorMessages) => InternalFailResult(ResultStatus.Forbidden, errorMessages);
//
//
//     /// <summary>
//     ///     Not found error. A resource needed to complete the request could not be found.
//     /// </summary>
//     public static new Result<T> NotFound(string errorMessage, ILogger? logger = null) => NotFound(new[] { errorMessage }, logger);
//
//     /// <summary>
//     ///     Not found error. A resource needed to complete the request could not be found.
//     /// </summary>
//     public static new Result<T> NotFound(IEnumerable<string> errorMessages, ILogger? logger = null) =>
//         InternalFailResult(ResultStatus.NotFound, errorMessages, logger);
//
//
//     /// <summary>
//     ///     Conflict error. Indicates that the failed as it conflicted with the current state of a resource (concurrency
//     ///     exception maybe).
//     /// </summary>
//     public static new Result<T> Conflict(string errorMessage) => Conflict(new[] { errorMessage });
//
//     /// <summary>
//     ///     Conflict error. Indicates that the failed as it conflicted with the current state of a resource (concurrency
//     ///     exception maybe).
//     /// </summary>
//     public static new Result<T> Conflict(IEnumerable<string> errorMessages) => InternalFailResult(ResultStatus.Conflict, errorMessages);
//
//
//     /// <summary>
//     ///     Gone error. A resource needed to complete this request was initially available, but is no longer available or
//     ///     removed.
//     /// </summary>
//     public static new Result<T> Gone(string errorMessage) => Gone(new[] { errorMessage });
//
//     /// <summary>
//     ///     Gone error. A resource needed to complete this request was initially available, but is no longer available or
//     ///     removed.
//     /// </summary>
//     public static new Result<T> Gone(IEnumerable<string> errorMessages) => InternalFailResult(ResultStatus.Gone, errorMessages);
//
//     /// <summary>
//     ///     Internal server error. Indicates that something has gone wrong on the backend, such as an exception being thrown.
//     /// </summary>
//     public static new Result<T> InternalServerError(Exception ex, string errorMessage, ILogger? logger = null) =>
//         InternalFailResult(ResultStatus.InternalServerError, new[] { errorMessage }, logger, ex);
//
//
//     private static Result<T> InternalFailResult(ResultStatus statusCode,
//         IEnumerable<string> errorMessages,
//         ILogger? logger = null,
//         Exception? ex = null)
//     {
//         SetOpenTelemetryError();
//
//         if (ex != null)
//         {
//             logger?.LogError(ex, string.Join(", ", errorMessages));
//         }
//         else
//         {
//             logger?.LogWarning(string.Join(", ", errorMessages));
//         }
//
//         return new Result<T>
//         {
//             StatusCode = statusCode,
//             Errors = errorMessages.Concat(new[] { ex?.Message, ex?.StackTrace }).Where(x => x != null).Select(x => x!)
//         };
//     }
//
//     private static void SetOpenTelemetryError() => Activity.Current?.SetTag("otel.status_code", "ERROR");
//
//     #endregion
// }
