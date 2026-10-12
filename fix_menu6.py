import re

target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
with open(target, 'r', encoding='utf-8') as f:
    content = f.read()

# Verify the F1 handler exists with empty body
f1_empty = r'''        if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !WasKeyDown(prev, Microsoft.Xna.Framework.Input.Keys.F1))
        {
        }'''

if f1_empty not in content:
    print("F1 handler with empty body not found - checking current state")
    # Check what the F1 handler looks like
    for i, line in enumerate(content.split('\n')):
        if 'F1' in line:
            print(f"Line {i+1}: {line}")
else:
    print("F1 handler with empty body found - proceeding")

# Find the exact text after the F1 handler
# Look for the empty F1 handler pattern and add our code inside
f1_pattern = r'if \(prev\.IsKeyDown\(Microsoft\.Xna\.Framework\.Input\.Keys\.F1\) && !WasKeyDown\(prev, Microsoft\.Xna\.Framework\.Input\.Keys\.F1\)\)\n        \{\n        \}'

if not re.search(f1_pattern, content):
    print("F1 empty pattern not found")
    import sys; sys.exit(1)

# Add the menu toggle in the F1 handler
f1_replacement = '''if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !WasKeyDown(prev, Microsoft.Xna.Framework.Input.Keys.F1))
        {
#if WINDOWS
            debugMenuVisible = !debugMenuVisible;
            logger.Info($"[DEBUG] Menu: {(debugMenuVisible ? "ON" : "OFF")}");
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
            }
#endif
        }'''

content = re.sub(f1_pattern, f1_replacement, content, count=1)
print("F1 handler updated with #if WINDOWS guard")

# Find the TypeLoadCommand method and add the menu code BEFORE it
type_load_pattern = r'    public void TypeLoadCommand\(\)\n    \{'
type_load_match = re.search(type_load_pattern, content)
if not type_load_match:
    print("TypeLoadCommand method not found")
    import sys; sys.exit(1)

type_load_start = type_load_match.start()

