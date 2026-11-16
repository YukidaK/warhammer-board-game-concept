using System.Collections.Generic;
using UnityEngine;

// Gera o campo de batalha: chão, ruínas (cobertura total), caixas e barricadas (meia cobertura),
// marcos grandes do cenário (capela, chaminé, prédio em ruínas) e as casas iniciais das duas equipes.
// Repete até o mapa ficar todo conectado. O visual vem do Cenario escolhido no menu.
public static class GeradorDeMapa
{
    public const int Largura = 16, Altura = 22;

    // marco colocado no mapa: qual peça, canto inferior e tamanho em casas (já girado)
    struct MarcoPosto { public Marco marco; public Vector2Int origem, tamanho; public bool girado; }

    public static Grade Gerar(int seed, Jogo jogo, Cenario cenario, Transform pai, out List<Vector2Int> inicioAzul, out List<Vector2Int> inicioVermelho)
    {
        var rnd = new System.Random(seed);
        Grade grade;
        List<MarcoPosto> marcos;
        int tentativa = 0;
        do
        {
            grade = Distribuir(rnd, cenario, out inicioAzul, out inicioVermelho, out marcos);
            tentativa++;
        } while (!Conectado(grade, inicioAzul, inicioVermelho) && tentativa < 50);

        Construir(grade, jogo, cenario, marcos, pai, rnd);
        return grade;
    }

    static Grade Distribuir(System.Random rnd, Cenario cenario, out List<Vector2Int> azul, out List<Vector2Int> vermelho, out List<MarcoPosto> marcos)
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

        // marcos grandes do tema (ocupam um retângulo de casas, no miolo do mapa)
        marcos = new List<MarcoPosto>();
        if (cenario != null && cenario.marcos != null && cenario.marcos.Length > 0)
        {
            int qtd = rnd.Next(2, 4);
            for (int i = 0, tent = 0; i < qtd && tent < 60; tent++)
            {
                var m = cenario.marcos[rnd.Next(cenario.marcos.Length)];
                bool girar = m.tamanho.x != m.tamanho.y && rnd.Next(2) == 0;
                var t = girar ? new Vector2Int(m.tamanho.y, m.tamanho.x) : m.tamanho;
                var o = new Vector2Int(rnd.Next(0, Largura - t.x + 1), rnd.Next(3, Altura - 3 - t.y + 1));
                bool livre = true;
                for (int x = -1; x <= t.x && livre; x++)
                    for (int y = -1; y <= t.y && livre; y++)
                    {
                        var c = o + new Vector2Int(x, y);
                        bool dentroDoMarco = x >= 0 && y >= 0 && x < t.x && y < t.y;
                        if (dentroDoMarco && reservado.Contains(c)) livre = false;
                        // mantém uma casa de folga em volta, para não fechar corredores
                        if (g.Dentro(c) && g.tipo[c.x, c.y] != Grade.Livre) livre = false;
                    }
                if (!livre) continue;
                for (int x = 0; x < t.x; x++)
                    for (int y = 0; y < t.y; y++)
                        g.tipo[o.x + x, o.y + y] = Grade.CoberturaTotal;
                marcos.Add(new MarcoPosto { marco = m, origem = o, tamanho = t, girado = girar });
                i++;
            }
        }

        // ruínas: muros em L ou retos
        int ruinas = marcos.Count > 0 ? rnd.Next(4, 7) : rnd.Next(7, 10);
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

