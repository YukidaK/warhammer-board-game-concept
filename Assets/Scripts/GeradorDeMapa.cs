using System.Collections.Generic;
using UnityEngine;

// Gera o campo de batalha: chão, ruínas (cobertura total), caixas e barricadas (meia cobertura)
// e as casas iniciais das duas equipes. Repete até o mapa ficar todo conectado.
public static class GeradorDeMapa
{
    public const int Largura = 16, Altura = 22;

    public static Grade Gerar(int seed, Jogo jogo, Transform pai, out List<Vector2Int> inicioAzul, out List<Vector2Int> inicioVermelho)
    {
        var rnd = new System.Random(seed);
        Grade grade;
        int tentativa = 0;
        do
        {
            grade = Distribuir(rnd, out inicioAzul, out inicioVermelho);
            tentativa++;
        } while (!Conectado(grade, inicioAzul, inicioVermelho) && tentativa < 50);

        Construir(grade, jogo, pai, rnd);
        return grade;
    }

    static Grade Distribuir(System.Random rnd, out List<Vector2Int> azul, out List<Vector2Int> vermelho)
    {
        var g = new Grade(Largura, Altura);
        var reservado = new HashSet<Vector2Int>();

        azul = new List<Vector2Int>();
        int ax = Largura / 2 - 2 + rnd.Next(-2, 3);
        foreach (var c in new[] { new Vector2Int(ax, 1), new Vector2Int(ax + 2, 1), new Vector2Int(ax + 1, 0), new Vector2Int(ax + 3, 0) })
            azul.Add(c);

        // inimigos em 3 grupos de 2, espalhados pela metade de cima
        vermelho = new List<Vector2Int>();
        int[] colunas = { rnd.Next(1, 5), rnd.Next(6, 10), rnd.Next(11, 15) };
        foreach (int cx in colunas)
        {
            int cy = rnd.Next(14, Altura - 2);
            vermelho.Add(new Vector2Int(cx, cy));
            vermelho.Add(new Vector2Int(Mathf.Clamp(cx + (rnd.Next(2) == 0 ? -1 : 1), 0, Largura - 1), cy + 1));
        }

        foreach (var c in azul) Reservar(reservado, c, 1);
        foreach (var c in vermelho) Reservar(reservado, c, 0);

        // ruínas: muros em L ou retos
        int ruinas = rnd.Next(7, 10);
        for (int i = 0; i < ruinas; i++)
        {
            var o = new Vector2Int(rnd.Next(0, Largura), rnd.Next(3, Altura - 1));
            var d = rnd.Next(2) == 0 ? Vector2Int.right : Vector2Int.up;
            int n = rnd.Next(2, 5);
            for (int k = 0; k < n; k++) Colocar(g, reservado, o + d * k, Grade.CoberturaTotal);
            if (rnd.Next(2) == 0)
            {
                var d2 = d == Vector2Int.right ? Vector2Int.up : Vector2Int.right;
                int n2 = rnd.Next(1, 3);
                for (int k = 1; k <= n2; k++) Colocar(g, reservado, o + d2 * k, Grade.CoberturaTotal);
            }
        }

        // caixas e barricadas em grupinhos
        int grupos = rnd.Next(15, 21);
        for (int i = 0; i < grupos; i++)
        {
            var o = new Vector2Int(rnd.Next(0, Largura), rnd.Next(2, Altura));
            int n = rnd.Next(1, 4);
            var d = rnd.Next(2) == 0 ? Vector2Int.right : Vector2Int.up;
            for (int k = 0; k < n; k++) Colocar(g, reservado, o + d * k, Grade.MeiaCobertura);
        }

        // cobertura logo à frente das casas iniciais, para os dois lados terem onde se proteger
        foreach (var c in vermelho) Colocar(g, reservado, c + Vector2Int.down * 2, Grade.MeiaCobertura, true);
        Colocar(g, reservado, new Vector2Int(ax, 3), Grade.MeiaCobertura, true);
        Colocar(g, reservado, new Vector2Int(ax + 3, 3), Grade.MeiaCobertura, true);
        return g;
    }

    static void Reservar(HashSet<Vector2Int> r, Vector2Int c, int raio)
    {
        for (int x = -raio; x <= raio; x++)
            for (int y = -raio; y <= raio; y++)
                r.Add(c + new Vector2Int(x, y));
    }

