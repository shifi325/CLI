using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

class Program
{
    static async Task Main(string[] args)
    {
        // Define options
        var languageOption = new Option<string>(
            name: "--language",
            description: "Programming languages to include (e.g., python, csharp). Use 'all' to include all."
        );

        var outputOption = new Option<string>(
            name: "--output",
            description: "Output file path where the bundle will be saved."
        );

        var noteOption = new Option<bool>(
            name: "--note",
            description: "Include source file paths as comments in the bundle."
        );

        var sortOption = new Option<string>(
            name: "--sort",
            description: "Sort files by 'name' or 'type'. Default is 'name'.",
            getDefaultValue: () => "name"
        );

        var removeEmptyLinesOption = new Option<bool>(
            name: "--remove-empty-lines",
            description: "Remove empty lines from source files."
        );

        var authorOption = new Option<string>(
            name: "--author",
            description: "Author name to include in the bundle header.",
            getDefaultValue: () => string.Empty
        );

        // Create the bundle command
        var bundleCommand = new Command("bundle", "Bundle code files to a single file");

        // Add options to the command
        bundleCommand.AddOption(languageOption);
        bundleCommand.AddOption(outputOption);
        bundleCommand.AddOption(noteOption);
        bundleCommand.AddOption(sortOption);
        bundleCommand.AddOption(removeEmptyLinesOption);
        bundleCommand.AddOption(authorOption);

        // Add handler
        bundleCommand.SetHandler(
            (string language, string output, bool note, string sort, bool removeEmptyLines, string author) =>
            {
                try
                {
                    // Validate parameters
                    if (string.IsNullOrWhiteSpace(output))
                        throw new ArgumentException("Output file path is required.");

                    if (string.IsNullOrWhiteSpace(language))
                        throw new ArgumentException("The --language option is required.");

                    // Create the bundle
                    CreateBundle(language, output, note, sort, removeEmptyLines, author);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            },
            languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption
        );

        // Add the command to the root
        var rootCommand = new RootCommand("Root command for file Bundler CLI");
        rootCommand.AddCommand(bundleCommand);

        // Invoke the command
        await rootCommand.InvokeAsync(args);
    }

    static void CreateBundle(string language, string output, bool note, string sort, bool removeEmptyLines, string author)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var files = Directory.GetFiles(currentDirectory, "*.*", SearchOption.AllDirectories)
            .Where(file => !file.Contains("\\bin\\") && !file.Contains("\\debug\\"))
            .ToList();

        // Filter files by language
        if (language.ToLower() != "all")
        {
            var extensions = GetExtensionsByLanguage(language);
            files = files.Where(file => extensions.Contains(Path.GetExtension(file))).ToList();
        }

        // Sort files
        files = sort.ToLower() switch
        {
            "type" => files.OrderBy(file => Path.GetExtension(file)).ThenBy(file => file).ToList(),
            _ => files.OrderBy(file => file).ToList()
        };

        using var writer = new StreamWriter(output);

        // Add author and header
        if (!string.IsNullOrWhiteSpace(author))
        {
            writer.WriteLine($"// Author: {author}");
            writer.WriteLine($"// Generated on: {DateTime.Now}");
            writer.WriteLine();
        }

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(currentDirectory, file);
            var lines = File.ReadAllLines(file);

            if (note)
                writer.WriteLine($"// File: {relativePath}");

            foreach (var line in lines)
            {
                if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                    continue;

                writer.WriteLine(line);
            }

            writer.WriteLine(); // Add a blank line between files
        }

        Console.WriteLine($"Bundle created successfully at: {output}");
    }

    static List<string> GetExtensionsByLanguage(string language)
    {
        return language.ToLower() switch
        {
            "csharp" => new List<string> { ".cs" },
            "python" => new List<string> { ".py" },
            "javascript" => new List<string> { ".js" },
            "java" => new List<string> { ".java" },
            "cpp" => new List<string> { ".cpp", ".hpp" },
            _ => throw new ArgumentException($"Unsupported language: {language}")
        };
    }
}
