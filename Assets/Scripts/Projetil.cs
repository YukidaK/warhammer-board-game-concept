using UnityEngine;

// Projétil do disparo: anda pela física (Rigidbody), deixa um rastro (TrailRenderer) e,
// ao encostar num Collider (OnTriggerEnter) ou chegar ao destino, solta as partículas de impacto.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Projetil : MonoBehaviour
{
    public float velocidade = 34f;
    public GameObject fxImpacto;

    Rigidbody rb;
    Unidade atirador, alvo;
    Vector3 origem, destino;
    float distancia;
    bool acerto, terminou;

    public bool Ativo => !terminou;
    public Vector3 Destino => destino;
    public Vector3 Origem => origem;
    public bool Acerto => acerto;

    public void Disparar(Vector3 de, Vector3 para, Unidade quem, Unidade alvo, bool acertou)
    {
        rb = GetComponent<Rigidbody>();
        origem = de;
        destino = para;
        atirador = quem;
        this.alvo = alvo;
        acerto = acertou;
        distancia = Vector3.Distance(de, para);
        var dir = (para - de).normalized;
        transform.SetPositionAndRotation(de, Quaternion.LookRotation(dir));
        rb.position = de;   // o Rigidbody só sincroniza com o Transform no próximo passo da física
        rb.rotation = transform.rotation;
        rb.linearVelocity = dir * velocidade;
    }

    void FixedUpdate()
    {
        if (terminou) return;
        // se passou do destino sem bater em nada, termina ali (no acerto, é o peito do alvo)
        if (Vector3.Distance(origem, transform.position) >= distancia + (acerto ? 0f : 3f)) Impacto(acerto ? destino : rb.position);
    }

    void OnTriggerEnter(Collider outro)
    {
        if (terminou) return;
        var u = outro.GetComponentInParent<Unidade>();
        if (u != null && u == atirador) return;
        // um tiro certeiro só para no alvo; um errado bate no que estiver no caminho
        if (acerto && u != alvo) return;
        if (!acerto && Vector3.Distance(origem, transform.position) < 1.3f) return;
        if (!acerto && u != null && u == alvo) return;
        Impacto(outro.ClosestPoint(transform.position));
    }

    void Impacto(Vector3 ponto)
    {
        terminou = true;
        rb.linearVelocity = Vector3.zero;
        if (fxImpacto != null)
        {
            var fx = Instantiate(fxImpacto, ponto, Quaternion.LookRotation(-transform.forward));
            fx.name = "FxImpacto";
        }
        GetComponent<Renderer>().enabled = false;
        Destroy(gameObject, 0.2f);   // deixa o rastro sumir
    }
}
