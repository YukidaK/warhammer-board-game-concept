using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Um soldado no tabuleiro: atributos, pontos de ação e animações de andar, atirar e morrer.
public class Unidade : MonoBehaviour
{
    public string nome;
    public string classe;
    public bool azul;
    public int hp, hpMax;
    public int mira;
    public float mobilidade = 6f;
    public int danoMin, danoMax;
    public int pa;
    public bool vigilancia, defendendo;
    public bool atirador;   // sniper: bônus de longe, penalidade de perto
    public bool medico;
    public int curas = 2;
    public Vector2Int cel;
    public Transform modelo;
    public Vector3 posModelo;

    public bool Viva => hp > 0;
    public Vector3 Peito => transform.position + Vector3.up * 1.0f;

    public void NovoTurno()
    {
        pa = 2;
        defendendo = false;
    }

    public IEnumerator Andar(List<Vector2Int> caminho, Grade grade, System.Func<Unidade, IEnumerator> aoEntrarNaCasa)
    {
        foreach (var proxima in caminho)
        {
            grade.ocupante[cel.x, cel.y] = null;
            cel = proxima;
            grade.ocupante[cel.x, cel.y] = this;

            var de = transform.position;
            var para = Grade.Mundo(proxima);
            Virar(para);
            float dur = Vector3.Distance(de, para) / 6f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                transform.position = Vector3.Lerp(de, para, k) + Vector3.up * Mathf.Abs(Mathf.Sin(k * Mathf.PI)) * 0.12f;
                yield return null;
            }
            transform.position = para;
            yield return aoEntrarNaCasa(this);
            if (!Viva) yield break;
        }
    }

    public void Virar(Vector3 alvo)
    {
        var d = alvo - transform.position;
        d.y = 0;
        if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
    }

    public IEnumerator Recuo()
    {
        for (float t = 0; t < 0.15f; t += Time.deltaTime)
        {
            modelo.localPosition = posModelo - Vector3.forward * Mathf.Sin(t / 0.15f * Mathf.PI) * 0.12f;
            yield return null;
        }
        modelo.localPosition = posModelo;
    }

    public IEnumerator Tremer()
    {
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            modelo.localPosition = posModelo + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * 0.06f;
            yield return null;
        }
        modelo.localPosition = posModelo;
    }

    public IEnumerator Morrer(Grade grade)
    {
        grade.ocupante[cel.x, cel.y] = null;
        var inicio = transform.rotation;
        var fim = inicio * Quaternion.Euler(-85f, 0, 0);
        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            transform.rotation = Quaternion.Slerp(inicio, fim, t / 0.6f);
            yield return null;
        }
        transform.rotation = fim;
        yield return new WaitForSeconds(0.6f);
        var escala = transform.localScale;
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            transform.localScale = escala * (1f - t / 0.4f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
