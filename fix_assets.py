import os

target = r'C:\Dev\retrosystem\spectrum.debug\Program.cs'
new_class = '''internal sealed class FileSystemLoggerAssets : IAssetProvider
{
    private readonly string[] _paths;
    public FileSystemLoggerAssets(string basePath)
    {
        _paths = new[] {
            basePath,
            Path.Combine(basePath, "spectrum"),
            AppDomain.CurrentDomain.BaseDirectory,
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spectrum"),
            @"C:\\Dev\\retrosystem\\spectrum.debug",
            @"C:\\Dev\\retrosystem\\spectrum"
        }.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    public bool Exists(string r)
    {
        if (Path.IsPathRooted(r) && File.Exists(r)) return true;
        foreach (var p in _paths) if (File.Exists(Path.Combine(p, r))) return true;
        return false;
    }
    public byte[]? ReadAllBytes(string r)
    {
        if (Path.IsPathRooted(r) && File.Exists(r)) return File.ReadAllBytes(r);
        foreach (var p in _paths) { var f = Path.Combine(p, r); if (File.Exists(f)) return File.ReadAllBytes(f); }
        return null;
    }
    public IEnumerable<string> ListFiles(string r) => Enumerable.Empty<string>();
}'''

with open(target, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the start of the class and the end (next "}\n}" or end of class)
start_marker = 'internal sealed class FileSystemLoggerAssets : IAssetProvider'
start = content.find(start_marker)
if start < 0:
    print('NOT FOUND')
    import sys; sys.exit(1)

# Find the closing brace of the class (last } before the next class or end of file)
# The class ends with "}\n}" - the last } closes the namespace
# Find the last } before the file ends
end = content.rfind('}')
if end < start:
    print('NO END FOUND')
    import sys; sys.exit(1)

# Also include the trailing newline
new_content = content[:start] + new_class + '\n' + content[end+1:]
with open(target, 'w', encoding='utf-8') as f:
    f.write(new_content)
print(f'Replaced from {start} to {end}')
