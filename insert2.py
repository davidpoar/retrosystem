target = r'C:\Dev\retrosystem\spectrum.core\SpectrumGame.cs'
append_file = r'C:\Dev\retrosystem\spectrum.core\InitNoGfx.cs.append'

with open(target, 'r', encoding='utf-8') as f:
    content = f.read()
with open(append_file, 'r', encoding='utf-8') as f:
    methods = f.read()

marker = 'public void TypeLoadCommand()'
idx = content.find(marker)
if idx < 0:
    print('Marker not found!')
    import sys; sys.exit(1)

new_content = content[:idx] + methods + '\n' + content[idx:]
with open(target, 'w', encoding='utf-8') as f:
    f.write(new_content)

import os
os.remove(append_file)
print(f'Inserted at offset {idx}')
