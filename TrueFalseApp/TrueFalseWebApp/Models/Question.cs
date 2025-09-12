namespace TrueFalseWebApp.Models;

public record Question(
    int Id,
    string Text,
    bool Answer
);