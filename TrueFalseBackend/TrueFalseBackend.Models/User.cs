using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TrueFalseBackend.Models;

// the user as it is in the DB
public class User
{
    [Key]
    [MaxLength(32)]
    public required string Username { get; set; }

    [Column("time_first_activity")]
    public DateTime TimeFirstActivity { get; set; }

    [Column("time_last_activity")]
    public DateTime? TimeLastActivity { get; set; }

    [Column("rooms_created")]
    public int RoomsCreated { get; set; } = 0;

    [Column("games_played")]
    public int GamesPlayed { get; set; } = 0;
}

// user inside of Init Data string from Telegram
public record WebAppUser
(
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("last_name")] string? LastName,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("photo_url")] string? PhotoUrl
);
