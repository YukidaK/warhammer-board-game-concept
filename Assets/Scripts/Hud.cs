using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using E = UiEstilo;

// Interface da partida montada por código (estilo em UiEstilo): cartões do esquadrão, faixa de turno,
// barra de ações com atalhos, placar e legenda, registro de combate, barras de vida sobre as unidades,
// chance de acerto sobre os alvos, mensagens de pontos e tela de fim.
public class Hud : MonoBehaviour
{
    Jogo jogo;
    Camera cam;
    RectTransform raiz;

    // faixa de turno
    Image faixaFundo, faixaLinha;
    Text faixaTurno, faixaFase;

    // cartões do esquadrão
    readonly List<Cartao> cartoes = new();
    class Cartao
    {
        public Image fundo, borda, faixa;
        public Text nome, info, estado;
        public Image[] pa = new Image[2];
        public RectTransform vidaArea;
        public readonly List<Image> segmentos = new();
    }

    // barra de ações
    Text infoNome, infoStats;
    readonly Dictionary<string, (Button b, Image fundo, Image borda, Text txt, Image tecla)> botoes = new();

    // placar e legenda
    Text placar, feed;
    GameObject painelLegenda;
    float placarMostrado, pulso;
    readonly List<string> linhasFeed = new();

    // registro de combate
    Text logTexto;
    readonly List<string> linhasLog = new();

    Text dica;
    GameObject telaFim;
    Text fimTitulo, fimPontos, fimSub;
    Image fimLinha;

    readonly Dictionary<Unidade, Barra> barras = new();
    class Barra { public RectTransform rt; public Image vida, fundo; public Text texto; }
    readonly List<(RectTransform rt, Image fundo, Text txt)> rotulosAlvo = new();
    readonly List<(RectTransform rt, Vector3 mundo, float t)> popups = new();

    public void Criar(Jogo jogo, Camera cam)
    {
        this.jogo = jogo;
        this.cam = cam;

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        raiz = canvasGo.GetComponent<RectTransform>();

        CriarFaixa();
        CriarCartoes();
        CriarPlacar();
        CriarLog();
        CriarAcoes();
        CriarAjuda();
        CriarFim();

        var d = E.Retangulo(raiz, "Dica", new Vector2(0.5f, 0), new Vector2(0, 112), new Vector2(700, 30));
        dica = E.Rotulo(d, "", 19, E.Forte, E.Ouro, TextAnchor.MiddleCenter, true);
        jogo.pontos.AoPontuar += AoPontuar;
    }

    // ---------- construção ----------

    void CriarFaixa()
    {
        faixaFundo = E.Painel(raiz, "Faixa", new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(330, 50));
        faixaLinha = E.Bloco(faixaFundo.transform, "Linha", new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(330 - 24, 3), E.Azul);
        faixaFase = E.Rotulo(faixaFundo.transform, "", 13, E.Forte, E.Ouro, TextAnchor.UpperCenter, 0, 4, 0, 0);
        faixaTurno = E.Rotulo(faixaFundo.transform, "", 24, E.Titulo, E.Texto, TextAnchor.LowerCenter, 0, 0, 0, 6);
    }

