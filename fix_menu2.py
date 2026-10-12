import re

target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
with open(target, 'r', encoding='utf-8') as f:
    content = f.read()

# Find the F1 handler
f1_pattern = r'''if \(prev\.IsKeyDown\(Microsoft\.Xna\.Framework\.Input\.Keys\.F1\) && !WasKeyDown\(prev, Microsoft\.Xna\.Framework\.Input\.Keys\.F1\)\)
        \{
        \}'''
f1_replacement = '''if (prev.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1) && !WasKeyDown(prev, Microsoft.Xna.Framework.Input.Keys.F1))
        {
            debugMenuVisible = !debugMenuVisible;
            logger.Info($"[DEBUG] Menu: {(debugMenuVisible ? \"ON\" : \"OFF\")}");
        }'''

if not re.search(f1_pattern, content):
    print("F1 pattern not found")
    import sys; sys.exit(1)

content = re.sub(f1_pattern, f1_replacement, content)

# Add the debug menu fields and methods just before TypeLoadCommand
type_load_marker = 'public void TypeLoadCommand()'
if type_load_marker not in content:
    print("TypeLoadCommand not found")
    import sys; sys.exit(1)

insertion = '''
    private bool debugMenuVisible = false;
    private int debugMenuSelected = 0;
    private string[] debugMenuItems = System.Array.Empty<string>();
    private TextRenderer? menuTitle;
    private TextRenderer? menuItemNormal;
    private TextRenderer? menuItemSelected;
    private TextRenderer? menuHelp;
    private TextRenderer? menuStatus;
    private Microsoft.Xna.Framework.Graphics.Texture2D? menuPanel;
    private Microsoft.Xna.Framework.Graphics.Texture2D? menuHighlight;
    private Microsoft.Xna.Framework.Graphics.Texture2D? menuHeader;

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

'''

content = content.replace(type_load_marker, insertion + type_load_marker)

# Add keyboard handling for menu navigation - after the F1 handler
menu_nav = '''
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
'''
# Insert after the F1 handler
f1_replacement_with_nav = f1_replacement + menu_nav
content = content.replace(f1_replacement, f1_replacement_with_nav, 1)

# Add DrawDebugMenu call in Draw method - after spriteBatch.End()
draw_call = '\n        if (debugMenuVisible) DrawDebugMenu();\n'
if 'DrawDebugMenu();' not in content:
    # Find the spriteBatch.End() in Draw method and add after
    content = content.replace('spriteBatch.End();\n    }\n\n    private void Update', 
                              'spriteBatch.End();' + draw_call + '    }\n\n    private void Update', 1)

with open(target, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updates applied successfully')
