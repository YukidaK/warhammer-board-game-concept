using System.Collections.Generic;
using UnityEngine;

// Tabuleiro em casas: tipo de cada casa, quem está nela, caminhos, linha de visão e cobertura.
public class Grade
{
    public const int Livre = 0, MeiaCobertura = 1, CoberturaTotal = 2;
    public const float Tamanho = 1.5f;

    public readonly int largura, altura;
    public readonly int[,] tipo;
    public readonly Unidade[,] ocupante;

    static readonly Vector2Int[] Cardeais = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    static readonly Vector2Int[] Vizinhos =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
        new(1, 1), new(1, -1), new(-1, -1), new(-1, 1)
    };

    public Grade(int largura, int altura)
    {
        this.largura = largura;
        this.altura = altura;
        tipo = new int[largura, altura];
        ocupante = new Unidade[largura, altura];
    }

    public bool Dentro(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < largura && c.y < altura;
    public bool Passavel(Vector2Int c) => Dentro(c) && tipo[c.x, c.y] == Livre;
    public bool Vazia(Vector2Int c) => Passavel(c) && ocupante[c.x, c.y] == null;

    public static Vector3 Mundo(Vector2Int c) => new(c.x * Tamanho, 0f, c.y * Tamanho);
    public static Vector2Int Celula(Vector3 p) => new(Mathf.RoundToInt(p.x / Tamanho), Mathf.RoundToInt(p.z / Tamanho));
    public static float Distancia(Vector2Int a, Vector2Int b) => Vector2.Distance(a, b);

    // Dijkstra com passos retos (1) e diagonais (1,4), sem cortar quina de obstáculo.
    public Dictionary<Vector2Int, float> Alcance(Vector2Int origem, float maximo, out Dictionary<Vector2Int, Vector2Int> anterior)
    {
        var dist = new Dictionary<Vector2Int, float> { [origem] = 0f };
        anterior = new Dictionary<Vector2Int, Vector2Int>();
        var abertos = new List<Vector2Int> { origem };
        while (abertos.Count > 0)
        {
            int melhor = 0;
            for (int i = 1; i < abertos.Count; i++)
                if (dist[abertos[i]] < dist[abertos[melhor]]) melhor = i;
            var atual = abertos[melhor];
            abertos.RemoveAt(melhor);
            foreach (var d in Vizinhos)
            {
                var n = atual + d;
                if (!Vazia(n)) continue;
                bool diagonal = d.x != 0 && d.y != 0;
                if (diagonal && (!Passavel(new Vector2Int(atual.x + d.x, atual.y)) || !Passavel(new Vector2Int(atual.x, atual.y + d.y))))
                    continue;
                float nd = dist[atual] + (diagonal ? 1.4f : 1f);
                if (nd > maximo + 0.01f) continue;
                if (dist.TryGetValue(n, out float velho) && velho <= nd) continue;
                if (!dist.ContainsKey(n)) abertos.Add(n);
                dist[n] = nd;
                anterior[n] = atual;
            }
        }
        return dist;
    }

    public static List<Vector2Int> Caminho(Dictionary<Vector2Int, Vector2Int> anterior, Vector2Int origem, Vector2Int destino)
    {
        var caminho = new List<Vector2Int>();
        var c = destino;
        while (c != origem)
        {
            caminho.Add(c);
            if (!anterior.TryGetValue(c, out c)) return new List<Vector2Int>();
        }
        caminho.Reverse();
        return caminho;
    }

    // Só muros (cobertura total) bloqueiam a visão. O atirador pode "espiar" pelas casas livres ao lado.
    public bool Visao(Vector2Int de, Vector2Int para)
    {
        if (LinhaLivre(de, para)) return true;
        foreach (var d in Cardeais)
        {
            var espiar = de + d;
            if (Passavel(espiar) && TemMuroAoLado(de) && LinhaLivre(espiar, para)) return true;
        }
        return false;
    }

    bool TemMuroAoLado(Vector2Int c)
    {
        foreach (var d in Cardeais)
            if (Dentro(c + d) && tipo[c.x + d.x, c.y + d.y] == CoberturaTotal) return true;
        return false;
    }

    bool LinhaLivre(Vector2Int a, Vector2Int b)
    {
        Vector2 pa = a, pb = b;
        int passos = Mathf.CeilToInt(Vector2.Distance(pa, pb) * 5f);
        for (int i = 1; i < passos; i++)
        {
            var p = Vector2.Lerp(pa, pb, i / (float)passos);
            var c = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
            if (c == a || c == b) continue;
            if (Dentro(c) && tipo[c.x, c.y] == CoberturaTotal) return false;
        }
        return true;
    }

    // Cobertura que o alvo tem contra o atirador: maior obstáculo vizinho voltado para o atirador.
    public int Cobertura(Vector2Int alvo, Vector2Int atirador)
    {
        Vector2 dir = atirador - alvo;
        if (dir == Vector2.zero) return 0;
        dir.Normalize();
        int melhor = 0;
        foreach (var d in Cardeais)
        {
            var n = alvo + d;
            if (!Dentro(n)) continue;
            int t = tipo[n.x, n.y];
            if (t == Livre) continue;
            if (Vector2.Dot(d, dir) > 0.3f) melhor = Mathf.Max(melhor, t);
        }
        return melhor;
    }

    // Maior cobertura disponível numa casa, em qualquer direção (para os escudos da interface).
    public int CoberturaEm(Vector2Int c, Vector2Int direcao) => Dentro(c + direcao) ? tipo[c.x + direcao.x, c.y + direcao.y] : 0;
    public static Vector2Int[] Direcoes => Cardeais;
}
