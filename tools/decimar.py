#!/usr/bin/env python3
"""Reduz um STL (miniatura impressa em 3D, em mm) para um OBJ leve para a Unity.

Python puro, sem bibliotecas externas:
  - lê STL binário ou ASCII;
  - junta vértices repetidos e reduz a malha por *vertex clustering* (grade 3D),
    ajustando o tamanho da grade até chegar perto da meta de triângulos;
  - gira de Z para cima (impressão 3D) para Y para cima (Unity): (x, y, z) -> (x, z, -y);
  - centraliza em XZ e põe a base em y = 0;
  - recalcula normais (suaves para personagens, por face para cenário);
  - grava o OBJ e devolve as dimensões em mm.

Uso: decimar.py entrada.stl saida.obj --tris 15000 [--modo figura|cenario] [--altura 1.0 | --escala 0.045]
"""
import argparse
import json
import math
import struct
import sys


def ler_stl(caminho):
    with open(caminho, "rb") as f:
        dados = f.read()
    n = struct.unpack_from("<I", dados, 80)[0] if len(dados) >= 84 else 0
    if len(dados) == 84 + n * 50 and n > 0:
        tris = []
        for i in range(n):
            v = struct.unpack_from("<9f", dados, 84 + i * 50 + 12)
            tris.append(v)
        return tris
    # ASCII
    tris, atual = [], []
    for linha in dados.decode("latin-1").splitlines():
        p = linha.split()
        if len(p) == 4 and p[0] == "vertex":
            atual.extend(float(x) for x in p[1:])
            if len(atual) == 9:
                tris.append(tuple(atual))
                atual = []
    return tris


def indexar(tris):
    """Vértices únicos (já girados para Y-up) e lista de índices."""
    mapa, verts, faces = {}, [], []
    for t in tris:
        f = []
        for k in range(3):
            x, y, z = t[k * 3], t[k * 3 + 1], t[k * 3 + 2]
            chave = (round(x, 4), round(z, 4), round(-y, 4))
            i = mapa.get(chave)
            if i is None:
                i = len(verts)
                mapa[chave] = i
                verts.append(chave)
            f.append(i)
        if f[0] != f[1] and f[1] != f[2] and f[0] != f[2]:
            faces.append(f)
    return verts, faces


def agrupar(verts, faces, celula):
    """Vertex clustering: um vértice (média) por célula da grade."""
    cel_de, soma = [], {}
    for x, y, z in verts:
        c = (int(math.floor(x / celula)), int(math.floor(y / celula)), int(math.floor(z / celula)))
        cel_de.append(c)
        s = soma.get(c)
        if s is None:
            soma[c] = [x, y, z, 1]
        else:
            s[0] += x; s[1] += y; s[2] += z; s[3] += 1
    idx, novos = {}, []
    for c, s in soma.items():
        idx[c] = len(novos)
        novos.append((s[0] / s[3], s[1] / s[3], s[2] / s[3]))
    vistos, nf = set(), []
    for a, b, c in faces:
        ia, ib, ic = idx[cel_de[a]], idx[cel_de[b]], idx[cel_de[c]]
        if ia == ib or ib == ic or ia == ic:
            continue
        chave = tuple(sorted((ia, ib, ic)))
        if chave in vistos:
            continue
        vistos.add(chave)
        nf.append((ia, ib, ic))
    # remove vértices sem uso
    usados = sorted({i for f in nf for i in f})
    remap = {o: n for n, o in enumerate(usados)}
    return [novos[i] for i in usados], [(remap[a], remap[b], remap[c]) for a, b, c in nf]


def limites(verts):
    mn = [min(v[i] for v in verts) for i in range(3)]
    mx = [max(v[i] for v in verts) for i in range(3)]
    return mn, mx


def normal(a, b, c):
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    return (uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx)


def unit(n):
    l = math.sqrt(n[0] ** 2 + n[1] ** 2 + n[2] ** 2) or 1.0
    return (n[0] / l, n[1] / l, n[2] / l)


