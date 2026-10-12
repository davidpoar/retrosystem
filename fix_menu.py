target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
with open(target, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the menu section - it has "// WINDOWS" markers now
# Find the start (just before the first menu field)
start_marker = '    private TextRenderer? menuTitle;'
idx = content.find(start_marker)
if idx < 0:
    print('Start marker not found')
    import sys; sys.exit(1)

# Find the end of DrawDebugMenu method (next non-menu field or method)
# The method ends with a closing brace. Look for the next "private " or "public " after the closing brace
# First find the start of the method after InitMenuGraphics
draw_start = content.find('    private void DrawDebugMenu()', idx)
if draw_start < 0:
    print('DrawDebugMenu not found')
    import sys; sys.exit(1)

# Find the closing brace of DrawDebugMenu - count braces from the opening
brace_count = 0
in_method = False
draw_end = -1
for i in range(draw_start, len(content)):
    c = content[i]
    if c == '{':
        brace_count += 1
        in_method = True
    elif c == '}':
        brace_count -= 1
        if in_method and brace_count == 0:
            draw_end = i + 1
            break

if draw_end < 0:
    print('DrawDebugMenu end not found')
    import sys; sys.exit(1)

# Remove any existing #if WINDOWS / #endif in the menu area
# Then add them properly

# Find the start of the menu area (just before the first menu field)
menu_area_start = idx
# Go back to find the previous line
line_start = content.rfind('\n', 0, menu_area_start) + 1

# Remove existing #if WINDOWS / #endif between line_start and draw_end
section = content[line_start:draw_end]
# Remove the #if WINDOWS and #endif lines
import re
section = re.sub(r'^#if WINDOWS\s*\n', '', section, flags=re.MULTILINE)
section = re.sub(r'^#endif\s*\n', '', section, flags=re.MULTILINE)

# Now prepend #if WINDOWS and append #endif
new_section = '#if WINDOWS\n' + section + '\n#endif\n'

# Replace the section
new_content = content[:line_start] + new_section + content[draw_end:]

with open(target, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f'Wrapped menu section from {line_start+1} to {draw_end}')
