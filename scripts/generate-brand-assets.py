#!/usr/bin/env python3
"""
CardMaker - Brand Asset Generator
Generates all derived platform-specific icons and web assets from the canonical master logo:
src/CardMaker.UI/wwwroot/branding/logo.png
"""

import os
import sys
import struct
from io import BytesIO
from PIL import Image

def make_icns(img, output_path):
    """
    Creates an Apple ICNS file containing modern PNG-encoded icon chunks:
    ic07 (128x128), ic08 (256x256), ic09 (512x512), ic10 (1024x1024)
    """
    chunks = [
        (b'ic07', 128),
        (b'ic08', 256),
        (b'ic09', 512),
        (b'ic10', 1024),
    ]
    icns_body = bytearray()
    for ostype, size in chunks:
        resized = img.resize((size, size), Image.Resampling.LANCZOS)
        buf = BytesIO()
        resized.save(buf, format='PNG', optimize=True)
        data = buf.getvalue()
        chunk_len = len(data) + 8
        icns_body.extend(ostype)
        icns_body.extend(struct.pack('>I', chunk_len))
        icns_body.extend(data)
    
    total_len = len(icns_body) + 8
    with open(output_path, 'wb') as f:
        f.write(b'icns')
        f.write(struct.pack('>I', total_len))
        f.write(icns_body)

def main():
    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
    
    # Check source: either canonical destination or root logo.png during first run
    canonical_src = os.path.join(repo_root, 'src', 'CardMaker.UI', 'wwwroot', 'branding', 'logo.png')
    root_src = os.path.join(repo_root, 'logo.png')
    
    if os.path.exists(canonical_src):
        src_path = canonical_src
    elif os.path.exists(root_src):
        src_path = root_src
    else:
        print(f"Error: Neither '{canonical_src}' nor '{root_src}' found.", file=sys.stderr)
        sys.exit(1)
        
    print(f"Using source logo: {src_path}")
    img = Image.open(src_path).convert('RGBA')
    print(f"Source image dimensions: {img.size[0]}x{img.size[1]}")
    
    # 1. Ensure canonical destination directory
    branding_dir = os.path.join(repo_root, 'src', 'CardMaker.UI', 'wwwroot', 'branding')
    os.makedirs(branding_dir, exist_ok=True)
    dest_logo = os.path.join(branding_dir, 'logo.png')
    if src_path != dest_logo:
        import shutil
        shutil.copy2(src_path, dest_logo)
        print(f"Copied canonical master logo to: {dest_logo}")

    # 2. Web Assets
    web_wwwroot = os.path.join(repo_root, 'src', 'CardMaker.Web', 'wwwroot')
    os.makedirs(web_wwwroot, exist_ok=True)
    
    # favicon.ico (multi-resolution 16, 32, 48)
    ico_sizes = [(16, 16), (32, 32), (48, 48)]
    img.save(os.path.join(web_wwwroot, 'favicon.ico'), format='ICO', sizes=ico_sizes)
    print("Generated Web favicon.ico")
    
    # Web PNG icons
    web_pngs = {
        'favicon.png': 32,
        'favicon-16x16.png': 16,
        'favicon-32x32.png': 32,
        'apple-touch-icon.png': 180,
        'icon-192.png': 192,
        'icon-512.png': 512,
    }
    for filename, size in web_pngs.items():
        resized = img.resize((size, size), Image.Resampling.LANCZOS)
        resized.save(os.path.join(web_wwwroot, filename), format='PNG', optimize=True)
        print(f"Generated Web {filename} ({size}x{size})")

    # 3. Desktop Assets
    desktop_root = os.path.join(repo_root, 'src', 'CardMaker.Desktop')
    desktop_wwwroot = os.path.join(desktop_root, 'wwwroot')
    os.makedirs(desktop_wwwroot, exist_ok=True)
    
    # Desktop Windows ICO (16, 32, 48, 64, 128, 256)
    desktop_ico_sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    desktop_ico_path = os.path.join(desktop_root, 'icon.ico')
    img.save(desktop_ico_path, format='ICO', sizes=desktop_ico_sizes)
    img.save(os.path.join(desktop_wwwroot, 'icon.ico'), format='ICO', sizes=desktop_ico_sizes)
    print("Generated Desktop icon.ico")
    
    # Desktop icon.png
    desktop_png = img.resize((512, 512), Image.Resampling.LANCZOS)
    desktop_png.save(os.path.join(desktop_wwwroot, 'icon.png'), format='PNG', optimize=True)
    desktop_png.save(os.path.join(desktop_root, 'icon.png'), format='PNG', optimize=True)
    print("Generated Desktop icon.png (512x512)")
    
    # macOS ICNS
    desktop_icns_path = os.path.join(desktop_root, 'icon.icns')
    make_icns(img, desktop_icns_path)
    print("Generated Desktop icon.icns")
    
    # Linux FreeDesktop Icons
    hicolor_root = os.path.join(desktop_root, 'Resources', 'icons', 'hicolor')
    for sz in [16, 32, 48, 64, 128, 256, 512]:
        sz_dir = os.path.join(hicolor_root, f"{sz}x{sz}", "apps")
        os.makedirs(sz_dir, exist_ok=True)
        r = img.resize((sz, sz), Image.Resampling.LANCZOS)
        r.save(os.path.join(sz_dir, 'cardmaker.png'), format='PNG', optimize=True)
    print("Generated Linux hicolor icons (16px to 512px)")
    
    print("\nAll brand assets generated successfully!")

if __name__ == '__main__':
    main()
