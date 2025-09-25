using System.CommandLine;
using System.Text.Json;

RootCommand rootCmd = new("An application for converting questions in the JSON format to CSV format");
Option<DirectoryInfo> dirOption = new("--directory")
{
    Description = "directory containing all JSON files with questions",
    Required = true
};
dirOption.Aliases.Add("-d");
Option<FileInfo> outputOption = new("--output")
{
    Description = "output file path for a newly created CSV file",
    Required = true,
};
outputOption.Aliases.Add("-o");
rootCmd.Add(dirOption);
rootCmd.Add(outputOption);
rootCmd.SetAction(parseResult =>
{
    return ConvertAll(parseResult);
});
ParseResult parseResult = rootCmd.Parse(args);
return parseResult.Invoke();

int ConvertAll(ParseResult parseResult)
{
    DirectoryInfo parsedDir = parseResult.GetValue(dirOption);
    FileInfo parsedFile = parseResult.GetValue(outputOption);
    if (!parsedDir.Exists)
    {
        Console.WriteLine("the directory doesn't exist");
        return 2;
    }
    string csvString = "Text|Answer|Category|Explanation\n";
    foreach (FileInfo file in parsedDir.GetFiles("*.json"))
    {
        string jsonText = File.ReadAllText(file.FullName);
        List<Question>? questions = JsonSerializer.Deserialize<List<Question>>(jsonText);
        List<string> csvLines = questions.Select(q =>
        {
            return $"{q.Text}|{q.Answer}|{q.Category}|{q.Explanation}";
        }).ToList();
        csvString += string.Join("\n", csvLines);
    }
    File.WriteAllText(parsedFile.FullName, csvString);
    return 0;
}

string ConvertFile(FileInfo fi)
{
    string fileContent = File.ReadAllText(fi.FullName);
    return fileContent;
}