def gravar_obj(caminho, verts, faces, suave):
    linhas = ["# gerado por tools/decimar.py"]
    if suave:
        acc = [[0.0, 0.0, 0.0] for _ in verts]
        for f in faces:
            n = normal(verts[f[0]], verts[f[1]], verts[f[2]])
            for i in f:
                acc[i][0] += n[0]; acc[i][1] += n[1]; acc[i][2] += n[2]
        for v in verts:
            linhas.append(f"v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}")
        for n in acc:
            n = unit(n)
            linhas.append(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}")
        for a, b, c in faces:
            linhas.append(f"f {a+1}//{a+1} {b+1}//{b+1} {c+1}//{c+1}")
    else:
        for v in verts:
            linhas.append(f"v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}")
        eixos = []
        for k, (a, b, c) in enumerate(faces):
            n = unit(normal(verts[a], verts[b], verts[c]))
            linhas.append(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}")
            eixos.append(max(range(3), key=lambda i: abs(n[i])))
        # UV por projeção em caixa: cada face usa o plano do eixo dominante da normal
        for k, f in enumerate(faces):
            u, v = [(1, 2), (0, 2), (0, 1)][eixos[k]]
            for i in f:
                linhas.append(f"vt {verts[i][u] * 0.6:.4f} {verts[i][v] * 0.6:.4f}")
        for k, (a, b, c) in enumerate(faces):
            t = k * 3
            linhas.append(f"f {a+1}/{t+1}/{k+1} {b+1}/{t+2}/{k+1} {c+1}/{t+3}/{k+1}")
    with open(caminho, "w") as f:
        f.write("\n".join(linhas) + "\n")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("entrada", nargs="+")
    ap.add_argument("saida")
    ap.add_argument("--tris", type=int, default=15000)
    ap.add_argument("--modo", choices=["figura", "cenario"], default="figura")
    ap.add_argument("--altura", type=float, default=0, help="normaliza a altura final (figuras)")
    ap.add_argument("--escala", type=float, default=0.045, help="unidades por mm (cenário)")
    ap.add_argument("--deitado", choices=["", "x", "-x", "z", "-z"], default="",
                    help="gira 90 graus quando a peça foi exportada deitada")
    a = ap.parse_args()

    tris = []
    for e in a.entrada:
        tris.extend(ler_stl(e))
    verts, faces = indexar(tris)
    del tris
    if a.deitado:
        def gira(v):
            x, y, z = v
            return {"x": (x, -z, y), "-x": (x, z, -y), "z": (y, -x, z), "-z": (-y, x, z)}[a.deitado]
        verts = [gira(v) for v in verts]
    mn, mx = limites(verts)
    tam_mm = [mx[i] - mn[i] for i in range(3)]

    # busca do tamanho da célula que chega perto da meta de triângulos
    maior = max(tam_mm)
    lo, hi = maior / 2000.0, maior / 8.0
    melhor = None
    if len(faces) <= a.tris:
        melhor = (verts, faces)
    else:
        for _ in range(9):
            cel = math.sqrt(lo * hi)
            v2, f2 = agrupar(verts, faces, cel)
            if melhor is None or abs(len(f2) - a.tris) < abs(len(melhor[1]) - a.tris):
                melhor = (v2, f2)
            if len(f2) > a.tris * 1.12:
                lo = cel
            elif len(f2) < a.tris * 0.88:
                hi = cel
            else:
                break
    verts, faces = melhor

    mn, mx = limites(verts)
    cx, cz = (mn[0] + mx[0]) / 2, (mn[2] + mx[2]) / 2
    if a.altura > 0:
        s = a.altura / (mx[1] - mn[1])
    else:
        s = a.escala
    verts = [((x - cx) * s, (y - mn[1]) * s, (z - cz) * s) for x, y, z in verts]
    gravar_obj(a.saida, verts, faces, a.modo == "figura")
    mn2, mx2 = limites(verts)
    print(json.dumps({
        "saida": a.saida, "tris": len(faces), "verts": len(verts),
        "mm": [round(t, 1) for t in tam_mm],
        "tam": [round(mx2[i] - mn2[i], 3) for i in range(3)],
    }))


if __name__ == "__main__":
    sys.exit(main())
