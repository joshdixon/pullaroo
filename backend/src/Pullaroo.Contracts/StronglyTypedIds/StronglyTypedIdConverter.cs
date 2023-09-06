using System.ComponentModel;
using System.Globalization;

namespace Pullaroo.Contracts.StronglyTypedIds;

public class StronglyTypedIdConverter : TypeConverter
{
    private readonly Type _type;

    public StronglyTypedIdConverter(Type type)
    {
        _type = type;
    }

    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => 
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => 
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is string s)
        {
            var factory = StronglyTypedIdHelper.GetFactory(_type);
            return factory(s);
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (value is StronglyTypedId id && destinationType == typeof(string))
            return id.Value;
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class StronglyTypedIdConverterAttribute : Attribute
{
    public Type StronglyTypedIdType { get; }

    public StronglyTypedIdConverterAttribute(Type stronglyTypedIdType)
    {
        if (!StronglyTypedIdHelper.IsStronglyTypedId(stronglyTypedIdType))
            throw new ArgumentException($"The type '{stronglyTypedIdType}' is not an StronglyTypedId", nameof(stronglyTypedIdType));

        StronglyTypedIdType = stronglyTypedIdType;
    }
}