    void CriarCartoes()
    {
        var cab = E.Retangulo(raiz, "CabEsquadrao", new Vector2(0, 1), new Vector2(14, -12), new Vector2(250, 20));
        E.Rotulo(cab, (jogo.EquipeJogador != null ? jogo.EquipeJogador.nome : "Esquadrão").ToUpper(), 14, E.Titulo, E.Azul, TextAnchor.MiddleLeft);

        for (int i = 0; i < 4; i++)
        {
            int indice = i;
            var c = new Cartao();
            c.fundo = E.Painel(raiz, "Cartao" + i, new Vector2(0, 1), new Vector2(14, -34 - i * 70), new Vector2(250, 64));
            c.borda = c.fundo.transform.Find("Contorno").GetComponent<Image>();
            c.fundo.raycastTarget = true;
            var t = c.fundo.transform;
            c.faixa = E.Bloco(t, "Faixa", new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(4, 40), E.Azul);
            c.nome = E.Rotulo(t, "", 19, E.Titulo, E.Texto, TextAnchor.UpperLeft, 14, 5, 60, 0);
            c.info = E.Rotulo(t, "", 13, E.Normal, E.TextoFraco, TextAnchor.UpperLeft, 14, 27, 10, 0);
            c.estado = E.Rotulo(t, "", 12, E.Forte, E.Ouro, TextAnchor.UpperRight, 0, 27, 10, 0);
            for (int k = 0; k < 2; k++)
            {
                c.pa[k] = E.Bloco(t, "PA" + k, new Vector2(1, 1), new Vector2(-12 - k * 14, -10), new Vector2(11, 11), E.Ouro);
                c.pa[k].sprite = E.SpriteLosango;
            }
            c.vidaArea = E.Retangulo(t, "Vida", new Vector2(0, 0), new Vector2(14, 9), new Vector2(222, 7));
            var botao = c.fundo.gameObject.AddComponent<Button>();
            botao.targetGraphic = c.fundo;
            var cores = botao.colors;
            cores.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
            cores.colorMultiplier = 1.4f;
            botao.colors = cores;
            botao.onClick.AddListener(() => { if (indice < jogo.azuis.Count) jogo.Selecionar(jogo.azuis[indice]); });
            cartoes.Add(c);
        }
    }

    // Barra de vida em segmentos (um por ponto de vida), recriada quando o máximo muda.
    void Segmentos(Cartao c, int max)
    {
        if (c.segmentos.Count == max) return;
        foreach (var s in c.segmentos) Destroy(s.gameObject);
        c.segmentos.Clear();
        float esp = 2f, larg = (222f - esp * (max - 1)) / max;
        for (int i = 0; i < max; i++)
        {
            var s = E.Bloco(c.vidaArea, "S" + i, new Vector2(0, 0.5f), new Vector2(i * (larg + esp), 0), new Vector2(larg, 7), E.Azul);
            c.segmentos.Add(s);
        }
    }

    void CriarPlacar()
    {
        var p = E.Painel(raiz, "Pontos", new Vector2(0, 1), new Vector2(14, -320), new Vector2(250, 104)).transform;
        E.Rotulo(p, "PONTOS", 13, E.Forte, E.Ouro, TextAnchor.UpperLeft, 14, 8, 0, 0);
        placar = E.Rotulo(p, "0", 36, E.Titulo, E.Texto, TextAnchor.UpperRight, 0, 0, 14, 0);
        E.Bloco(p, "Linha", new Vector2(0, 1), new Vector2(14, -46), new Vector2(222, 1), E.Borda);
        feed = E.Rotulo(p, "", 13, E.Normal, E.Texto, TextAnchor.UpperLeft, 14, 52, 10, 0);

        var leg = E.Painel(raiz, "Legenda", new Vector2(0, 1), new Vector2(14, -430), new Vector2(250, 176));
        painelLegenda = leg.gameObject;
        E.Rotulo(leg.transform, "LEGENDA DE PONTOS", 13, E.Forte, E.Ouro, TextAnchor.UpperLeft, 14, 8, 0, 0);
        E.Tecla(leg.transform, "L", new Vector2(1, 1), new Vector2(-10, -8));
        var nomes = new System.Text.StringBuilder();
        var valores = new System.Text.StringBuilder();
        foreach (var (acao, pts) in Pontuacao.Legenda)
        {
            nomes.Append(acao).Append('\n');
            valores.Append($"<color={E.Hex(pts >= 0 ? E.Verde : E.Vermelho)}>{(pts >= 0 ? "+" : "")}{pts}</color>\n");
        }
        var tn = E.Rotulo(leg.transform, nomes.ToString(), 13, E.Normal, E.TextoFraco, TextAnchor.UpperLeft, 14, 32, 0, 0);
        tn.lineSpacing = 1.05f;
        var tv = E.Rotulo(leg.transform, valores.ToString(), 13, E.Forte, E.Texto, TextAnchor.UpperRight, 0, 32, 14, 0);
        tv.lineSpacing = 1.05f;
    }

