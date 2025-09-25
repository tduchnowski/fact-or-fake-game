using System.Text.Json.Serialization;

internal class Question
{
    [JsonPropertyName("question")]
    public required string Text { get; set; }
    [JsonPropertyName("answer")]
    public required bool Answer { get; set; }
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}
