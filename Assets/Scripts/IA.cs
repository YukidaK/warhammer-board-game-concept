using System.Collections;
using UnityEngine;

// Decisão automática de uma unidade: atira se tem boa chance; senão procura uma casa com
// cobertura, de preferência flanqueando, a uma distância boa do inimigo mais próximo.
public static class IA
{
    public static IEnumerator Agir(Jogo j, Unidade u)
    {
        int seguranca = 0;
        while (u.Viva && u.pa > 0 && !j.Acabou && seguranca++ < 4)
        {
            var (alvo, chance) = MelhorAlvo(j, u);

            if (u.pa == 2 && alvo != null && chance >= 55)
            {
                yield return j.Atirar(u, alvo);
                yield break;
            }

            if (u.pa == 1)
            {
                if (alvo != null)
                {
                    yield return j.Atirar(u, alvo);
                    yield break;
                }
                if (u.medico && u.curas > 0)
                {
                    var ferido = Ferido(j, u);
                    if (ferido != null)
                    {
                        yield return j.Curar(u, ferido);
                        yield break;
                    }
                }
                if (DistanciaAoMaisProximo(j, u, u.cel) <= 11f)
                {
                    yield return j.Vigiar(u);
                    yield break;
                }
            }

            var destino = MelhorCasa(j, u);
            if (destino == u.cel)
            {
                if (alvo != null) yield return j.Atirar(u, alvo);
                else yield return j.Vigiar(u);
                yield break;
            }
            yield return j.Mover(u, destino, 1);
        }
    }

    static (Unidade, int) MelhorAlvo(Jogo j, Unidade u)
    {
        Unidade melhor = null;
        int melhorChance = -1;
        foreach (var (alvo, chance) in j.Alvos(u))
        {
            // prefere quem morre com o tiro
            int peso = chance + (alvo.hp <= u.danoMax ? 10 : 0);
            if (peso > melhorChance) { melhorChance = peso; melhor = alvo; }
        }
        return melhor == null ? (null, 0) : (melhor, j.Chance(u, melhor, out _, out _));
    }

    static Unidade Ferido(Jogo j, Unidade medico)
    {
        Unidade pior = null;
        foreach (var a in medico.azul ? j.azuis : j.vermelhos)
            if (a.Viva && a.hp <= a.hpMax - 3 && Grade.Distancia(a.cel, medico.cel) < 1.5f && (pior == null || a.hp < pior.hp))
                pior = a;
        return pior;
    }

    static float DistanciaAoMaisProximo(Jogo j, Unidade u, Vector2Int c)
    {
        float m = 999f;
        foreach (var o in j.Oponentes(u))
            if (o.Viva) m = Mathf.Min(m, Grade.Distancia(c, o.cel));
        return m;
    }

    static Vector2Int MelhorCasa(Jogo j, Unidade u)
    {
        var g = j.grade;
        var alcance = g.Alcance(u.cel, u.mobilidade, out _);
        float ideal = u.atirador ? 10f : u.danoMax >= 6 ? 4f : 6.5f;
        Vector2Int melhor = u.cel;
        float melhorNota = float.MinValue;

        foreach (var kv in alcance)
        {
            var c = kv.Key;
            float dmin = DistanciaAoMaisProximo(j, u, c);
            float nota = -Mathf.Abs(dmin - ideal) * 1.6f;
            int vistos = 0;
            foreach (var o in j.Oponentes(u))
            {
                if (!o.Viva) continue;
                float d = Grade.Distancia(c, o.cel);
                if (d > 14f || !g.Visao(c, o.cel)) continue;
                vistos++;
                int minhaCobertura = g.Cobertura(c, o.cel);
                nota += minhaCobertura == 2 ? 22 : minhaCobertura == 1 ? 12 : -12;
                if (g.Cobertura(o.cel, c) == 0) nota += 14;  // flanqueando
            }
            if (vistos == 0) nota -= 6;
            nota -= kv.Value * 0.15f;   // leve preferência por andar menos
            nota += Random.Range(0f, 2.5f);
            if (nota > melhorNota) { melhorNota = nota; melhor = c; }
        }
        return melhor;
    }
}
