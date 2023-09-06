using System.ComponentModel;

namespace Pullaroo.Contracts.StronglyTypedIds;

[TypeConverter(typeof(StronglyTypedIdConverter))]
public abstract record StronglyTypedId {
    protected StronglyTypedId(string value) 
    {
        // if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException();

        Value = value;
    }

    public string Value { get; }

    public sealed override string ToString() => Value;

    public static implicit operator string?(StronglyTypedId? id) => id?.ToString();

    public void Deconstruct(out string value) => value = Value;
}
