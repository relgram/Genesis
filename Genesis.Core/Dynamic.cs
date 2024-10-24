using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genesis.Core;

[JsonConverter(typeof(DynamicConverter))]
public sealed class Dynamic
{
    private readonly string _value = string.Empty;

    public Dynamic(string value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static Dynamic Empty { get; } = new(string.Empty);

    public static implicit operator bool(Dynamic property) => bool.TryParse(property, out var result) && result;

    public static implicit operator double(Dynamic property) => double.TryParse(property, out var result) ? result : 0;

    public static implicit operator Dynamic(bool value) => new(value.ToString());

    public static implicit operator Dynamic(double value) => new(value.ToString());

    public static implicit operator Dynamic(Guid value) => new(value.ToString());

    public static implicit operator Dynamic(int value) => new(value.ToString());

    public static implicit operator Dynamic(string value) => new(value);

    public static implicit operator Guid(Dynamic property) => Guid.TryParse(property, out var result) ? result : Guid.Empty;

    public static implicit operator int(Dynamic property) => int.TryParse(property, out var result) ? result : 0;

    public static implicit operator string(Dynamic property) => property.ToString();

    public override string ToString() => _value;

    public string ToUpper() => _value.ToUpper();
}

public sealed class DynamicConverter : JsonConverter<Dynamic>
{
    public override Dynamic Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, Dynamic value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}