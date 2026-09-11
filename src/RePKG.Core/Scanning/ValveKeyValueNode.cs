namespace RePKG.Core.Scanning;

public sealed class ValveKeyValueNode
{
    private readonly Dictionary<string, ValveKeyValueNode> _children = new(StringComparer.OrdinalIgnoreCase);

    public string? Value { get; private init; }

    public IReadOnlyDictionary<string, ValveKeyValueNode> Children => _children;

    public string? GetValue(string key)
    {
        return _children.TryGetValue(key, out var node) ? node.Value : null;
    }

    internal void Add(string key, ValveKeyValueNode value)
    {
        _children[key] = value;
    }

    internal static ValveKeyValueNode FromValue(string value)
    {
        return new ValveKeyValueNode { Value = value };
    }
}
