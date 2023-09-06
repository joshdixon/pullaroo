namespace Pullaroo.Contracts;

public enum ResultStatus
{
    Success = 200,
    Created = 201,
    SuccessNoContent = 204,
    GenericError = 400,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    Gone = 410,
    InternalServerError = 500
}
