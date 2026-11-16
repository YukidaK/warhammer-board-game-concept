#!/usr/bin/env python3
"""Render rápido de OBJs (frente, lado e 3/4) com PIL, para identificar as peças.

Uso: render.py saida.png arquivo1.obj [arquivo2.obj ...]  -> prancha com uma linha por OBJ
"""
import math
import sys

from PIL import Image, ImageDraw, ImageFont


def ler_obj(caminho):
    v, f = [], []
    for l in open(caminho):
        if l.startswith("v "):
            v.append(tuple(map(float, l.split()[1:4])))
        elif l.startswith("f "):
            f.append(tuple(int(p.split("/")[0]) - 1 for p in l.split()[1:4]))
    return v, f


def vista(v, f, yaw, pitch, tam=220):
    cy, sy = math.cos(yaw), math.sin(yaw)
    cp, sp = math.cos(pitch), math.sin(pitch)
    pts = []
    for x, y, z in v:
        x1, z1 = x * cy - z * sy, x * sy + z * cy
        y2, z2 = y * cp - z1 * sp, y * sp + z1 * cp
        pts.append((x1, y2, z2))
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    esc = (tam - 20) / max(max(xs) - min(xs), max(ys) - min(ys), 1e-6)
    ox, oy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    img = Image.new("RGB", (tam, tam), (40, 44, 52))
    d = ImageDraw.Draw(img)
    luz = (0.4, 0.7, -0.6)
    tri = []
    for a, b, c in f:
        A, B, C = pts[a], pts[b], pts[c]
        ux, uy, uz = B[0] - A[0], B[1] - A[1], B[2] - A[2]
        wx, wy, wz = C[0] - A[0], C[1] - A[1], C[2] - A[2]
        n = (uy * wz - uz * wy, uz * wx - ux * wz, ux * wy - uy * wx)
        l = math.sqrt(n[0] ** 2 + n[1] ** 2 + n[2] ** 2) or 1
        k = abs(n[0] * luz[0] + n[1] * luz[1] + n[2] * luz[2]) / l
        tri.append(((A[2] + B[2] + C[2]) / 3, k, (A, B, C)))
    tri.sort(key=lambda t: -t[0])
    for _, k, (A, B, C) in tri:
        g = int(70 + 170 * k)
        d.polygon([(tam / 2 + (p[0] - ox) * esc, tam / 2 - (p[1] - oy) * esc) for p in (A, B, C)], fill=(g, g, int(g * 0.95)))
    return img


def main():
    saida, objs = sys.argv[1], sys.argv[2:]
    tam = 220
    prancha = Image.new("RGB", (tam * 3 + 260, tam * len(objs)), (25, 27, 32))
    d = ImageDraw.Draw(prancha)
    for i, o in enumerate(objs):
        v, f = ler_obj(o)
        for j, (yaw, pitch) in enumerate([(0, 0), (math.pi / 2, 0), (math.pi / 4, -0.5)]):
            prancha.paste(vista(v, f, yaw, pitch, tam), (j * tam, i * tam))
        xs = [p[0] for p in v]; ys = [p[1] for p in v]; zs = [p[2] for p in v]
        d.text((tam * 3 + 10, i * tam + 10), o.split("/")[-1], fill=(255, 220, 90))
        d.text((tam * 3 + 10, i * tam + 30), f"{len(f)} tris", fill=(200, 200, 200))
        d.text((tam * 3 + 10, i * tam + 50), f"x {max(xs)-min(xs):.2f} y {max(ys)-min(ys):.2f} z {max(zs)-min(zs):.2f}", fill=(200, 200, 200))
    prancha.save(saida)


if __name__ == "__main__":
    main()
