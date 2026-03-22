namespace Catalog.API;

public class Attribute
{
    public Attribute() { }

    public Attribute(string name, List<AttributeValue> values)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Invalid name");

        Values = values;
    }

    public static Attribute Create(string name, List<AttributeValue> values)
        => new Attribute(name, values);

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<AttributeValue> Values { get; set; } = [];

    public AttributeNameEdited EditName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Invalid name");
        Name = name;
        return new AttributeNameEdited(Id, name);
    }

    public AttributeValueAdded AddValue(string text, string bgStyle, string textStyle, string borderStyle)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Invalid value text");

        var value = new AttributeValue
        {
            Text = text.Trim(),
            BgStyle = bgStyle ?? string.Empty,
            TextStyle = textStyle ?? string.Empty,
            BorderStyle = borderStyle ?? string.Empty
        };
        Values.Add(value);

        return new AttributeValueAdded(Id, value);
    }

    public AttributeValueRemoved RemoveValue(string valueText)
    {
        var value = Values.FirstOrDefault(x => x.Text == valueText)
            ?? throw new ArgumentException($"Value {valueText} does not exists");
        Values.Remove(value);
        return new AttributeValueRemoved(Id, valueText);
    }
}

public class AttributeValue
{
    public string Text { get; set; } = string.Empty;
    public string BgStyle { get; set; } = string.Empty;
    public string TextStyle { get; set; } = string.Empty;
    public string BorderStyle { get; set; } = string.Empty;
}
