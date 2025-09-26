using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrueFalseBackend.Models;

public class Question
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public bool Answer { get; set; }

    public string? Category { get; set; }

    public string? Explanation { get; set; }
}