    static void Colocar(Grade g, HashSet<Vector2Int> reservado, Vector2Int c, int t, bool forcar = false)
    {
        if (!g.Dentro(c) || reservado.Contains(c)) return;
        if (!forcar && g.tipo[c.x, c.y] != Grade.Livre) return;
        g.tipo[c.x, c.y] = t;
    }

    static bool Conectado(Grade g, List<Vector2Int> azul, List<Vector2Int> vermelho)
    {
        var alcance = g.Alcance(azul[0], 999f, out _);
        foreach (var c in azul) if (!alcance.ContainsKey(c)) return false;
        foreach (var c in vermelho) if (!alcance.ContainsKey(c)) return false;
        int livres = 0;
        for (int x = 0; x < g.largura; x++)
            for (int y = 0; y < g.altura; y++)
                if (g.tipo[x, y] == Grade.Livre) livres++;
        return alcance.Count >= livres * 0.95f;
    }

    static void Construir(Grade g, Jogo jogo, Transform pai, System.Random rnd)
    {
        float t = Grade.Tamanho;
        // chão externo, escuro, para o tabuleiro não flutuar no vazio
        var fora = Cubo(pai, "Terreno", new Vector3((g.largura - 1) * t / 2, -0.3f, (g.altura - 1) * t / 2),
            new Vector3(g.largura * t + 30, 0.5f, g.altura * t + 30), jogo.matTerreno);
        fora.GetComponent<Renderer>().receiveShadows = true;

        for (int x = 0; x < g.largura; x++)
            for (int y = 0; y < g.altura; y++)
            {
                var c = new Vector2Int(x, y);
                var p = Grade.Mundo(c);
                Cubo(pai, "Casa", p + Vector3.down * 0.05f, new Vector3(t * 0.97f, 0.1f, t * 0.97f), (x + y) % 2 == 0 ? jogo.matChaoA : jogo.matChaoB);

                if (g.tipo[x, y] == Grade.CoberturaTotal)
                {
                    float h = 1.5f + (float)rnd.NextDouble() * 0.5f;
                    Cubo(pai, "Muro", p + Vector3.up * h / 2, new Vector3(t, h, t), jogo.matMuro);
                    // detalhe no topo para quebrar a forma
                    if (rnd.Next(3) == 0)
                        Cubo(pai, "Entulho", p + new Vector3(0.3f, h + 0.15f, -0.2f), new Vector3(0.6f, 0.3f, 0.5f), jogo.matMuro);
                }
                else if (g.tipo[x, y] == Grade.MeiaCobertura)
                {
                    if (rnd.Next(2) == 0)
                    {
                        // caixa
                        var cx = Cubo(pai, "Caixa", p + Vector3.up * 0.42f, new Vector3(1.15f, 0.84f, 1.15f), jogo.matCaixa);
                        cx.transform.rotation = Quaternion.Euler(0, rnd.Next(-12, 12), 0);
                        if (rnd.Next(2) == 0)
                            Cubo(pai, "Caixa", p + new Vector3(0.1f, 1.05f, 0.05f), new Vector3(0.6f, 0.42f, 0.6f), jogo.matCaixa)
                                .transform.rotation = Quaternion.Euler(0, rnd.Next(0, 90), 0);
                    }
                    else
                    {
                        // barricada de sacos de areia
                        for (int k = 0; k < 3; k++)
                            Cubo(pai, "Saco", p + new Vector3(0, 0.17f + k * 0.28f, 0), new Vector3(1.4f - k * 0.12f, 0.3f, 1.0f - k * 0.12f), jogo.matSaco);
                    }
                }
            }

        // pedras decorativas em volta do tabuleiro
        for (int i = 0; i < 40; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2;
            var centro = new Vector3((g.largura - 1) * t / 2, 0, (g.altura - 1) * t / 2);
            float rx = g.largura * t / 2 + 3 + (float)rnd.NextDouble() * 10;
            float rz = g.altura * t / 2 + 3 + (float)rnd.NextDouble() * 10;
            var pos = centro + new Vector3(Mathf.Cos(ang) * rx, 0, Mathf.Sin(ang) * rz);
            float s = 0.4f + (float)rnd.NextDouble() * 1.1f;
            var pedra = Cubo(pai, "Pedra", pos + Vector3.up * s * 0.3f, new Vector3(s, s * 0.8f, s * 1.2f), jogo.matMuro);
            pedra.transform.rotation = Quaternion.Euler(rnd.Next(-20, 20), rnd.Next(0, 360), rnd.Next(-20, 20));
        }
    }

    public static GameObject Cubo(Transform pai, string nome, Vector3 pos, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(pai, false);
        go.transform.position = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }
}
