using System.Text;
using VnAddressSanitizer;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/VnAddressSanitizer.Runner -- <input.txt>");
    Console.Error.WriteLine("Each line in the file is treated as one address to sanitize.");
    return 1;
}

var inputFile = args[0];
if (!File.Exists(inputFile))
{
    Console.Error.WriteLine($"File not found: {inputFile}");
    return 1;
}

var outputFile = Path.GetFileNameWithoutExtension(inputFile) + "_output.txt";
var lines = File.ReadAllLines(inputFile, Encoding.UTF8);

int total = 0, changed = 0, unchanged = 0;
var sb = new StringBuilder();

foreach (var line in lines)
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    total++;

    var sanitized = AddressSanitizer.Sanitize(line);
    bool isChanged = !string.Equals(line.Trim(), sanitized, StringComparison.Ordinal);

    if (isChanged)
    {
        changed++;
        sb.AppendLine($"[{total:D3}] Changed");
        sb.AppendLine($"  IN:  {line.Trim()}");
        sb.AppendLine($"  OUT: {sanitized}");
    }
    else
    {
        unchanged++;
        sb.AppendLine($"[{total:D3}] No change");
        sb.AppendLine($"  IN:  {line.Trim()}");
        sb.AppendLine($"  OUT: <same>");
    }
    sb.AppendLine();
}

sb.AppendLine($"Total: {total} | Changed: {changed} | Unchanged: {unchanged}");

File.WriteAllText(outputFile, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"Output written to: {outputFile}");
Console.WriteLine($"Total: {total} | Changed: {changed} | Unchanged: {unchanged}");

return 0;
