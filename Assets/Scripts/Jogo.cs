using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Controla a partida: monta o mapa e as equipes, alterna os turnos e executa as ações
// (mover, atirar, vigilância, defender, curar) escolhidas para cada soldado.
public class Jogo : MonoBehaviour
{
    public enum Modo { Mover, Atirar, Curar }

    [Header("Modelos")]
    public GameObject[] modelosAzuis;
    public GameObject[] modelosVermelhos;

    [Header("Materiais")]
    public Material matAzul, matVermelho, matChaoA, matChaoB, matTerreno, matMuro, matCaixa, matSaco;
    public Material matMover1, matMover2, matCursor, matTraco, matEscudo, matAnel, matBaseAzul, matBaseVermelha;

    [Header("Cena")]
    public Camera cam;
    public CameraTatica camTatica;
    public Hud hud;

    // usados pelo roteiro automático de testes
    public static int seedFixa = -1;
    public static bool autoJogador;
    public Vector2Int? mouseForcado;

    public Grade grade;
    public readonly List<Unidade> azuis = new();
    public readonly List<Unidade> vermelhos = new();
    public Unidade selecionada;
    public Modo modo = Modo.Mover;
    public int turno = 1;
    public bool turnoDoJogador;
    public bool ocupado;
    public bool Acabou { get; private set; }
    public bool Vitoria { get; private set; }
    public bool EsperandoJogador => turnoDoJogador && !ocupado && !Acabou;

    bool fimDoTurnoPedido;
    Dictionary<Vector2Int, float> alcance;
    Dictionary<Vector2Int, Vector2Int> anterior;
    readonly List<GameObject> destaques = new();
    readonly List<GameObject> escudos = new();
    GameObject cursor, anel;
    LineRenderer linhaCaminho;
    Transform mapa;

    void Start()
    {
        int seed = seedFixa >= 0 ? seedFixa : Random.Range(0, 100000);
        Random.InitState(seed);
        mapa = new GameObject("Mapa").transform;
        grade = GeradorDeMapa.Gerar(seed, this, mapa, out var inicioAzul, out var inicioVermelho);
        Debug.Log($"[Jogo] mapa gerado com seed {seed}");

        hud.Criar(this, cam);
        CriarEquipes(inicioAzul, inicioVermelho);
        CriarMarcadores();

        float t = Grade.Tamanho;
        camTatica.limiteMin = new Vector2(-2, -4);
        camTatica.limiteMax = new Vector2((grade.largura - 1) * t + 2, (grade.altura - 1) * t + 2);

        StartCoroutine(Partida());
    }

    // ---------- montagem ----------

    static readonly (string nome, string classe, int hp, int mira, int dmin, int dmax, float mob)[] Esquadrao =
    {
        ("Sgt. Varn", "Sargento", 8, 72, 3, 5, 6f),
        ("Cabo Rhee", "Fuzileiro", 7, 68, 3, 5, 6f),
        ("Kessa", "Atiradora", 6, 80, 4, 7, 5f),
        ("Doc Malek", "Médico", 6, 62, 2, 4, 6f),
    };

    static readonly Dictionary<string, (string nome, int hp, int mira, int dmin, int dmax, float mob)> Hostis = new()
    {
        ["Trooper"] = ("Soldado", 5, 58, 2, 4, 6f),
        ["Plasma"] = ("Plasma", 5, 58, 3, 5, 6f),
        ["Melta"] = ("Melta", 6, 55, 3, 6, 5f),
        ["Volleygun"] = ("Metralha", 5, 62, 2, 4, 5f),
        ["Demo"] = ("Demolidor", 6, 55, 3, 5, 6f),
        ["Recon"] = ("Batedor", 4, 60, 2, 4, 7f),
        ["Vox"] = ("Rádio", 4, 55, 2, 3, 6f),
    };

