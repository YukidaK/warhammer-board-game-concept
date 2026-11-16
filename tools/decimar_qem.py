#!/usr/bin/env python3
"""Converte STL (miniatura em mm) em OBJ leve para a Unity, preservando a forma.

Diferente do decimar.py (vertex clustering, que "derrete" detalhes), este usa
simplificação por erro quádrico (pyfqmr, o mesmo método do Decimate do Blender)
e normais com ângulo de quina: superfícies curvas ficam lisas e quinas continuam vivas.

Requer: pip install numpy pyfqmr
Uso: decimar_qem.py entrada.stl|.obj saida.obj --tris 40000 [--modo figura|cenario] [--escala 0.045] [--deitado -x]
"""
import argparse
import json

import numpy as np
import pyfqmr


def ler_obj(caminho):
    vs, fs = [], []
    with open(caminho, "r", errors="ignore") as arq:
        for l in arq:
            if l.startswith("v "):
                p = l.split()
                vs.append((float(p[1]), float(p[2]), float(p[3])))
            elif l.startswith("f "):
                idx = [int(x.split("/")[0]) for x in l.split()[1:]]
                idx = [i - 1 if i > 0 else len(vs) + i for i in idx]
                for k in range(1, len(idx) - 1):          # triangula polígonos em leque
                    fs.append((idx[0], idx[k], idx[k + 1]))
    return np.array(vs, dtype=np.float64), np.array(fs, dtype=np.int64)


def ler_stl(caminho):
    if caminho.lower().endswith(".obj"):
        v, f = ler_obj(caminho)
        ok = (f[:, 0] != f[:, 1]) & (f[:, 1] != f[:, 2]) & (f[:, 0] != f[:, 2])
        return v, f[ok]
    dados = open(caminho, "rb").read()
    n = int(np.frombuffer(dados, dtype="<u4", count=1, offset=80)[0]) if len(dados) >= 84 else 0
    if n > 0 and len(dados) == 84 + n * 50:
        reg = np.dtype([("n", "<f4", 3), ("v", "<f4", 9), ("a", "<u2")])
        tri = np.frombuffer(dados, dtype=reg, count=n, offset=84)["v"].reshape(-1, 3).astype(np.float64)
    else:
        vs = [list(map(float, l.split()[1:4])) for l in dados.decode("latin-1").splitlines() if l.strip().startswith("vertex")]
        tri = np.array(vs, dtype=np.float64)
    # junta vértices repetidos
    chave = np.round(tri, 4)
    verts, inv = np.unique(chave, axis=0, return_inverse=True)
    faces = inv.reshape(-1, 3)
    ok = (faces[:, 0] != faces[:, 1]) & (faces[:, 1] != faces[:, 2]) & (faces[:, 0] != faces[:, 2])
    return verts, faces[ok]