# Menu code wrapped in #if WINDOWS
menu_code = '''#if WINDOWS
    public bool debugMenuVisible = false;
    public int debugMenuSelected = 0;
    public string[] debugMenuItems = System.Array.Empty<string>();
    public TextRenderer? menuTitle;
    public TextRenderer? menuItemNormal;
    public TextRenderer? menuItemSelected;
    public TextRenderer? menuHelp;
    public TextRenderer? menuStatus;
    public Microsoft.Xna.Framework.Graphics.Texture2D? menuPanel;
    public Microsoft.Xna.Framework.Graphics.Texture2D? menuHighlight;
    public Microsoft.Xna.Framework.Graphics.Texture2D? menuHeader;

    private void InitDebugMenu()
    {
        if (menuTitle != null) return;
        debugMenuItems = new[] {
            "1: Inject LOAD escape + Enter",
            "2: TypeLoadCommand (auto-typing)",
            "3: Reset CPU",
            "4: Toggle Tape",
            "5: Rewind Tape",
            "6: Advance 100 frames",
            "7: Advance 500 frames",
            "8: Dump state to console",
            "9: Dump buffer 0x5C00",
            "0: Close menu (ESC)"
        };
        try
        {
            var titleColor = new Microsoft.Xna.Framework.Color(255, 220, 100);
            var itemColor = new Microsoft.Xna.Framework.Color(220, 220, 220);
            var selColor = new Microsoft.Xna.Framework.Color(255, 255, 100);
            var helpColor = new Microsoft.Xna.Framework.Color(150, 150, 180);
            menuTitle = new TextRenderer(GraphicsDevice, "> ZX SPECTRUM 48K DEBUG <", 320, 14, titleColor, null);
            menuHelp = new TextRenderer(GraphicsDevice, "ARROWS / ENTER / ESC", 320, 12, helpColor, null);
            menuItemNormal = new TextRenderer(GraphicsDevice, debugMenuItems[0], 320, 12, itemColor, null);
            menuItemSelected = new TextRenderer(GraphicsDevice, "> " + debugMenuItems[0], 320, 12, selColor, null);
            menuStatus = new TextRenderer(GraphicsDevice, "PC:0000 A:00", 200, 12, new Microsoft.Xna.Framework.Color(180, 220, 255), null);
            menuPanel = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, 1, 1);
            menuPanel.SetData(new[] { new Microsoft.Xna.Framework.Color(20, 20, 40, 220) });
            menuHighlight = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, 1, 1);
            menuHighlight.SetData(new[] { new Microsoft.Xna.Framework.Color(60, 80, 160, 200) });
            menuHeader = new Microsoft.Xna.Framework.Graphics.Texture2D(GraphicsDevice, 1, 1);
            menuHeader.SetData(new[] { new Microsoft.Xna.Framework.Color(40, 60, 120, 255) });
        }
        catch (Exception ex)
        {
            logger.Error($"InitDebugMenu failed: {ex.Message}");
        }
    }

    private void DrawDebugMenu()
    {
        if (!debugMenuVisible) return;
        if (menuTitle == null) InitDebugMenu();
        if (menuTitle == null || menuPanel == null) return;
        try
        {
            int screenW = GraphicsDevice.Viewport.Width;
            int screenH = GraphicsDevice.Viewport.Height;
            int panelW = 480;
            int panelH = 280;
            int px = (screenW - panelW) / 2;
            int py = (screenH - panelH) / 2;
            spriteBatch.Begin();
            spriteBatch.Draw(menuPanel, new Microsoft.Xna.Framework.Rectangle(px, py, panelW, panelH), Microsoft.Xna.Framework.Color.White);
            spriteBatch.Draw(menuHeader, new Microsoft.Xna.Framework.Rectangle(px, py, panelW, 18), Microsoft.Xna.Framework.Color.White);
            menuTitle.Draw(spriteBatch, px + 6, py + 2);
            int itemY = py + 24;
            int itemH = 18;
            for (int i = 0; i < debugMenuItems.Length; i++)
            {
                int y = itemY + i * itemH;
                if (i == debugMenuSelected)
                {
                    spriteBatch.Draw(menuHighlight, new Microsoft.Xna.Framework.Rectangle(px + 2, y - 2, panelW - 4, itemH), Microsoft.Xna.Framework.Color.White);
                    menuItemSelected.Draw(spriteBatch, px + 6, y);
                }
                else
                {
                    var t = new TextRenderer(GraphicsDevice, "  " + debugMenuItems[i], 320, 12, new Microsoft.Xna.Framework.Color(200, 200, 200), null);
                    t.Draw(spriteBatch, px + 6, y);
                    if (t.Texture != null) t.Texture.Dispose();
                }
            }
            string status = "PC:" + GetPC().ToString("X4") + " A:" + GetA().ToString("X2") + " CYC:" + GetCycles();
            var s = new TextRenderer(GraphicsDevice, status, 200, 12, new Microsoft.Xna.Framework.Color(180, 220, 255), null);
            s.Draw(spriteBatch, px + 6, py + panelH - 30);
            if (s.Texture != null) s.Texture.Dispose();
            menuHelp.Draw(spriteBatch, px + 6, py + panelH - 16);
            spriteBatch.End();
        }
        catch (Exception ex)
        {
            logger.Error($"DrawDebugMenu failed: {ex.Message}");
        }
    }

    private void ExecuteDebugMenuItem()
    {
        switch (debugMenuSelected)
        {
            case 0: logger.Info("[MENU] InjectLoadToBuffer"); InjectLoadToBuffer(); break;
            case 1: logger.Info("[MENU] TypeLoadCommand"); TypeLoadCommand(); break;
            case 2: logger.Info("[MENU] ResetCpu"); ResetCpu(); break;
            case 3: logger.Info("[MENU] ToggleTape: " + ToggleTape()); break;
            case 4: logger.Info("[MENU] RewindTape"); RewindTape(); break;
            case 5: logger.Info("[MENU] Advance 100 frames"); AdvanceFrames(100); break;
            case 6: logger.Info("[MENU] Advance 500 frames"); AdvanceFrames(500); break;
            case 7: DumpDebugInfo(); break;
            case 8: System.Console.WriteLine("Buffer 0x5C00: " + DumpInputBuffer()); break;
            case 9: debugMenuVisible = false; break;
        }
    }
#endif

'''

content = content[:type_load_start] + menu_code + content[type_load_start:]
print("Menu code inserted before TypeLoadCommand")

# Now add the DrawDebugMenu call in Draw method
# Find the Draw method end
draw_pattern = r'(spriteBatch\.End\(\);)\n(    \}\n\n    protected override void Update)'
if re.search(draw_pattern, content):
    new_content = re.sub(draw_pattern, r'\1\n\n#if WINDOWS\n        if (debugMenuVisible) DrawDebugMenu();\n#endif\n\2', content, count=1)
    print("DrawDebugMenu call added to Draw method")
else:
    print("Draw pattern not found - DrawDebugMenu call not added")
    new_content = content

with open(target, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("All changes applied successfully")