    void CriarEquipes(List<Vector2Int> inicioAzul, List<Vector2Int> inicioVermelho)
    {
        for (int i = 0; i < 4; i++)
        {
            var e = Esquadrao[i];
            var u = CriarUnidade(modelosAzuis[i], true, inicioAzul[i], e.nome);
            u.classe = e.classe;
            u.hp = u.hpMax = e.hp;
            u.mira = e.mira;
            u.danoMin = e.dmin;
            u.danoMax = e.dmax;
            u.mobilidade = e.mob;
            u.atirador = i == 2;
            u.medico = i == 3;
            azuis.Add(u);
        }

        var usados = new Dictionary<string, int>();
        for (int i = 0; i < inicioVermelho.Count; i++)
        {
            var modelo = modelosVermelhos[Random.Range(0, modelosVermelhos.Length)];
            if (!Hostis.TryGetValue(modelo.name, out var h)) h = ("Inimigo", 5, 58, 2, 4, 6f);
            usados[h.nome] = usados.TryGetValue(h.nome, out int n) ? n + 1 : 1;
            var u = CriarUnidade(modelo, false, inicioVermelho[i], usados[h.nome] > 1 ? $"{h.nome} {usados[h.nome]}" : h.nome);
            u.classe = "Inimigo";
            u.hp = u.hpMax = h.hp;
            u.mira = h.mira;
            u.danoMin = h.dmin;
            u.danoMax = h.dmax;
            u.mobilidade = h.mob;
            u.transform.rotation = Quaternion.Euler(0, 180, 0);
            vermelhos.Add(u);
        }

        foreach (var u in azuis) hud.CriarBarra(u);
        foreach (var u in vermelhos) hud.CriarBarra(u);
    }