    static void Construir(Grade g, Jogo jogo, Cenario cen, List<MarcoPosto> marcos, Transform pai, System.Random rnd)
    {
        float t = Grade.Tamanho;
        bool temPecas = cen != null && cen.muros != null && cen.muros.Length > 0;
        var matChaoA = cen != null && cen.matChaoA != null ? cen.matChaoA : jogo.matChaoA;
        var matChaoB = cen != null && cen.matChaoB != null ? cen.matChaoB : jogo.matChaoB;
        var matTerreno = cen != null && cen.matTerreno != null ? cen.matTerreno : jogo.matTerreno;

        // chão externo, escuro, para o tabuleiro não flutuar no vazio
        var fora = Bloco(jogo.prefTerreno, pai, "Terreno", new Vector3((g.largura - 1) * t / 2, -0.3f, (g.altura - 1) * t / 2),
            new Vector3(g.largura * t + 60, 0.5f, g.altura * t + 60), matTerreno);
        fora.transform.position += Vector3.down * 0.02f;
        fora.GetComponent<Renderer>().receiveShadows = true;

        var noMarco = new HashSet<Vector2Int>();
        foreach (var m in marcos)
        {
            for (int x = 0; x < m.tamanho.x; x++)
                for (int y = 0; y < m.tamanho.y; y++)
                    noMarco.Add(m.origem + new Vector2Int(x, y));
            var centro = (Grade.Mundo(m.origem) + Grade.Mundo(m.origem + m.tamanho - Vector2Int.one)) / 2;
            var go = Peca(m.marco.prefab, pai, "Marco", centro, cen.matMuro);
            Encaixar(go, new Vector3(m.tamanho.x * t - 0.25f, 0, m.tamanho.y * t - 0.25f), m.girado, rnd.Next(2) == 0);
        }

        for (int x = 0; x < g.largura; x++)
            for (int y = 0; y < g.altura; y++)
            {
                var c = new Vector2Int(x, y);
                var p = Grade.Mundo(c);
                Bloco(jogo.prefCasa, pai, "Casa", p + Vector3.down * 0.05f, new Vector3(t * 0.97f, 0.1f, t * 0.97f), (x + y) % 2 == 0 ? matChaoA : matChaoB);
                if (noMarco.Contains(c)) continue;

                if (g.tipo[x, y] == Grade.CoberturaTotal)
                {
                    if (temPecas)
                    {
                        // muro alinhado com os vizinhos: sequência na horizontal fica de frente para Z
                        bool horiz = Muro(g, c + Vector2Int.left) || Muro(g, c + Vector2Int.right);
                        bool vert = Muro(g, c + Vector2Int.up) || Muro(g, c + Vector2Int.down);
                        bool girar = vert && !horiz || !vert && !horiz && rnd.Next(2) == 0;
                        var go = Peca(cen.muros[rnd.Next(cen.muros.Length)], pai, "Muro", p, cen.matMuro);
                        Esticar(go, t * 0.98f, 1.75f + (float)rnd.NextDouble() * 0.35f, 0.75f, girar, rnd.Next(2) == 0);
                        continue;
                    }
                    float h = 1.5f + (float)rnd.NextDouble() * 0.5f;
                    Bloco(jogo.prefMuro, pai, "Muro", p + Vector3.up * h / 2, new Vector3(t, h, t), jogo.matMuro);
                    // detalhe no topo para quebrar a forma
                    if (rnd.Next(3) == 0)
                        Bloco(jogo.prefMuro, pai, "Entulho", p + new Vector3(0.3f, h + 0.15f, -0.2f), new Vector3(0.6f, 0.3f, 0.5f), jogo.matMuro);
                }
                else if (g.tipo[x, y] == Grade.MeiaCobertura)
                {
                    if (temPecas && cen.meiaCobertura.Length > 0)
                    {
                        var go = Peca(cen.meiaCobertura[rnd.Next(cen.meiaCobertura.Length)], pai, "Barricada", p, cen.matMeia);
                        Esticar(go, t * 0.92f, 0.85f + (float)rnd.NextDouble() * 0.12f, 1.2f, rnd.Next(2) == 0, rnd.Next(2) == 0);
                        go.transform.rotation *= Quaternion.Euler(0, rnd.Next(-10, 10), 0);
                        continue;
                    }
                    if (rnd.Next(2) == 0)
                    {
                        // caixa
                        var cx = Bloco(jogo.prefCaixa, pai, "Caixa", p + Vector3.up * 0.42f, new Vector3(1.15f, 0.84f, 1.15f), jogo.matCaixa);
                        cx.transform.rotation = Quaternion.Euler(0, rnd.Next(-12, 12), 0);
                        if (rnd.Next(2) == 0)
                            Bloco(jogo.prefCaixa, pai, "Caixa", p + new Vector3(0.1f, 1.05f, 0.05f), new Vector3(0.6f, 0.42f, 0.6f), jogo.matCaixa)
                                .transform.rotation = Quaternion.Euler(0, rnd.Next(0, 90), 0);
                    }
                    else
                    {
                        // barricada de sacos de areia
                        for (int k = 0; k < 3; k++)
                            Bloco(jogo.prefSaco, pai, "Saco", p + new Vector3(0, 0.17f + k * 0.28f, 0), new Vector3(1.4f - k * 0.12f, 0.3f, 1.0f - k * 0.12f), jogo.matSaco);
                    }
                }
            }

        // decoração em volta do tabuleiro: peças grandes do tema ou pedras
        var meio = new Vector3((g.largura - 1) * t / 2, 0, (g.altura - 1) * t / 2);
        bool temDecor = cen != null && cen.decoracao != null && cen.decoracao.Length > 0;
        int qtdDecor = temDecor ? 26 : 40;
        for (int i = 0; i < qtdDecor; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2;
            float rx = g.largura * t / 2 + 3 + (float)rnd.NextDouble() * (temDecor ? 14 : 10);
            float rz = g.altura * t / 2 + 3 + (float)rnd.NextDouble() * (temDecor ? 14 : 10);
            var pos = meio + new Vector3(Mathf.Cos(ang) * rx, 0, Mathf.Sin(ang) * rz);
            if (temDecor && i % 3 != 2)
            {
                var go = Peca(cen.decoracao[rnd.Next(cen.decoracao.Length)], pai, "Decoracao", pos, cen.matMuro);
                float s = (0.7f + (float)rnd.NextDouble() * 0.5f) * cen.escalaDecoracao;
                go.transform.localScale = Vector3.one * s;
                go.transform.rotation = Quaternion.Euler(0, rnd.Next(0, 360), 0);
                continue;
            }
            float e = 0.4f + (float)rnd.NextDouble() * 1.1f;
            var pedra = Bloco(jogo.prefPedra, pai, "Pedra", pos + Vector3.up * e * 0.3f, new Vector3(e, e * 0.8f, e * 1.2f), jogo.prefPedra != null ? null : jogo.matMuro);
            pedra.transform.rotation = Quaternion.Euler(rnd.Next(-20, 20), rnd.Next(0, 360), rnd.Next(-20, 20));
        }
    }