def normais_com_quina(v, f, quina_graus):
    a, b, c = v[f[:, 0]], v[f[:, 1]], v[f[:, 2]]
    cruz = np.cross(b - a, c - a)
    area = np.linalg.norm(cruz, axis=1, keepdims=True)
    fn = cruz / np.maximum(area, 1e-12)
    limite = np.cos(np.radians(quina_graus))
    # faces vizinhas de cada vértice
    viz = [[] for _ in range(len(v))]
    for i, (p, q, r) in enumerate(f):
        viz[p].append(i); viz[q].append(i); viz[r].append(i)
    viz = [np.array(x) for x in viz]
    cantos = np.zeros((len(f), 3, 3))
    for i in range(len(f)):
        for k in range(3):
            g = viz[f[i, k]]
            m = (fn[g] @ fn[i]) > limite
            s = (cruz[g[m]]).sum(axis=0)          # soma ponderada pela área
            n = np.linalg.norm(s)
            cantos[i, k] = s / n if n > 1e-12 else fn[i]
    return cantos


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("entrada")
    ap.add_argument("saida")
    ap.add_argument("--tris", type=int, default=40000)
    ap.add_argument("--modo", choices=["figura", "cenario"], default="figura")
    ap.add_argument("--escala", type=float, default=0.045)
    ap.add_argument("--altura", type=float, default=0, help="normaliza a altura final (ignora --escala)")
    ap.add_argument("--quina", type=float, default=0, help="ângulo de quina (padrão: 60 figura, 35 cenário)")
    ap.add_argument("--deitado", choices=["", "x", "-x", "z", "-z"], default="")
    a = ap.parse_args()

    v, f = ler_stl(a.entrada)
    tam_mm = (v.max(0) - v.min(0)).tolist()
    tris_orig = len(f)
    if len(f) > a.tris:
        s = pyfqmr.Simplify()
        s.setMesh(v, f)
        s.simplify_mesh(target_count=a.tris, aggressiveness=5, preserve_border=True, verbose=False)
        v, f, _ = s.getMesh()
        v = v.astype(np.float64)

    # Z para cima (impressão 3D) -> Y para cima (Unity)
    v = np.stack([v[:, 0], v[:, 2], -v[:, 1]], axis=1)
    if a.deitado:
        x, y, z = v[:, 0], v[:, 1], v[:, 2]
        v = {"x": np.stack([x, -z, y], 1), "-x": np.stack([x, z, -y], 1),
             "z": np.stack([y, -x, z], 1), "-z": np.stack([-y, x, z], 1)}[a.deitado]
    mn, mx = v.min(0), v.max(0)
    esc = a.altura / (mx[1] - mn[1]) if a.altura > 0 else a.escala
    v = (v - np.array([(mn[0] + mx[0]) / 2, mn[1], (mn[2] + mx[2]) / 2])) * esc

    quina = a.quina or (60 if a.modo == "figura" else 35)
    cn = normais_com_quina(v, f, quina)

    # vértice de saída = (posição, normal) único
    linhas = ["# gerado por tools/decimar_qem.py"]
    for p in v:
        linhas.append(f"v {p[0]:.5f} {p[1]:.5f} {p[2]:.5f}")
    nq = np.round(cn.reshape(-1, 3), 3)
    nu, ninv = np.unique(nq, axis=0, return_inverse=True)
    ninv = ninv.reshape(-1, 3)
    for n in nu:
        linhas.append(f"vn {n[0]:.3f} {n[1]:.3f} {n[2]:.3f}")
    if a.modo == "cenario":
        # UV por projeção em caixa (eixo dominante da normal da face)
        a_, b_, c_ = v[f[:, 0]], v[f[:, 1]], v[f[:, 2]]
        fnn = np.abs(np.cross(b_ - a_, c_ - a_))
        eixo = fnn.argmax(1)
        planos = np.array([[1, 2], [0, 2], [0, 1]])[eixo]
        uv = np.zeros((len(f), 3, 2))
        for k in range(3):
            pk = v[f[:, k]]
            uv[:, k, 0] = pk[np.arange(len(f)), planos[:, 0]] * 0.6
            uv[:, k, 1] = pk[np.arange(len(f)), planos[:, 1]] * 0.6
        uq = np.round(uv.reshape(-1, 2), 3)
        uu, uinv = np.unique(uq, axis=0, return_inverse=True)
        uinv = uinv.reshape(-1, 3)
        for t in uu:
            linhas.append(f"vt {t[0]:.3f} {t[1]:.3f}")
        for i, (p, q, r) in enumerate(f):
            linhas.append(f"f {p+1}/{uinv[i,0]+1}/{ninv[i,0]+1} {q+1}/{uinv[i,1]+1}/{ninv[i,1]+1} {r+1}/{uinv[i,2]+1}/{ninv[i,2]+1}")
    else:
        for i, (p, q, r) in enumerate(f):
            linhas.append(f"f {p+1}//{ninv[i,0]+1} {q+1}//{ninv[i,1]+1} {r+1}//{ninv[i,2]+1}")
    open(a.saida, "w").write("\n".join(linhas) + "\n")
    t = v.max(0) - v.min(0)
    print(json.dumps({"saida": a.saida, "tris_orig": tris_orig, "tris": int(len(f)), "mm": [round(x, 1) for x in tam_mm], "tam": [round(x, 3) for x in t.tolist()]}))


if __name__ == "__main__":
    main()
