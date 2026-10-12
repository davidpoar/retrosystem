target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
with open(target, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Find the menu-related declarations
# We need to wrap from the menu field declaration to the end of DrawDebugMenu

# Find the start: first occurrence of "private TextRenderer" or "private SpriteFont" for menu
start = None
for i, line in enumerate(lines):
    if 'private TextRenderer' in line or 'private SpriteFont' in line and 'menuFont' in line:
        start = i
        break

if start is None:
    # Try to find by looking for the first menu field
    for i, line in enumerate(lines):
        if 'menuTitle' in line and 'private' in line:
            start = i
            break

if start is None:
    print("Menu start not found")
    import sys; sys.exit(1)

# Find the end: the last line of DrawDebugMenu method
# The method ends with a closing brace followed by another method or end of class
# We'll find the method and then look for the next "private " or "public " declaration
end = None
for i in range(start, len(lines)):
    if i > start and (lines[i].startswith('    private ') or lines[i].startswith('    public ') or lines[i].startswith('}')):
        # Found next method or class end
        end = i
        break

if end is None:
    end = len(lines)

# Wrap with #if WINDOWS / #endif
new_lines = lines[:start] + ['#if WINDOWS\n'] + lines[start:end] + ['#endif\n'] + lines[end:]

with open(target, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print(f'Wrapped lines {start+1} to {end} with #if WINDOWS')