    void CriarLog()
    {
        var p = E.Painel(raiz, "Log", new Vector2(1, 1), new Vector2(-14, -10), new Vector2(370, 178)).transform;
        E.Rotulo(p, "REGISTRO DE COMBATE", 13, E.Forte, E.Ouro, TextAnchor.UpperLeft, 14, 8, 0, 0);
        E.Bloco(p, "Linha", new Vector2(0, 1), new Vector2(14, -28), new Vector2(342, 1), E.Borda);
        logTexto = E.Rotulo(p, "", 14, E.Normal, E.Texto, TextAnchor.UpperLeft, 14, 34, 12, 8);
        logTexto.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    void CriarAcoes()
    {
        var barra = E.Painel(raiz, "Acoes", new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(780, 96));
        barra.raycastTarget = true;
        var t = barra.transform;
        infoNome = E.Rotulo(t, "", 18, E.Titulo, E.Texto, TextAnchor.UpperLeft, 16, 7, 0, 0);
        infoStats = E.Rotulo(t, "", 14, E.Normal, E.TextoFraco, TextAnchor.UpperRight, 0, 10, 16, 0);
        E.Bloco(t, "Linha", new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(748, 1), E.Borda);

        string[] nomes = { "MOVER", "ATIRAR", "VIGILÂNCIA", "DEFENDER", "CURAR", "FIM DO TURNO" };
        string[] teclas = { "1", "2", "3", "4", "5", "ENTER" };
        string[] chaves = { "mover", "atirar", "vigiar", "defender", "curar", "fim" };
        float x = 16;
        for (int i = 0; i < nomes.Length; i++)
        {
            string chave = chaves[i];
            float largura = i == 5 ? 158 : 110;
            var b = E.Botao(t, "Botao_" + chave, new Vector2(0, 0), new Vector2(x, 10), new Vector2(largura, 44), () => jogo.BotaoAcao(chave), out var fundo, out var borda);
            x += largura + 8;
            var tecla = E.Tecla(b.transform, teclas[i], new Vector2(0, 0.5f), new Vector2(9, 0), i == 5 ? 40 : 20);
            var txt = E.Rotulo(b.transform, nomes[i], 16, E.Titulo, E.Texto, TextAnchor.MiddleLeft, i == 5 ? 56 : 36, 0, 4, 0);
            botoes[chave] = (b, fundo, borda, txt, tecla);
        }
    }

    void CriarAjuda()
    {
        var p = E.Painel(raiz, "Ajuda", new Vector2(1, 0), new Vector2(-14, 12), new Vector2(214, 124)).transform;
        E.Rotulo(p, "CONTROLES", 13, E.Forte, E.Ouro, TextAnchor.UpperLeft, 12, 7, 0, 0);
        E.Rotulo(p, "Clique · Tab\nWASD · setas\nQ / E\nRoda do mouse\nBotão dir. · Esc\nF5", 13, E.Forte, E.Texto, TextAnchor.UpperLeft, 12, 27, 0, 0);
        E.Rotulo(p, "escolher soldado\nmover câmera\ngirar câmera\nzoom\ncancelar\nnovo mapa", 13, E.Normal, E.TextoFraco, TextAnchor.UpperRight, 0, 27, 12, 0);
    }

    void CriarFim()
    {
        var fundo = E.Bloco(raiz, "Fim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000), new Color(0.02f, 0.025f, 0.035f, 0.72f));
        telaFim = fundo.gameObject;
        var p = E.Painel(fundo.transform, "Quadro", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 300), E.FundoClaro).transform;
        fimTitulo = E.Rotulo(p, "", 64, E.Titulo, E.Texto, TextAnchor.UpperCenter, 0, 18, 0, 0, true);
        fimLinha = E.Bloco(p, "Linha", new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(380, 3), E.Ouro);
        fimPontos = E.Rotulo(p, "", 30, E.Forte, E.Ouro, TextAnchor.UpperCenter, 0, 116, 0, 0);
        fimSub = E.Rotulo(p, "", 16, E.Normal, E.TextoFraco, TextAnchor.UpperCenter, 0, 160, 0, 0);
        var b1 = E.Botao(p, "JogarDeNovo", new Vector2(0.5f, 0), new Vector2(-104, 26), new Vector2(196, 46), () => jogo.Reiniciar(), out var f1, out var bo1);
        f1.color = E.OuroEscuro; bo1.color = E.Ouro;
        E.Tecla(b1.transform, "R", new Vector2(0, 0.5f), new Vector2(12, 0));
        E.Rotulo(b1.transform, "JOGAR DE NOVO", 18, E.Titulo, E.Texto, TextAnchor.MiddleLeft, 42, 0, 4, 0);
        var b2 = E.Botao(p, "Menu", new Vector2(0.5f, 0), new Vector2(104, 26), new Vector2(196, 46), () => jogo.VoltarAoMenu(), out _, out _);
        E.Tecla(b2.transform, "M", new Vector2(0, 0.5f), new Vector2(12, 0));
        E.Rotulo(b2.transform, "MENU", 18, E.Titulo, E.Texto, TextAnchor.MiddleLeft, 42, 0, 4, 0);
        telaFim.SetActive(false);
    }

    public void CriarBarra(Unidade u)
    {
        var cor = u.azul ? E.Azul : E.Vermelho;
        var rt = E.Retangulo(raiz, "Barra_" + u.nome, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 7));
        rt.SetAsFirstSibling();
        var fundo = rt.gameObject.AddComponent<Image>();
        fundo.color = new Color(0, 0, 0, 0.75f);
        fundo.raycastTarget = false;
        var vida = E.Bloco(rt, "Vida", new Vector2(0, 0.5f), new Vector2(1, 0), new Vector2(62, 5), cor);
        var nome = E.Retangulo(rt, "Nome", new Vector2(0.5f, 1), new Vector2(0, 16), new Vector2(160, 16));
        var t = E.Rotulo(nome, "", 14, E.Forte, E.Texto, TextAnchor.LowerCenter, true);
        barras[u] = new Barra { rt = rt, vida = vida, fundo = fundo, texto = t };
    }

