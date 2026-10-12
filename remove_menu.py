target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
with open(target, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the menu block to remove
# It's between "private TextRenderer? menuTitle;" and the next "private " field
# Find "#if WINDOWS" before the menu code
start = content.find('    private TextRenderer? menuTitle;')
if start < 0:
    print("Menu code not found")
    import sys; sys.exit(1)

# Go back to find the #if WINDOWS line
line_start = content.rfind('\n', 0, start) + 1
if_start = content.rfind('#if WINDOWS', 0, start)
if if_start > 0:
    line_start = content.rfind('\n', 0, if_start) + 1

# Find the closing #endif
# Look for the matching #endif after DrawDebugMenu
draw_start = content.find('    private void DrawDebugMenu()')
if draw_start < 0:
    print("DrawDebugMenu not found")
    import sys; sys.exit(1)

# Find the closing brace of DrawDebugMenu
brace_count = 0
draw_end = -1
in_method = False
for i in range(draw_start, len(content)):
    c = content[i]
    if c == '{':
        brace_count += 1
        in_method = True
    elif c == '}':
        brace_count -= 1
        if in_method and brace_count == 0:
            # The closing brace is here
            # Find the next line end
            end_of_line = content.find('\n', i)
            if end_of_line > 0:
                draw_end = end_of_line + 1
            else:
                draw_end = i + 1
            break

if draw_end < 0:
    print("DrawDebugMenu end not found")
    import sys; sys.exit(1)

# Find the #endif after this
endif_pos = content.find('#endif', draw_end)
if endif_pos > 0:
    # Include the #endif line
    draw_end = content.find('\n', endif_pos) + 1

# Remove the section
new_content = content[:line_start] + content[draw_end:]

with open(target, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f'Removed menu section from {line_start+1} to {draw_end}')
