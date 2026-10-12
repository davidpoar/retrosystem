import re

target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
with open(target, 'r', encoding='utf-8') as f:
    content = f.read()

# Add the F1 handler with menu toggle
f1_old = '''if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !WasKeyDown(prev, Microsoft.Xna.Framework.Input.Keys.F1))
        {
        }'''
f1_new = '''if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !WasKeyDown(prev, Microsoft.Xna.Framework.Input.Keys.F1))
        {
            debugMenuVisible = !debugMenuVisible;
            logger.Info($"[DEBUG] Menu: {(debugMenuVisible ? "ON" : "OFF")}");
        }

        if (debugMenuVisible)
        {
            if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up))
                debugMenuSelected = Math.Max(0, debugMenuSelected - 1);
            if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down))
                debugMenuSelected = Math.Min(debugMenuItems.Length - 1, debugMenuSelected + 1);
            if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Enter))
                ExecuteDebugMenuItem();
            if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                debugMenuVisible = false;
        }'''

if f1_old in content:
    content = content.replace(f1_old, f1_new, 1)
    print("F1 handler updated")
else:
    print("F1 pattern not found")
    import sys; sys.exit(1)

# Find the debugMenuVisible field declaration and wrap with the menu code in #if WINDOWS
# We need to find a marker
marker = '    public bool debugMenuVisible = false;'
if marker in content:
    # Find the end of the menu-related code block - look for the next method that is NOT a menu method
    # Find a non-menu method/field after debugMenuVisible
    # The menu fields end with a property that is NOT about the menu
    # Look for the next line that starts with a non-menu field

    pos = content.find(marker)
    # Find the end of the entire menu section by finding the next non-menu field
    # The menu code ends before "public bool TypeLoadCommand" or similar
    # Find a closing brace } that matches
    # Actually, find the method that contains the menu code
    # We need to find the end of the last menu method (DrawDebugMenu or ExecuteDebugMenuItem)

    # Find the end of ExecuteDebugMenuItem method
    exec_start = content.find('private void ExecuteDebugMenuItem()')
    if exec_start < 0:
        print("ExecuteDebugMenuItem not found")
        import sys; sys.exit(1)

    # Find the matching closing brace
    # Find the class structure: count braces
    brace_count = 0
    end_pos = exec_start
    in_method = False
    for i in range(exec_start, len(content)):
        c = content[i]
        if c == '{':
            brace_count += 1
            in_method = True
        elif c == '}':
            brace_count -= 1
            if in_method and brace_count == 0:
                end_pos = i + 1
                break

    if end_pos == exec_start:
        print("Could not find end of ExecuteDebugMenuItem")
        import sys; sys.exit(1)

    # Find the start of the menu section (debugMenuVisible)
    start_pos = content.find('    public bool debugMenuVisible = false;')
    if start_pos < 0:
        print("debugMenuVisible not found")
        import sys; sys.exit(1)

    # Go back to start of the field declaration (it might be preceded by a comment or blank line)
    # Find the start of the line
    line_start = content.rfind('\n', 0, start_pos) + 1

    # Find the field type TextRenderer which is the first one
    # We want to wrap from the first TextRenderer field
    text_renderer_pos = content.find('    private TextRenderer?', line_start)
    if text_renderer_pos > 0 and text_renderer_pos < end_pos:
        line_start = content.rfind('\n', 0, text_renderer_pos) + 1

    # Insert #if WINDOWS before the menu section
    new_content = content[:line_start] + '#if WINDOWS\n' + content[line_start:end_pos] + '\n#endif\n' + content[end_pos:]

    with open(target, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print(f"Wrapped menu section ({line_start} to {end_pos}) with #if WINDOWS/#endif")
else:
    print("debugMenuVisible field not found")
    import sys; sys.exit(1)
