using Eventuous;

namespace Pullaroo.Contracts.StronglyTypedIds;

public record UserId(string Value) : StronglyTypedId(Value);
