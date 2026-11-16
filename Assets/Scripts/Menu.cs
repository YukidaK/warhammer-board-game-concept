using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using E = UiEstilo;

// Menu inicial montado por código: escolha do seu time, do time adversário e do cenário.
// Ao fundo, uma vitrine 3D mostra os modelos dos dois times escolhidos girando em pedestais.
public class Menu : MonoBehaviour
{
    public Equipe[] equipes;
    public Cenario[] cenarios;
    public Camera cam;
    public Material matPedestalAzul, matPedestalVermelho;

    int jogador, inimigo, cenario = -1;
    RectTransform raiz;
    readonly List<(Image fundo, Image borda, Image faixa, Text txt)> cartoesJogador = new(), cartoesInimigo = new(), cartoesCenario = new();
    Text descJogador, descInimigo, descCenario, rosterJogador, rosterInimigo, confronto;
    Transform vitrineJogador, vitrineInimigo;
    float giro;

    void Start()
    {
        jogador = Mathf.Clamp(Selecao.jogador, 0, equipes.Length - 1);
        inimigo = Selecao.inimigo >= 0 ? Mathf.Clamp(Selecao.inimigo, 0, equipes.Length - 1) : Mathf.Min(1, equipes.Length - 1);
        cenario = Selecao.cenario;
        CriarInterface();
        Atualizar();
    }