    // ---------- chamadas do jogo ----------

    public void Banner(string texto, bool jogador)
    {
        // "TURNO 3 — SUA VEZ" vira fase pequena em cima e título grande embaixo
        int i = texto.IndexOf(" — ");
        faixaFase.text = i >= 0 ? texto.Substring(0, i) : (jogador ? "" : "TURNO " + jogo.turno);
        faixaTurno.text = i >= 0 ? texto.Substring(i + 3) : texto;
        var cor = jogador ? E.Azul : E.Vermelho;
        faixaLinha.color = cor;
        faixaFundo.color = Color.Lerp(E.Fundo, E.ComAlfa(cor * 0.5f, 0.95f), 0.35f);
    }

    public void Log(string linha)
    {
        linhasLog.Add(linha);
        if (linhasLog.Count > 7) linhasLog.RemoveAt(0);
        // linhas mais antigas ficam mais apagadas; cabeçalhos de turno em dourado
        var sb = new System.Text.StringBuilder();
        for (int k = 0; k < linhasLog.Count; k++)
        {
            float a = 0.45f + 0.55f * (k + 1) / linhasLog.Count;
            string l = linhasLog[k];
            bool cab = l.StartsWith("—");
            var cor = cab ? E.Ouro : E.Texto;
            sb.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(E.ComAlfa(cor, a))}>{l.Trim('—', ' ')}</color>");
            if (k < linhasLog.Count - 1) sb.Append('\n');
        }
        logTexto.text = sb.ToString();
        Debug.Log("[Log] " + (Gravador.quadroAtual >= 0 ? $"q{Gravador.quadroAtual} " : "") + linha);
    }

    public void Dica(string texto) => dica.text = texto;

    public void Popup(Vector3 mundo, string texto, Color cor, float altura = 0f, int tamanho = 26)
    {
        var rt = E.Retangulo(raiz, "Popup", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 44));
        E.Rotulo(rt, texto, tamanho + 4, E.Titulo, cor, TextAnchor.MiddleCenter, true);
        popups.Add((rt, mundo + Vector3.up * altura, 0f));
    }

    void AoPontuar(int pts, string motivo, Vector3 onde)
    {
        string txt = (pts >= 0 ? "+" : "") + pts;
        linhasFeed.Insert(0, $"<color={E.Hex(pts >= 0 ? E.Verde : E.Vermelho)}>{txt}</color>   {motivo}");
        if (linhasFeed.Count > 3) linhasFeed.RemoveAt(3);
        feed.text = string.Join("\n", linhasFeed);
        pulso = 1f;
        Popup(onde, txt + " PTS", pts >= 0 ? E.Ouro : E.Vermelho, 0.75f, 18);
    }

    public void Fim(bool vitoria)
    {
        telaFim.SetActive(true);
        telaFim.transform.SetAsLastSibling();
        fimTitulo.text = vitoria ? "VITÓRIA" : "DERROTA";
        fimTitulo.color = vitoria ? E.Ouro : E.Vermelho;
        fimLinha.color = vitoria ? E.Ouro : E.Vermelho;
        fimPontos.text = $"{jogo.pontos.Total} PONTOS";
        fimSub.text = vitoria ? $"{jogo.EquipeInimiga.nome} eliminados no turno {jogo.turno}" : $"{jogo.EquipeJogador.nome} caíram no turno {jogo.turno}";
    }

    public void MostrarAlvos(List<(Unidade alvo, int chance)> alvos)
    {
        while (rotulosAlvo.Count < alvos.Count)
        {
            var img = E.Painel(raiz, "Alvo", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(62, 28));
            var txt = E.Rotulo(img.transform, "", 18, E.Titulo, E.Texto, TextAnchor.MiddleCenter);
            rotulosAlvo.Add((img.rectTransform, img, txt));
        }
        for (int i = 0; i < rotulosAlvo.Count; i++)
        {
            var r = rotulosAlvo[i];
            bool ativo = i < alvos.Count;
            r.rt.gameObject.SetActive(ativo);
            if (!ativo) continue;
            int ch = alvos[i].chance;
            var cor = ch >= 65 ? E.Verde : ch >= 40 ? E.Ouro : E.Vermelho;
            r.txt.text = ch + "%";
            r.txt.color = cor;
            r.fundo.transform.Find("Contorno").GetComponent<Image>().color = E.ComAlfa(cor, 0.8f);
            Posicionar(r.rt, alvos[i].alvo.transform.position + Vector3.up * (alvos[i].alvo.altura + 0.75f));
        }
    }

    // ---------- atualização ----------

    void LateUpdate()
    {
        if (jogo == null) return;

        if (Input.GetKeyDown(KeyCode.L)) painelLegenda.SetActive(!painelLegenda.activeSelf);

        // placar animado: o número sobe até o total e pulsa quando muda
        placarMostrado = Mathf.MoveTowards(placarMostrado, jogo.pontos.Total, Mathf.Max(60f, Mathf.Abs(jogo.pontos.Total - placarMostrado) * 4f) * Time.unscaledDeltaTime);
        placar.text = Mathf.RoundToInt(placarMostrado).ToString();
        pulso = Mathf.MoveTowards(pulso, 0, Time.unscaledDeltaTime * 2.5f);
        placar.rectTransform.localScale = Vector3.one * (1f + pulso * 0.2f);
        placar.color = Color.Lerp(E.Texto, E.Ouro, pulso);

        AtualizarCartoes();
        AtualizarAcoes();
        AtualizarBarras();

        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.t += Time.deltaTime;
            popups[i] = p;
            Posicionar(p.rt, p.mundo + Vector3.up * (2.1f + p.t * 0.8f));
            p.rt.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(p.t * 6f));
            if (p.t > 1.6f)
            {
                Destroy(p.rt.gameObject);
                popups.RemoveAt(i);
            }
        }
    }

    void AtualizarCartoes()
    {
        for (int i = 0; i < cartoes.Count; i++)
        {
            var c = cartoes[i];
            var u = i < jogo.azuis.Count ? jogo.azuis[i] : null;
            c.fundo.gameObject.SetActive(u != null);
            if (u == null) continue;
            bool sel = u == jogo.selecionada;
            Segmentos(c, u.hpMax);
            for (int k = 0; k < c.segmentos.Count; k++)
                c.segmentos[k].color = k < u.hp ? (u.hp <= u.hpMax / 3 ? E.Vermelho : E.Azul) : new Color(1, 1, 1, 0.08f);
            c.nome.text = u.nome.ToUpper();
            c.nome.color = u.Viva ? E.Texto : E.TextoFraco;
            c.info.text = u.Viva ? $"{u.classe}  ·  HP {u.hp}/{u.hpMax}" : "<color=#e65040>ABATIDO</color>";
            c.estado.text = !u.Viva ? "" : u.vigilancia ? "VIGILÂNCIA" : u.defendendo ? "<color=#8fb8ff>DEFENDENDO</color>" : "";
            for (int k = 0; k < 2; k++)
            {
                c.pa[k].gameObject.SetActive(u.Viva);
                c.pa[k].color = k < u.pa ? E.Ouro : new Color(1, 1, 1, 0.15f);
            }
            c.faixa.color = !u.Viva ? E.Desligado : sel ? E.Ouro : E.Azul;
            c.fundo.color = !u.Viva ? new Color(0.12f, 0.05f, 0.05f, 0.85f) : sel ? new Color(0.12f, 0.15f, 0.2f, 0.96f) : E.Fundo;
            c.borda.color = sel ? E.ComAlfa(E.Ouro, 0.9f) : E.Borda;
        }
    }

    void AtualizarAcoes()
    {
        var s = jogo.selecionada;
        bool podeAgir = jogo.EsperandoJogador && s != null && s.Viva && s.pa > 0;
        if (s != null && s.Viva)
        {
            infoNome.text = $"{s.nome.ToUpper()}  <color={E.Hex(E.TextoFraco)}><size=14>{s.classe}</size></color>";
            string branco = E.Hex(E.Texto);
            infoStats.text = $"HP <color={branco}>{s.hp}/{s.hpMax}</color>    MIRA <color={branco}>{s.mira}</color>    DANO <color={branco}>{s.danoMin}-{s.danoMax}</color>    AÇÕES <color={branco}>{s.pa}/2</color>"
                + (s.medico ? $"    CURAS <color={branco}>{s.curas}</color>" : "");
        }
        else infoNome.text = infoStats.text = "";

        foreach (var kv in botoes)
        {
            bool ok = kv.Key switch
            {
                "fim" => jogo.EsperandoJogador,
                "curar" => podeAgir && s.medico && s.curas > 0,
                _ => podeAgir
            };
            kv.Value.b.interactable = ok;
            bool ativo = podeAgir && ((kv.Key == "mover" && jogo.modo == Jogo.Modo.Mover) || (kv.Key == "atirar" && jogo.modo == Jogo.Modo.Atirar) ||
                         (kv.Key == "curar" && jogo.modo == Jogo.Modo.Curar));
            kv.Value.fundo.color = !ok ? new Color(0.07f, 0.075f, 0.09f, 0.9f) : ativo ? E.OuroEscuro : E.FundoClaro;
            kv.Value.borda.color = ativo ? E.Ouro : ok ? new Color(1, 1, 1, 0.16f) : E.Borda;
            kv.Value.txt.color = ok ? E.Texto : E.Desligado;
            kv.Value.tecla.color = ok ? new Color(0, 0, 0, 0.45f) : new Color(0, 0, 0, 0.2f);
            kv.Value.b.gameObject.SetActive(kv.Key != "curar" || (s != null && s.medico));
        }
    }

    void AtualizarBarras()
    {
        foreach (var kv in barras)
        {
            var u = kv.Key;
            bool visivel = u != null && u.gameObject.activeInHierarchy && u.Viva;
            kv.Value.rt.gameObject.SetActive(visivel);
            if (!visivel) continue;
            Posicionar(kv.Value.rt, u.transform.position + Vector3.up * (u.altura + 0.3f));
            kv.Value.vida.rectTransform.sizeDelta = new Vector2(62f * u.hp / u.hpMax, 5);
            bool sel = u == jogo.selecionada;
            string etiqueta = (u.vigilancia ? $"<color={E.Hex(E.Ouro)}>[V] </color>" : "") + (u.defendendo ? "<color=#8fb8ff>[D] </color>" : "");
            kv.Value.texto.text = etiqueta + u.nome;
            kv.Value.texto.color = sel ? E.Ouro : E.Texto;
        }
    }

    void Posicionar(RectTransform rt, Vector3 mundo)
    {
        var tela = cam.WorldToScreenPoint(mundo);
        bool frente = tela.z > 0;
        rt.gameObject.SetActive(rt.gameObject.activeSelf && frente);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, tela, cam, out var local);
        rt.anchoredPosition = local;
    }
}
