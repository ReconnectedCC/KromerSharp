using System.Text.Json.Serialization;

namespace Kromer.Models;

public class WebhookMessage
{
    [JsonPropertyName("content")] public string? Content { get; set; }

    [JsonPropertyName("embeds")] public List<Embed> Embeds { get; set; }

    public class Author
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        public Author()
        {
        }

        public Author(string name)
        {
            Name = name;
        }
    }

    public class Embed
    {
        [JsonPropertyName("title")] public string Title { get; set; }

        [JsonPropertyName("color")] public int Color { get; set; }

        [JsonPropertyName("fields")] public List<Field> Fields { get; set; }

        [JsonPropertyName("author")] public Author Author { get; set; }
    }

    public class Field
    {
        [JsonPropertyName("name")] public string Name { get; set; }

        [JsonPropertyName("value")] public string Value { get; set; }

        [JsonPropertyName("inline")] public bool Inline { get; set; }

        public Field()
        {
        }

        public Field(string name, string value, bool inline = false)
        {
            Name = name;
            Value = value;
            Inline = inline;
        }
    }
}