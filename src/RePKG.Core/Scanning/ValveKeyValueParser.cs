using System.Text;

namespace RePKG.Core.Scanning;

public static class ValveKeyValueParser
{
    public static ValveKeyValueNode Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var tokens = Tokenize(content);
        var index = 0;
        return ParseObject(tokens, ref index, false);
    }

    private static ValveKeyValueNode ParseObject(IReadOnlyList<string> tokens, ref int index, bool expectClosingBrace)
    {
        var node = new ValveKeyValueNode();
        while (index < tokens.Count)
        {
            var key = tokens[index++];
            if (key == "}")
            {
                if (!expectClosingBrace)
                {
                    throw new FormatException("The VDF contains an unexpected closing brace.");
                }

                return node;
            }

            if (index >= tokens.Count)
            {
                throw new FormatException($"The VDF key '{key}' does not have a value.");
            }

            if (tokens[index] == "{")
            {
                index++;
                node.Add(key, ParseObject(tokens, ref index, true));
            }
            else
            {
                node.Add(key, ValveKeyValueNode.FromValue(tokens[index++]));
            }
        }

        if (expectClosingBrace)
        {
            throw new FormatException("The VDF object is missing a closing brace.");
        }

        return node;
    }

    private static IReadOnlyList<string> Tokenize(string content)
    {
        var tokens = new List<string>();
        var index = 0;
        while (index < content.Length)
        {
            if (char.IsWhiteSpace(content[index]))
            {
                index++;
                continue;
            }

            if (content[index] == '/' && index + 1 < content.Length && content[index + 1] == '/')
            {
                index += 2;
                while (index < content.Length && content[index] != '\n')
                {
                    index++;
                }

                continue;
            }

            if (content[index] is '{' or '}')
            {
                tokens.Add(content[index++].ToString());
                continue;
            }

            tokens.Add(content[index] == '"'
                ? ReadQuoted(content, ref index)
                : ReadBare(content, ref index));
        }

        return tokens;
    }

    private static string ReadQuoted(string content, ref int index)
    {
        index++;
        var value = new StringBuilder();
        while (index < content.Length)
        {
            var character = content[index++];
            if (character == '"')
            {
                return value.ToString();
            }

            if (character == '\\' && index < content.Length)
            {
                var escaped = content[index++];
                value.Append(escaped switch
                {
                    'n' => '\n',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => escaped,
                });
            }
            else
            {
                value.Append(character);
            }
        }

        throw new FormatException("The VDF string is missing a closing quote.");
    }

    private static string ReadBare(string content, ref int index)
    {
        var start = index;
        while (index < content.Length && !char.IsWhiteSpace(content[index]) && content[index] is not '{' and not '}')
        {
            index++;
        }

        return content[start..index];
    }
}
