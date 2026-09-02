"""Pixel-art office sprites for Game Dev Studio. y=0 is the top of the PNG."""
from __future__ import annotations

import os
import uuid
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Office"))

NONE = (0, 0, 0, 0)


def new(w: int, h: int):
    return [[NONE for _ in range(w)] for _ in range(h)]


def put(px, x, y, c):
    if 0 <= y < len(px) and 0 <= x < len(px[0]) and c[3] > 0:
        px[y][x] = c


def rect(px, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(px, x, y, c)


def hline(px, x0, x1, y, c):
    for x in range(x0, x1 + 1):
        put(px, x, y, c)


def vline(px, x, y0, y1, c):
    for y in range(y0, y1 + 1):
        put(px, x, y, c)


def save(name, px, pivot_x=0.5, pivot_y=0.5):
    h = len(px)
    w = len(px[0])
    img = Image.new("RGBA", (w, h))
    img.putdata([px[y][x] for y in range(h) for x in range(w)])
    os.makedirs(ROOT, exist_ok=True)
    path = os.path.join(ROOT, name + ".png")
    img.save(path)
    write_meta(path, w, h, pivot_x, pivot_y)
    print("wrote", path, w, "x", h)


def write_meta(png_path, w, h, pivot_x, pivot_y):
    guid = uuid.uuid4().hex
    if pivot_y < 0.1:
        alignment = 7
        pivot_x, pivot_y = 0.5, 0.0
    elif abs(pivot_y - 0.5) < 0.05 and abs(pivot_x - 0.5) < 0.05:
        alignment = 0
    else:
        alignment = 9
    meta = f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 1
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 256
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 0
  spriteMeshType: 1
  alignment: {alignment}
  spritePivot: {{x: {pivot_x}, y: {pivot_y}}}
  spritePixelsToUnits: 16
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 256
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {guid[:16]}
    internalID: 1537655665
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(png_path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)


def floor_tile():
    px = new(16, 16)
    plank = [(168, 118, 70, 255), (158, 108, 62, 255), (176, 126, 78, 255)]
    gap = (112, 74, 42, 255)
    nail = (92, 60, 34, 255)
    for y in range(16):
        band = y // 4
        for x in range(16):
            c = plank[(band + x // 5) % 3]
            if y % 4 == 0:
                c = gap
            if (x + band * 3) % 7 == 2 and y % 4 == 2:
                c = nail
            put(px, x, y, c)
    return px


def wall_tile():
    px = new(16, 16)
    plaster = (214, 196, 168, 255)
    plaster2 = (204, 184, 154, 255)
    rail = (122, 86, 62, 255)
    rail_d = (96, 66, 48, 255)
    shadow = (188, 166, 136, 255)
    for y in range(16):
        for x in range(16):
            c = plaster if (x + y) % 5 else plaster2
            if y >= 12:
                c = rail if y != 12 and y != 15 else rail_d
            if y == 11:
                c = shadow
            put(px, x, y, c)
    return px


def desk():
    px = new(24, 18)
    ink = (36, 26, 22, 255)
    wood = (142, 96, 54, 255)
    wood_h = (168, 118, 68, 255)
    wood_d = (108, 72, 40, 255)
    leg = (72, 50, 34, 255)
    bezel = (40, 44, 52, 255)
    screen = (70, 168, 196, 255)
    screen_h = (190, 230, 240, 255)
    key = (58, 62, 70, 255)
    key_h = (90, 94, 104, 255)
    mug = (200, 86, 70, 255)
    coffee = (62, 38, 24, 255)
    rect(px, 3, 16, 5, 17, leg)
    rect(px, 18, 16, 20, 17, leg)
    rect(px, 1, 10, 22, 15, wood)
    hline(px, 1, 22, 10, wood_h)
    hline(px, 1, 22, 15, wood_d)
    vline(px, 1, 10, 15, ink)
    vline(px, 22, 10, 15, ink)
    rect(px, 6, 3, 17, 11, bezel)
    rect(px, 7, 4, 16, 10, screen)
    put(px, 8, 5, screen_h)
    put(px, 9, 5, screen_h)
    put(px, 10, 6, screen_h)
    rect(px, 8, 12, 15, 14, key)
    hline(px, 8, 15, 12, key_h)
    rect(px, 18, 8, 21, 11, mug)
    rect(px, 19, 9, 20, 10, coffee)
    put(px, 21, 9, mug)
    return px


def empty_slot():
    px = new(24, 16)
    dash = (96, 74, 58, 200)
    for x in range(24):
        for y in range(16):
            edge = x in (0, 23) or y in (0, 15)
            if edge and ((x + y) % 3 != 1):
                put(px, x, y, dash)
            if not edge and 6 <= y <= 9 and 6 <= x <= 17 and (x + y) % 5 == 0:
                put(px, x, y, (96, 74, 58, 90))
    return px


def door():
    px = new(16, 24)
    frame = (64, 42, 32, 255)
    wood = (128, 82, 48, 255)
    wood_d = (96, 60, 36, 255)
    panel = (150, 100, 60, 255)
    knob = (214, 176, 72, 255)
    rect(px, 0, 0, 15, 23, frame)
    rect(px, 2, 1, 13, 22, wood)
    rect(px, 3, 3, 12, 10, panel)
    rect(px, 3, 13, 12, 20, panel)
    vline(px, 7, 3, 20, wood_d)
    vline(px, 8, 3, 20, wood_d)
    rect(px, 11, 12, 13, 14, knob)
    put(px, 13, 13, (255, 220, 120, 255))
    return px


def coffee():
    px = new(16, 20)
    ink = (28, 24, 22, 255)
    body = (74, 78, 86, 255)
    body_h = (110, 114, 124, 255)
    body_d = (48, 50, 56, 255)
    tray = (40, 36, 34, 255)
    cup = (236, 232, 224, 255)
    coffee_c = (58, 36, 22, 255)
    drip = (186, 120, 48, 255)
    led = (90, 210, 110, 255)
    rect(px, 2, 16, 13, 19, tray)
    rect(px, 3, 4, 12, 16, body)
    rect(px, 4, 5, 11, 8, body_h)
    rect(px, 4, 9, 11, 11, body_d)
    hline(px, 3, 12, 4, ink)
    put(px, 5, 6, led)
    rect(px, 6, 12, 9, 15, cup)
    rect(px, 7, 13, 8, 14, coffee_c)
    put(px, 7, 11, drip)
    put(px, 8, 11, drip)
    return px


def toilet():
    px = new(16, 18)
    porcelain = (232, 232, 238, 255)
    shade = (196, 198, 210, 255)
    water = (150, 190, 210, 255)
    seat = (210, 210, 220, 255)
    tank = (220, 220, 228, 255)
    ink = (120, 122, 136, 255)
    floor = (90, 90, 98, 255)
    rect(px, 3, 16, 12, 17, floor)
    rect(px, 4, 8, 11, 15, porcelain)
    rect(px, 5, 10, 10, 14, water)
    hline(px, 4, 11, 8, seat)
    rect(px, 5, 1, 10, 8, tank)
    vline(px, 5, 1, 8, shade)
    vline(px, 10, 1, 8, shade)
    put(px, 9, 3, ink)
    return px


def sofa():
    px = new(26, 14)
    ink = (48, 28, 28, 255)
    cloth = (156, 64, 64, 255)
    cloth_h = (186, 88, 88, 255)
    cloth_d = (118, 46, 46, 255)
    leg = (62, 40, 32, 255)
    rect(px, 2, 12, 4, 13, leg)
    rect(px, 21, 12, 23, 13, leg)
    rect(px, 1, 6, 24, 12, cloth)
    hline(px, 1, 24, 6, cloth_h)
    hline(px, 1, 24, 12, ink)
    rect(px, 0, 3, 4, 11, cloth_d)
    rect(px, 21, 3, 25, 11, cloth_d)
    rect(px, 5, 4, 12, 8, cloth_h)
    rect(px, 13, 4, 20, 8, cloth_h)
    vline(px, 12, 4, 11, cloth_d)
    return px


def plant():
    px = new(14, 18)
    pot = (176, 92, 64, 255)
    pot_d = (128, 64, 44, 255)
    dirt = (74, 50, 34, 255)
    leaf = (62, 140, 72, 255)
    leaf_h = (110, 186, 96, 255)
    leaf_d = (36, 96, 52, 255)
    rect(px, 4, 12, 9, 17, pot)
    hline(px, 4, 9, 12, pot_d)
    rect(px, 5, 11, 8, 12, dirt)
    rect(px, 6, 4, 7, 11, leaf_d)
    put(px, 3, 6, leaf)
    put(px, 2, 7, leaf_h)
    put(px, 4, 5, leaf_h)
    put(px, 5, 6, leaf)
    put(px, 8, 5, leaf)
    put(px, 9, 4, leaf_h)
    put(px, 10, 6, leaf)
    put(px, 11, 7, leaf_d)
    put(px, 7, 3, leaf_h)
    put(px, 6, 2, leaf)
    put(px, 5, 3, leaf_d)
    return px


def window():
    px = new(16, 16)
    frame = (86, 64, 48, 255)
    sky = (92, 148, 196, 255)
    sky_h = (168, 206, 230, 255)
    pane = (48, 72, 96, 255)
    rect(px, 0, 0, 15, 15, frame)
    rect(px, 2, 2, 13, 13, sky)
    rect(px, 2, 2, 13, 6, sky_h)
    hline(px, 2, 13, 8, frame)
    vline(px, 8, 2, 13, frame)
    put(px, 4, 4, pane)
    put(px, 11, 10, pane)
    return px


def poster():
    px = new(12, 14)
    frame = (48, 40, 36, 255)
    paper = (236, 220, 180, 255)
    red = (196, 72, 72, 255)
    blue = (72, 110, 186, 255)
    rect(px, 0, 0, 11, 13, frame)
    rect(px, 1, 1, 10, 12, paper)
    rect(px, 2, 3, 5, 8, red)
    rect(px, 6, 6, 9, 10, blue)
    return px


def rug():
    px = new(16, 16)
    a = (92, 58, 48, 255)
    b = (110, 72, 58, 255)
    edge = (64, 40, 34, 255)
    for y in range(16):
        for x in range(16):
            c = a if ((x // 2) + (y // 2)) % 2 == 0 else b
            if x in (0, 15) or y in (0, 15):
                c = edge
            put(px, x, y, c)
    return px


def pad():
    px = new(16, 8)
    ring = (255, 220, 80, 200)
    fill = (255, 220, 80, 50)
    rect(px, 1, 1, 14, 6, fill)
    for x in range(16):
        for y in range(8):
            if x in (0, 15) or y in (0, 7):
                if (x + y) % 2 == 0:
                    put(px, x, y, ring)
    return px


def pip():
    px = new(5, 5)
    rect(px, 1, 1, 3, 3, (255, 255, 255, 255))
    put(px, 2, 0, (255, 255, 255, 255))
    put(px, 2, 4, (255, 255, 255, 255))
    put(px, 0, 2, (255, 255, 255, 255))
    put(px, 4, 2, (255, 255, 255, 255))
    return px


def folder_meta(path, guid):
    os.makedirs(path, exist_ok=True)
    text = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def main():
    res = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources"))
    os.makedirs(ROOT, exist_ok=True)
    folder_meta(res, "a1b2c3d4e5f60718293a4b5c6d7e8f90")
    folder_meta(ROOT, "b2c3d4e5f60718293a4b5c6d7e8f901a")
    save("floor", floor_tile(), 0.5, 0.5)
    save("wall", wall_tile(), 0.5, 0.5)
    save("desk", desk(), 0.5, 0.0)
    save("empty", empty_slot(), 0.5, 0.0)
    save("door", door(), 0.5, 0.0)
    save("coffee", coffee(), 0.5, 0.0)
    save("toilet", toilet(), 0.5, 0.0)
    save("sofa", sofa(), 0.5, 0.0)
    save("plant", plant(), 0.5, 0.0)
    save("window", window(), 0.5, 0.5)
    save("poster", poster(), 0.5, 0.5)
    save("rug", rug(), 0.5, 0.5)
    save("pad", pad(), 0.5, 0.5)
    save("pip", pip(), 0.5, 0.5)


if __name__ == "__main__":
    main()
