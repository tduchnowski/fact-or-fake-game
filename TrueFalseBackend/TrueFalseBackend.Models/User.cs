using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueFalseBackend.Models;

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