    static bool Muro(Grade g, Vector2Int c) => g.Dentro(c) && g.tipo[c.x, c.y] == Grade.CoberturaTotal;

    // Instancia uma peça de modelo 3D do cenário (prefab com PecaCenario + BoxCollider) na posição do chão.
    static GameObject Peca(GameObject prefab, Transform pai, string nome, Vector3 pos, Material mat)
    {
        var go = Object.Instantiate(prefab, pai, false);
        go.name = nome;
        go.transform.position = pos;
        if (mat != null)
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = mat;
        return go;
    }

    // Encaixa a peça numa casa: comprimento ao longo de X, altura fixa e profundidade proporcional (limitada).
    static void Esticar(GameObject go, float comprimento, float altura, float profMax, bool girar, bool espelhar)
    {
        var tam = go.GetComponent<PecaCenario>().tamanho;
        float sx = comprimento / tam.x;
        float sz = Mathf.Min(sx, comprimento * profMax / tam.z);
        float sy = altura / tam.y;
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.transform.rotation = Quaternion.Euler(0, (girar ? 90 : 0) + (espelhar ? 180 : 0), 0);
    }

    // Marco: escala uniforme para caber na área (proporções do modelo mantidas).
    static void Encaixar(GameObject go, Vector3 area, bool girar, bool espelhar)
    {
        var tam = go.GetComponent<PecaCenario>().tamanho;
        float lx = girar ? area.z : area.x, lz = girar ? area.x : area.z;
        float s = Mathf.Min(lx / tam.x, lz / tam.z);
        go.transform.localScale = Vector3.one * s;
        go.transform.rotation = Quaternion.Euler(0, (girar ? 90 : 0) + (espelhar ? 180 : 0), 0);
    }

    // Instancia um prefab do cenário (com BoxCollider e material texturizado) e ajusta o Transform.
    // Sem prefab, cai no cubo simples. O material só é trocado quando difere do prefab (ex.: casa clara/escura).
    public static GameObject Bloco(GameObject prefab, Transform pai, string nome, Vector3 pos, Vector3 escala, Material mat)
    {
        if (prefab == null) return Cubo(pai, nome, pos, escala, mat);
        var go = Object.Instantiate(prefab, pai, false);
        go.name = nome;
        go.transform.position = pos;
        go.transform.localScale = escala;
        var r = go.GetComponent<Renderer>();
        if (mat != null && r.sharedMaterial != mat) r.sharedMaterial = mat;
        return go;
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
