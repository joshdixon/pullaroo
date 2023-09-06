using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

using Pullaroo.Contracts.StronglyTypedIds;

namespace Pullaroo.Server;

public class StronglyTypedIdBsonSerializer<TStronglyTypedId> : SerializerBase<TStronglyTypedId>
    where TStronglyTypedId : StronglyTypedId
{
    public override TStronglyTypedId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var stringValue = context.Reader.ReadString();

        return (TStronglyTypedId)Activator.CreateInstance(typeof(TStronglyTypedId), stringValue) ?? throw new InvalidOperationException();
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TStronglyTypedId value)
    {
        context.Writer.WriteString(value?.Value);
    }
}