    void Update()
    {
        giro += Time.deltaTime * 18f;
        if (vitrineJogador != null) GirarVitrine(vitrineJogador, 1);
        if (vitrineInimigo != null) GirarVitrine(vitrineInimigo, -1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Iniciar();
        if (Input.GetKeyDown(KeyCode.Escape)) Sair();
    }

    void GirarVitrine(Transform v, int lado)
    {
        foreach (Transform p in v)
            if (p.childCount > 0) p.GetChild(0).localRotation = Quaternion.Euler(0, Mathf.Sin(giro * Mathf.Deg2Rad * 2f + p.GetSiblingIndex()) * 25f, 0);
    }

    // ---------- ações ----------

    public void EscolherJogador(int i) { jogador = i; Atualizar(); }
    public void EscolherInimigo(int i) { inimigo = i; Atualizar(); }
    public void EscolherCenario(int i) { cenario = i; Atualizar(); }

    void Iniciar()
    {
        Selecao.jogador = jogador;
        Selecao.inimigo = inimigo;
        Selecao.cenario = cenario;
        SceneManager.LoadScene("Batalha");
    }

    void Sair()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Atualizar()
    {
        Marcar(cartoesJogador, jogador, E.Azul);
        Marcar(cartoesInimigo, inimigo, E.Vermelho);
        Marcar(cartoesCenario, cenario + 1, E.Ouro);

        descJogador.text = equipes[jogador].descricao;
        descInimigo.text = equipes[inimigo].descricao;
        rosterJogador.text = Ficha(equipes[jogador], true);
        rosterInimigo.text = Ficha(equipes[inimigo], false);
        descCenario.text = cenario < 0 ? "Um dos cenários é sorteado a cada partida." : cenarios[cenario].descricao;
        confronto.text = $"<color={E.Hex(E.Azul)}>{equipes[jogador].nome.ToUpper()}</color>   <color={E.Hex(E.TextoFraco)}><size=18>VS</size></color>   <color={E.Hex(E.Vermelho)}>{equipes[inimigo].nome.ToUpper()}</color>";

        MontarVitrine(ref vitrineJogador, equipes[jogador], true);
        MontarVitrine(ref vitrineInimigo, equipes[inimigo], false);
    }

    static void Marcar(List<(Image fundo, Image borda, Image faixa, Text txt)> lista, int escolhido, Color cor)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            bool sel = i == escolhido;
            var c = lista[i];
            c.fundo.color = sel ? Color.Lerp(E.FundoClaro, cor, 0.28f) : E.FundoClaro;
            c.borda.color = sel ? cor : E.Borda;
            if (c.faixa != null) c.faixa.gameObject.SetActive(sel);
            c.txt.color = sel ? Color.white : E.TextoFraco;
        }
    }

    // Lista de membros com HP e mira alinhados à direita.
    string Ficha(Equipe e, bool doJogador)
    {
        var sb = new System.Text.StringBuilder();
        int n = doJogador ? Mathf.Min(4, e.membros.Length) : Mathf.Min(6, e.membros.Length);
        string ouro = E.Hex(E.Ouro), fraco = E.Hex(E.TextoFraco);
        sb.Append(doJogador ? $"<color={ouro}>ESQUADRÃO</color>\n" : $"<color={ouro}>ELENCO  ·  {Mathf.Min(e.qtdInimigos, 6)} EM CAMPO</color>\n");
        for (int i = 0; i < n; i++)
        {
            var m = e.membros[i];
            sb.Append($"<b>{m.nome}</b>  <color={fraco}>{m.classe}  ·  HP {m.hp}  ·  MIRA {m.mira}</color>");
            if (i < n - 1) sb.Append('\n');
        }
        if (!doJogador && e.membros.Length > n) sb.Append($"\n<color={fraco}>+{e.membros.Length - n} outros</color>");
        return sb.ToString();
    }

    // Quatro modelos do time em pedestais, à esquerda (jogador) ou à direita (oponente) da câmera.
    void MontarVitrine(ref Transform vitrine, Equipe e, bool doJogador)
    {
        if (vitrine != null) Destroy(vitrine.gameObject);
        vitrine = new GameObject(doJogador ? "VitrineJogador" : "VitrineInimigo").transform;
        float lado = doJogador ? -1 : 1;
        int n = Mathf.Min(4, e.membros.Length);
        for (int i = 0; i < n; i++)
        {
            var m = e.membros[i];
            var ped = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ped.GetComponent<Collider>());
            ped.name = "Pedestal";
            ped.transform.SetParent(vitrine, false);
            // duas fileiras no centro da tela (as colunas laterais cobrem as bordas)
            float x = i < 2 ? lado * (0.75f + i * 1.25f) : lado * (1.35f + (i - 2) * 1.25f);
            float z = i < 2 ? 1f : 3.2f;
            ped.transform.localPosition = new Vector3(x, 0, z);
            ped.transform.localScale = new Vector3(1.1f, 0.06f, 1.1f);
            ped.GetComponent<Renderer>().sharedMaterial = doJogador ? matPedestalAzul : matPedestalVermelho;

            var suporte = new GameObject("Modelo").transform;
            suporte.SetParent(ped.transform.parent, false);
            suporte.localPosition = new Vector3(x, 0.06f, z);
            var pivo = new GameObject("Pivo").transform;
            pivo.SetParent(suporte, false);
            var go = Instantiate(m.modelo, pivo);
            go.transform.localScale = Vector3.one * e.escala * m.escala * 0.8f;
            go.transform.localRotation = Quaternion.Euler(0, m.giro + 180 + lado * -25, 0);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = e.material;
        }
    }

    // ---------- interface ----------

    void CriarInterface()
    {
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        raiz = canvasGo.GetComponent<RectTransform>();

        // faixas escuras em cima e embaixo para dar leitura ao texto sobre a cena 3D
        var topo = E.Bloco(raiz, "SombraTopo", new Vector2(0.5f, 1), Vector2.zero, new Vector2(4000, 120), new Color(0, 0, 0, 0.35f));
        topo.rectTransform.anchorMin = new Vector2(0, 1); topo.rectTransform.anchorMax = new Vector2(1, 1);
        topo.rectTransform.offsetMin = new Vector2(0, -120); topo.rectTransform.offsetMax = Vector2.zero;

        // título
        var t = E.Retangulo(raiz, "Titulo", new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(520, 70));
        E.Rotulo(t, "KILL TEAM", 46, E.Titulo, E.Ouro, TextAnchor.UpperCenter, 0, 0, 0, 0, true);
        E.Bloco(t, "Linha", new Vector2(0.5f, 1), new Vector2(0, -54), new Vector2(200, 2), E.ComAlfa(E.Ouro, 0.6f));
        E.Rotulo(t, "T Á T I C O", 14, E.Forte, E.TextoFraco, TextAnchor.UpperCenter, 0, 59, 0, 0);

        Coluna("SEU TIME", new Vector2(0, 1), new Vector2(16, -16), E.Azul, cartoesJogador, EscolherJogador, out descJogador, out rosterJogador);
        Coluna("OPONENTE", new Vector2(1, 1), new Vector2(-16, -16), E.Vermelho, cartoesInimigo, EscolherInimigo, out descInimigo, out rosterInimigo);
        CriarRodape();
    }

    void Coluna(string titulo, Vector2 ancora, Vector2 pos, Color cor, List<(Image fundo, Image borda, Image faixa, Text txt)> lista,
                System.Action<int> aoEscolher, out Text desc, out Text roster)
    {
        const float L = 290;
        var p = E.Painel(raiz, titulo, ancora, pos, new Vector2(L, 688)).transform;
        E.Bloco(p, "Acento", new Vector2(0, 1), new Vector2(16, -16), new Vector2(4, 18), cor);
        E.Rotulo(p, titulo, 20, E.Titulo, cor, TextAnchor.UpperLeft, 28, 12, 0, 0);

        for (int i = 0; i < equipes.Length; i++)
        {
            int idx = i;
            E.Botao(p, equipes[i].nome, new Vector2(0, 1), new Vector2(16, -46 - i * 40), new Vector2(L - 32, 36), () => aoEscolher(idx), out var f, out var b);
            var faixa = E.Bloco(f.transform, "Faixa", new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(4, 22), cor);
            var tx = E.Rotulo(f.transform, equipes[i].nome.ToUpper(), 17, E.Titulo, E.Texto, TextAnchor.MiddleLeft, 16, 0, 8, 0);
            lista.Add((f, b, faixa, tx));
        }

        float y = 56 + equipes.Length * 40;
        E.Bloco(p, "Linha", new Vector2(0, 1), new Vector2(16, -y), new Vector2(L - 32, 1), E.Borda);
        desc = E.Rotulo(p, "", 15, E.Normal, E.TextoFraco, TextAnchor.UpperLeft, 16, y + 10, 16, 0);
        desc.horizontalOverflow = HorizontalWrapMode.Wrap;
        roster = E.Rotulo(p, "", 14, E.Normal, E.Texto, TextAnchor.LowerLeft, 16, 0, 16, 16);
        roster.horizontalOverflow = HorizontalWrapMode.Wrap;
        roster.lineSpacing = 1.1f;
    }

    void CriarRodape()
    {
        var p = E.Painel(raiz, "Rodape", new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(640, 196)).transform;
        confronto = E.Rotulo(p, "", 24, E.Titulo, E.Texto, TextAnchor.UpperCenter, 0, 10, 0, 0);
        E.Rotulo(p, "CENÁRIO", 13, E.Forte, E.Ouro, TextAnchor.UpperLeft, 16, 46, 0, 0);

        var nomes = new List<string> { "Aleatório" };
        foreach (var c in cenarios) nomes.Add(c.nome);
        float w = (640 - 32 - (nomes.Count - 1) * 6) / nomes.Count;
        for (int i = 0; i < nomes.Count; i++)
        {
            int idx = i - 1;
            E.Botao(p, nomes[i], new Vector2(0, 1), new Vector2(16 + i * (w + 6), -64), new Vector2(w, 30), () => EscolherCenario(idx), out var f, out var b);
            var tx = E.Rotulo(f.transform, nomes[i].ToUpper(), 14, E.Titulo, E.Texto, TextAnchor.MiddleCenter);
            cartoesCenario.Add((f, b, null, tx));
        }
        descCenario = E.Rotulo(p, "", 14, E.Normal, E.TextoFraco, TextAnchor.UpperCenter, 16, 98, 16, 0);
        descCenario.horizontalOverflow = HorizontalWrapMode.Wrap;

        E.Botao(p, "Iniciar", new Vector2(0.5f, 0), new Vector2(-62, 12), new Vector2(380, 40), Iniciar, out var fi, out var bi);
        fi.color = E.OuroEscuro; bi.color = E.Ouro;
        E.Tecla(fi.transform, "ENTER", new Vector2(0, 0.5f), new Vector2(12, 0), 42);
        E.Rotulo(fi.transform, "INICIAR BATALHA", 22, E.Titulo, E.Texto, TextAnchor.MiddleCenter, 50, 0, 0, 0);
        E.Botao(p, "Sair", new Vector2(0.5f, 0), new Vector2(196, 12), new Vector2(120, 40), Sair, out var fs, out _);
        E.Tecla(fs.transform, "Esc", new Vector2(0, 0.5f), new Vector2(10, 0), 30);
        E.Rotulo(fs.transform, "SAIR", 18, E.Titulo, E.Texto, TextAnchor.MiddleCenter, 36, 0, 0, 0);
    }
}