    Unidade CriarUnidade(GameObject modelo, bool azul, Vector2Int cel, string nome)
    {
        var go = new GameObject(nome);
        go.transform.position = Grade.Mundo(cel);
        var u = go.AddComponent<Unidade>();
        u.nome = nome;
        u.azul = azul;
        u.cel = cel;
        grade.ocupante[cel.x, cel.y] = u;

        var m = Instantiate(modelo, go.transform);
        m.name = "Modelo";
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.identity;
        m.transform.localScale = Vector3.one * 1.45f;
        foreach (var r in m.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterial = azul ? matAzul : matVermelho;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
        u.modelo = m.transform;
        u.posModelo = Vector3.zero;

        var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(b.GetComponent<Collider>());
        b.name = "Base";
        b.transform.SetParent(go.transform, false);
        b.transform.localScale = new Vector3(0.95f, 0.025f, 0.95f);
        b.transform.localPosition = Vector3.up * 0.025f;
        b.GetComponent<Renderer>().sharedMaterial = azul ? matBaseAzul : matBaseVermelha;
        return u;
    }

    void CriarMarcadores()
    {
        cursor = GeradorDeMapa.Cubo(null, "Cursor", Vector3.zero, new Vector3(1.38f, 0.02f, 1.38f), matCursor);
        cursor.SetActive(false);
        anel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(anel.GetComponent<Collider>());
        anel.name = "AnelSelecao";
        anel.transform.localScale = new Vector3(1.25f, 0.012f, 1.25f);
        anel.GetComponent<Renderer>().sharedMaterial = matAnel;
        var linhaGo = new GameObject("Caminho");
        linhaCaminho = linhaGo.AddComponent<LineRenderer>();
        linhaCaminho.sharedMaterial = matCursor;
        linhaCaminho.widthMultiplier = 0.12f;
        linhaCaminho.numCapVertices = 2;
        linhaCaminho.positionCount = 0;
    }

    // ---------- turnos ----------

    IEnumerator Partida()
    {
        hud.Log("Missão: elimine todos os inimigos vermelhos.");
        while (!Acabou)
        {
            yield return TurnoJogador();
            if (Acabou) break;
            yield return TurnoInimigo();
            turno++;
        }
        LimparDestaques();
        anel.SetActive(false);
        hud.Fim(Vitoria);
        hud.Banner(Vitoria ? "VITÓRIA" : "DERROTA", Vitoria);
        Debug.Log($"[Jogo] fim da partida no turno {turno}: {(Vitoria ? "VITÓRIA" : "DERROTA")}");
    }

    IEnumerator TurnoJogador()
    {
        foreach (var u in azuis) if (u.Viva) { u.NovoTurno(); u.vigilancia = false; }
        turnoDoJogador = true;
        fimDoTurnoPedido = false;
        hud.Banner($"TURNO {turno} — SUA VEZ", true);
        hud.Log($"— Turno {turno}: equipe azul —");
        Selecionar(azuis.Find(u => u.Viva));

        while (!Acabou && !fimDoTurnoPedido && azuis.Exists(u => u.Viva && u.pa > 0))
        {
            if (autoJogador && !ocupado)
            {
                var proxima = azuis.Find(u => u.Viva && u.pa > 0);
                ocupado = true;
                Selecionar(proxima);
                yield return new WaitForSeconds(0.3f);
                LimparDestaques();
                yield return IA.Agir(this, proxima);
                ocupado = false;
                AtualizarDestaques();
            }
            yield return null;
        }
        turnoDoJogador = false;
        LimparDestaques();
        hud.Dica("");
        hud.MostrarAlvos(new List<(Unidade, int)>());
    }

    IEnumerator TurnoInimigo()
    {
        hud.Banner("TURNO INIMIGO", false);
        hud.Log($"— Turno {turno}: inimigos —");
        anel.SetActive(false);
        yield return new WaitForSeconds(0.6f);
        foreach (var u in vermelhos) if (u.Viva) { u.NovoTurno(); u.vigilancia = false; }
        foreach (var u in new List<Unidade>(vermelhos))
        {
            if (Acabou) yield break;
            if (!u.Viva) continue;
            camTatica.Focar(u.transform.position);
            yield return new WaitForSeconds(0.45f);
            yield return IA.Agir(this, u);
            yield return new WaitForSeconds(0.25f);
        }
    }

    // ---------- entrada do jogador ----------

    void Update()
    {
        if (Acabou)
        {
            if (Input.GetKeyDown(KeyCode.R)) Reiniciar();
            return;
        }
        if (Input.GetKeyDown(KeyCode.F5)) { Reiniciar(); return; }
        if (!EsperandoJogador || autoJogador) return;

        if (Input.GetKeyDown(KeyCode.Tab)) ProximaUnidade(Input.GetKey(KeyCode.LeftShift) ? -1 : 1);
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) BotaoAcao("mover");
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) BotaoAcao("atirar");
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) BotaoAcao("vigiar");
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) BotaoAcao("defender");
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) BotaoAcao("curar");
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Backspace)) BotaoAcao("fim");
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) TrocarModo(Modo.Mover);

        Hover(Input.GetMouseButtonDown(0) && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()));
    }

    // Mostra o que acontece se clicar onde está o mouse e, se clicou, executa.
    public void Hover(bool clicou)
    {
        var s = selecionada;
        Unidade sob = UnidadeSobMouse(out var casa);
        cursor.SetActive(false);
        linhaCaminho.positionCount = 0;
        LimparEscudos();
        string dica = "";

        if (s == null || !s.Viva || s.pa == 0)
        {
            hud.Dica(s != null && s.Viva ? $"{s.nome} já agiu neste turno. Tab: próximo soldado" : "");
            if (clicou && sob != null && sob.azul) Selecionar(sob);
            return;
        }

        if (modo == Modo.Mover)
        {
            hud.MostrarAlvos(new List<(Unidade, int)>());
            if (sob != null && sob.azul && sob != s)
            {
                dica = $"Selecionar {sob.nome}";
                if (clicou) Selecionar(sob);
            }
            else if (casa.HasValue && alcance != null && alcance.TryGetValue(casa.Value, out float d) && casa.Value != s.cel)
            {
                int custo = d <= s.mobilidade + 0.01f ? 1 : 2;
                cursor.SetActive(true);
                cursor.transform.position = Grade.Mundo(casa.Value) + Vector3.up * 0.04f;
                var caminho = Grade.Caminho(anterior, s.cel, casa.Value);
                linhaCaminho.positionCount = caminho.Count + 1;
                linhaCaminho.SetPosition(0, s.transform.position + Vector3.up * 0.08f);
                for (int i = 0; i < caminho.Count; i++) linhaCaminho.SetPosition(i + 1, Grade.Mundo(caminho[i]) + Vector3.up * 0.08f);
                int melhor = MostrarEscudos(casa.Value);
                dica = (custo == 1 ? "Mover (1 ação)" : "Avançar (2 ações)") + (melhor == 2 ? "  ·  cobertura TOTAL" : melhor == 1 ? "  ·  meia cobertura" : "  ·  sem cobertura");
                if (clicou) StartCoroutine(Executar(Mover(s, casa.Value, custo)));
            }
            else if (sob != null && !sob.azul)
            {
                dica = "Aperte 2 (Atirar) para mirar neste inimigo";
            }
        }
        else if (modo == Modo.Atirar)
        {
            var alvos = Alvos(s);
            hud.MostrarAlvos(alvos.ConvertAll(a => (a.alvo, a.chance)));
            if (alvos.Count == 0) dica = "Nenhum inimigo à vista. Clique direito para voltar";
            else dica = "Clique num inimigo para atirar";
            foreach (var a in alvos)
                if (a.alvo == sob)
                {
                    Chance(s, a.alvo, out int cob, out int crit);
                    dica = $"{a.alvo.nome}: {a.chance}% de acerto  ·  dano {s.danoMin}-{s.danoMax}  ·  crítico {crit}%" +
                           (cob == 2 ? "  ·  alvo em cobertura total" : cob == 1 ? "  ·  alvo em meia cobertura" : "  ·  FLANQUEADO");
                    cursor.SetActive(true);
                    cursor.transform.position = Grade.Mundo(a.alvo.cel) + Vector3.up * 0.04f;
                    if (clicou) StartCoroutine(Executar(Atirar(s, a.alvo)));
                }
            if (clicou && sob != null && sob.azul && sob != s) Selecionar(sob);
        }
        else if (modo == Modo.Curar)
        {
            dica = "Clique em você ou num aliado ao lado para curar 4 HP";
            if (sob != null && sob.azul && Grade.Distancia(sob.cel, s.cel) < 1.5f)
            {
                dica = $"Curar {sob.nome} (HP {sob.hp}/{sob.hpMax})";
                if (clicou) StartCoroutine(Executar(Curar(s, sob)));
            }
        }
        hud.Dica(dica);
    }

    Unidade UnidadeSobMouse(out Vector2Int? casa)
    {
        casa = null;
        if (mouseForcado.HasValue)
        {
            casa = mouseForcado.Value;
            return grade.Dentro(casa.Value) ? grade.ocupante[casa.Value.x, casa.Value.y] : null;
        }
        var raio = cam.ScreenPointToRay(Input.mousePosition);
        if (new Plane(Vector3.up, Vector3.zero).Raycast(raio, out float dist))
        {
            var c = Grade.Celula(raio.GetPoint(dist));
            if (grade.Dentro(c)) casa = c;
        }
        // o corpo da unidade na tela tem prioridade sobre a casa do chão
        Unidade melhor = null;
        float melhorD = 45f * Screen.height / 720f;
        foreach (var u in Todas())
        {
            if (!u.Viva) continue;
            var p = cam.WorldToScreenPoint(u.transform.position + Vector3.up * 0.8f);
            if (p.z < 0) continue;
            float d = Vector2.Distance(p, Input.mousePosition);
            if (d < melhorD) { melhorD = d; melhor = u; }
        }
        if (melhor != null) return melhor;
        return casa.HasValue ? grade.ocupante[casa.Value.x, casa.Value.y] : null;
    }

    public void BotaoAcao(string acao)
    {
        if (acao == "fim")
        {
            if (EsperandoJogador)
            {
                fimDoTurnoPedido = true;
                hud.Log("Turno encerrado.");
            }
            return;
        }
        var s = selecionada;
        if (!EsperandoJogador || s == null || !s.Viva || s.pa == 0) return;
        switch (acao)
        {
            case "mover": TrocarModo(Modo.Mover); break;
            case "atirar": TrocarModo(Modo.Atirar); break;
            case "curar": if (s.medico && s.curas > 0) TrocarModo(Modo.Curar); break;
            case "vigiar": StartCoroutine(Executar(Vigiar(s))); break;
            case "defender": StartCoroutine(Executar(Defender(s))); break;
        }
    }

    void TrocarModo(Modo m)
    {
        modo = m;
        AtualizarDestaques();
        if (m != Modo.Atirar || selecionada == null) return;
        // enquadra o atirador e o alvo mais provável
        var alvos = Alvos(selecionada);
        if (alvos.Count == 0) return;
        alvos.Sort((a, b) => b.chance.CompareTo(a.chance));
        var alvo = alvos[0].alvo;
        camTatica.Focar((selecionada.transform.position + alvo.transform.position) / 2);
        camTatica.ZoomSuave(Mathf.Max(18f, Vector3.Distance(selecionada.transform.position, alvo.transform.position) * 1.15f));
    }

    public void Selecionar(Unidade u)
    {
        if (u == null) return;
        selecionada = u;
        modo = Modo.Mover;
        if (u.azul && turnoDoJogador) camTatica.Focar(u.transform.position);
        AtualizarDestaques();
    }

    void ProximaUnidade(int dir)
    {
        int i = Mathf.Max(0, azuis.IndexOf(selecionada));
        for (int k = 1; k <= azuis.Count; k++)
        {
            var u = azuis[((i + dir * k) % azuis.Count + azuis.Count) % azuis.Count];
            if (u.Viva && u.pa > 0) { Selecionar(u); return; }
        }
    }

    IEnumerator Executar(IEnumerator acao)
    {
        ocupado = true;
        LimparDestaques();
        hud.Dica("");
        hud.MostrarAlvos(new List<(Unidade, int)>());
        yield return acao;
        ocupado = false;
        if (Acabou) yield break;
        if (selecionada == null || !selecionada.Viva || selecionada.pa == 0)
        {
            var proxima = azuis.Find(u => u.Viva && u.pa > 0);
            if (proxima != null) Selecionar(proxima);
        }
        else Selecionar(selecionada);
    }

    // ---------- destaques no tabuleiro ----------

    void AtualizarDestaques()
    {
        LimparDestaques();
        var s = selecionada;
        if (s == null || !s.Viva) { anel.SetActive(false); return; }
        anel.SetActive(true);
        anel.transform.SetParent(s.transform, false);
        anel.transform.localPosition = Vector3.up * 0.015f;
        if (!turnoDoJogador || s.pa == 0 || modo != Modo.Mover || autoJogador) return;

        alcance = grade.Alcance(s.cel, s.mobilidade * s.pa, out anterior);
        foreach (var kv in alcance)
        {
            if (kv.Key == s.cel) continue;
            var d = Destaque();
            d.transform.position = Grade.Mundo(kv.Key) + Vector3.up * 0.02f;
            d.GetComponent<Renderer>().sharedMaterial = kv.Value <= s.mobilidade + 0.01f ? matMover1 : matMover2;
        }
    }

    GameObject Destaque()
    {
        foreach (var d in destaques)
            if (!d.activeSelf) { d.SetActive(true); return d; }
        var novo = GeradorDeMapa.Cubo(null, "Destaque", Vector3.zero, new Vector3(1.3f, 0.01f, 1.3f), matMover1);
        destaques.Add(novo);
        return novo;
    }

    void LimparDestaques()
    {
        foreach (var d in destaques) d.SetActive(false);
        alcance = null;
        if (cursor != null) cursor.SetActive(false);
        if (linhaCaminho != null) linhaCaminho.positionCount = 0;
        LimparEscudos();
    }

    int MostrarEscudos(Vector2Int c)
    {
        int melhor = 0;
        int i = 0;
        foreach (var dir in Grade.Direcoes)
        {
            int t = grade.CoberturaEm(c, dir);
            if (t == Grade.Livre) continue;
            melhor = Mathf.Max(melhor, t);
            if (i >= escudos.Count) escudos.Add(GeradorDeMapa.Cubo(null, "Escudo", Vector3.zero, Vector3.one, matEscudo));
            var e = escudos[i++];
            e.SetActive(true);
            float h = t == 2 ? 0.8f : 0.4f;
            var pos = Grade.Mundo(c) + new Vector3(dir.x, 0, dir.y) * 0.55f + Vector3.up * (h / 2 + 0.05f);
            e.transform.position = pos;
            e.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
            e.transform.localScale = new Vector3(0.6f, h, 0.06f);
        }
        return melhor;
    }

    void LimparEscudos()
    {
        foreach (var e in escudos) e.SetActive(false);
    }

    // ---------- regras ----------

    public IEnumerable<Unidade> Todas()
    {
        foreach (var u in azuis) yield return u;
        foreach (var u in vermelhos) yield return u;
    }

    public List<Unidade> Oponentes(Unidade u) => u.azul ? vermelhos : azuis;

    public float AlcanceDeTiro(Unidade u) => u.atirador ? 22f : 13f;

    public bool PodeVer(Unidade a, Unidade b) =>
        a.Viva && b.Viva && Grade.Distancia(a.cel, b.cel) <= AlcanceDeTiro(a) && grade.Visao(a.cel, b.cel);

    public int Chance(Unidade a, Unidade alvo, out int cobertura, out int critico, bool reacao = false)
    {
        cobertura = grade.Cobertura(alvo.cel, a.cel);
        float d = Grade.Distancia(a.cel, alvo.cel);
        int c = a.mira;
        c -= cobertura == 2 ? 40 : cobertura == 1 ? 20 : 0;
        if (alvo.defendendo) c -= 20;
        if (a.atirador) c += d >= 6 ? 10 : -10;
        else if (d <= 3) c += 15;
        else if (d > 10) c -= Mathf.RoundToInt((d - 10) * 4);
        if (reacao) c -= 15;
        critico = cobertura == 0 ? 33 : 10;
        return Mathf.Clamp(c, 5, 95);
    }

    public List<(Unidade alvo, int chance)> Alvos(Unidade a)
    {
        var lista = new List<(Unidade, int)>();
        foreach (var o in Oponentes(a))
            if (PodeVer(a, o)) lista.Add((o, Chance(a, o, out _, out _)));
        return lista;
    }

    public IEnumerator Mover(Unidade u, Vector2Int destino, int custo)
    {
        var dist = grade.Alcance(u.cel, u.mobilidade * custo, out var ant);
        if (!dist.ContainsKey(destino)) yield break;
        var caminho = Grade.Caminho(ant, u.cel, destino);
        u.pa -= custo;
        if (custo == 2) hud.Log($"{u.nome} avançou correndo.");
        yield return u.Andar(caminho, grade, Vigilancias);
        if (!u.Viva) u.pa = 0;
        // fica virado para o inimigo mais próximo
        Unidade perto = null;
        foreach (var o in Oponentes(u))
            if (o.Viva && (perto == null || Grade.Distancia(o.cel, u.cel) < Grade.Distancia(perto.cel, u.cel))) perto = o;
        if (perto != null && u.Viva) u.Virar(perto.transform.position);
    }

    // Unidades em vigilância atiram na primeira unidade inimiga que se mover à vista.
    IEnumerator Vigilancias(Unidade movendo)
    {
        foreach (var o in new List<Unidade>(Oponentes(movendo)))
        {
            if (!o.Viva || !o.vigilancia || !movendo.Viva) continue;
            if (!PodeVer(o, movendo)) continue;
            o.vigilancia = false;
            hud.Popup(o.transform.position, "VIGILÂNCIA!", new Color(1f, 0.8f, 0.3f));
            yield return Atirar(o, movendo, true);
        }
    }

    public IEnumerator Atirar(Unidade a, Unidade alvo, bool reacao = false)
    {
        int chance = Chance(a, alvo, out _, out int crit, reacao);
        a.Virar(alvo.transform.position);
        if (!reacao)
        {
            a.pa = 0;
            a.vigilancia = false;
        }
        var foco = (a.transform.position + alvo.transform.position) / 2;
        camTatica.Focar(foco);
        yield return new WaitForSeconds(0.35f);

        bool acertou = Random.Range(0, 100) < chance;
        bool critico = acertou && Random.Range(0, 100) < crit;
        var destino = acertou ? alvo.Peito : alvo.Peito + new Vector3(Random.Range(-1f, 1f), Random.Range(0.3f, 1f), Random.Range(-1f, 1f));
        var origem = a.transform.position + Vector3.up * 0.95f + a.transform.forward * 0.45f;

        int rajadas = a.atirador ? 1 : 3;
        for (int i = 0; i < rajadas; i++)
        {
            StartCoroutine(a.Recuo());
            StartCoroutine(Traco(origem, destino + Random.insideUnitSphere * 0.1f));
            yield return new WaitForSeconds(0.09f);
        }
        yield return new WaitForSeconds(0.2f);

        if (acertou)
        {
            int dano = Random.Range(a.danoMin, a.danoMax + 1) + (critico ? 2 : 0);
            alvo.hp = Mathf.Max(0, alvo.hp - dano);
            hud.Popup(alvo.transform.position, critico ? $"CRÍTICO -{dano}" : $"-{dano}", critico ? new Color(1f, 0.6f, 0.1f) : new Color(1f, 0.35f, 0.3f));
            hud.Log($"{a.nome} atirou em {alvo.nome} ({chance}%): {(critico ? "CRÍTICO" : "acertou")}, {dano} de dano.");
            StartCoroutine(alvo.Tremer());
            if (!alvo.Viva)
            {
                hud.Log($"{alvo.nome} foi abatido!");
                alvo.vigilancia = false;
                yield return alvo.Morrer(grade);
                VerificarFim();
            }
        }
        else
        {
            hud.Popup(alvo.transform.position, "ERROU", new Color(0.85f, 0.85f, 0.85f));
            hud.Log($"{a.nome} atirou em {alvo.nome} ({chance}%): errou.");
        }
        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator Traco(Vector3 de, Vector3 para)
    {
        var go = new GameObject("Traco");
        var l = go.AddComponent<LineRenderer>();
        l.sharedMaterial = matTraco;
        l.widthMultiplier = 0.06f;
        l.positionCount = 2;
        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(flash.GetComponent<Collider>());
        flash.transform.position = de;
        flash.transform.localScale = Vector3.one * 0.25f;
        flash.GetComponent<Renderer>().sharedMaterial = matTraco;
        float dur = 0.12f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            l.SetPosition(0, Vector3.Lerp(de, para, Mathf.Max(0, k - 0.35f)));
            l.SetPosition(1, Vector3.Lerp(de, para, k));
            yield return null;
        }
        Destroy(flash);
        Destroy(go);
    }

    public IEnumerator Vigiar(Unidade u)
    {
        u.pa = 0;
        u.vigilancia = true;
        hud.Popup(u.transform.position, "VIGILÂNCIA", new Color(1f, 0.8f, 0.3f));
        hud.Log($"{u.nome} entrou em vigilância.");
        yield return new WaitForSeconds(0.4f);
    }

    public IEnumerator Defender(Unidade u)
    {
        u.pa = 0;
        u.defendendo = true;
        hud.Popup(u.transform.position, "DEFENDENDO", new Color(0.55f, 0.75f, 1f));
        hud.Log($"{u.nome} se abaixou para se defender (-20% para acertá-lo).");
        yield return new WaitForSeconds(0.4f);
    }

    public IEnumerator Curar(Unidade medico, Unidade alvo)
    {
        medico.pa = 0;
        medico.curas--;
        int antes = alvo.hp;
        alvo.hp = Mathf.Min(alvo.hpMax, alvo.hp + 4);
        hud.Popup(alvo.transform.position, $"+{alvo.hp - antes} HP", new Color(0.4f, 1f, 0.5f));
        hud.Log($"{medico.nome} curou {alvo.nome} em {alvo.hp - antes} HP.");
        yield return new WaitForSeconds(0.5f);
    }

    void VerificarFim()
    {
        if (!vermelhos.Exists(u => u.Viva)) { Acabou = true; Vitoria = true; }
        else if (!azuis.Exists(u => u.Viva)) { Acabou = true; Vitoria = false; }
    }

    void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
