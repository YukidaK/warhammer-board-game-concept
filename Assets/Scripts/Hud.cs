using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Interface montada por código: cartões da equipe, barra de ações, faixa de turno, log,
// barras de vida sobre as unidades, rótulos de chance de acerto e telas de fim.
public class Hud : MonoBehaviour
{
    static readonly Color Fundo = new(0.05f, 0.07f, 0.1f, 0.82f);
    static readonly Color AzulUi = new(0.25f, 0.55f, 1f);
    static readonly Color VermelhoUi = new(0.95f, 0.25f, 0.2f);
    static readonly Color Amarelo = new(1f, 0.82f, 0.3f);

    Jogo jogo;
    Camera cam;
    RectTransform raiz;
    Font fonte;

    Text faixaTurno, logTexto, dica, infoSel;
    Image faixaFundo;
    readonly List<Cartao> cartoes = new();
    readonly Dictionary<Unidade, Barra> barras = new();
    readonly List<Text> rotulosAlvo = new();
    readonly List<(RectTransform rt, Vector3 mundo, float t)> popups = new();
    readonly List<string> linhasLog = new();
    readonly Dictionary<string, (Button b, Image img, Text txt)> botoes = new();
    GameObject telaFim;
    Text textoFim;

    class Cartao { public Unidade u; public Image fundo, vida; public Text nome, info; }
    class Barra { public RectTransform rt; public Image vida; public Text texto; }

    public void Criar(Jogo jogo, Camera cam)
    {
        this.jogo = jogo;
        this.cam = cam;
        fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

        // faixa de turno
        var faixa = Painel(raiz, "Faixa", new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(380, 44), Fundo);
        faixaFundo = faixa.GetComponent<Image>();
        faixaTurno = Texto(faixa, "", 22, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);

        // cartões da equipe
        for (int i = 0; i < 4; i++)
        {
            int indice = i;
            var p = Painel(raiz, "Cartao" + i, new Vector2(0, 1), new Vector2(10, -10 - i * 66), new Vector2(230, 60), Fundo);
            var c = new Cartao { fundo = p.GetComponent<Image>() };
            c.nome = Texto(p, "", 16, TextAnchor.UpperLeft, Color.white, FontStyle.Bold, new Vector2(10, -5), new Vector2(-10, 0));
            c.info = Texto(p, "", 12, TextAnchor.UpperLeft, new Color(0.8f, 0.85f, 0.9f), FontStyle.Normal, new Vector2(10, -25), new Vector2(-10, 0));
            var fundoVida = Painel(p, "VidaFundo", new Vector2(0, 0), new Vector2(10, 7), new Vector2(210, 8), new Color(0.2f, 0.2f, 0.2f));
            var vida = Painel(fundoVida, "Vida", new Vector2(0, 0.5f), Vector2.zero, new Vector2(210, 8), AzulUi);
            vida.pivot = new Vector2(0, 0.5f);
            c.vida = vida.GetComponent<Image>();
            var botao = p.gameObject.AddComponent<Button>();
            botao.targetGraphic = c.fundo;
            c.fundo.raycastTarget = true;
            botao.onClick.AddListener(() => { if (indice < jogo.azuis.Count) jogo.Selecionar(jogo.azuis[indice]); });
            cartoes.Add(c);
        }

        // barra de ações
        var barra = Painel(raiz, "Acoes", new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(760, 92), Fundo);
        barra.GetComponent<Image>().raycastTarget = true;
        infoSel = Texto(barra, "", 15, TextAnchor.UpperCenter, Color.white, FontStyle.Bold, new Vector2(0, -6), Vector2.zero);
        string[] nomes = { "1  Mover", "2  Atirar", "3  Vigilância", "4  Defender", "5  Curar", "Enter  Fim do turno" };
        string[] chaves = { "mover", "atirar", "vigiar", "defender", "curar", "fim" };
        float x = -305;
        for (int i = 0; i < nomes.Length; i++)
        {
            string chave = chaves[i];
            float largura = i == 5 ? 150 : 108;
            var b = Painel(barra, "Botao_" + chave, new Vector2(0.5f, 0), new Vector2(x + largura / 2 - 54, 10), new Vector2(largura, 42), new Color(0.15f, 0.22f, 0.32f));
            x += largura + 8;
            var img = b.GetComponent<Image>();
            img.raycastTarget = true;
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => jogo.BotaoAcao(chave));
            var t = Texto(b, nomes[i], 14, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            botoes[chave] = (btn, img, t);
        }

        // log de combate
        var log = Painel(raiz, "Log", new Vector2(1, 1), new Vector2(-10, -10), new Vector2(360, 150), Fundo);
        logTexto = Texto(log, "", 13, TextAnchor.UpperLeft, new Color(0.88f, 0.9f, 0.92f), FontStyle.Normal, new Vector2(10, -8), new Vector2(-10, 8));

        // dica (tooltip)
        var d = Painel(raiz, "Dica", new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(560, 34), new Color(0, 0, 0, 0));
        dica = Texto(d, "", 17, TextAnchor.MiddleCenter, Amarelo, FontStyle.Bold);
        dica.gameObject.AddComponent<Outline>().effectColor = Color.black;

        // ajuda
        var ajuda = Painel(raiz, "Ajuda", new Vector2(1, 0), new Vector2(-10, 10), new Vector2(230, 92), Fundo);
        Texto(ajuda, "Clique/Tab: escolher soldado\nWASD/setas: mover câmera\nQ/E: girar  ·  Roda: zoom\nBotão dir./Esc: cancelar\nF5: novo mapa", 12,
            TextAnchor.UpperLeft, new Color(0.75f, 0.8f, 0.85f), FontStyle.Normal, new Vector2(10, -6), new Vector2(-6, 4));

        // tela de fim
        var fim = Painel(raiz, "Fim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2400, 1400), new Color(0, 0, 0, 0.6f));
        telaFim = fim.gameObject;
        textoFim = Texto(fim, "", 56, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
        textoFim.gameObject.AddComponent<Outline>().effectColor = Color.black;
        telaFim.SetActive(false);
    }

    public void CriarBarra(Unidade u)
    {
        var p = Painel(raiz, "Barra_" + u.nome, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 9), new Color(0.1f, 0.1f, 0.1f, 0.85f));
        p.SetAsFirstSibling();
        var vida = Painel(p, "Vida", new Vector2(0, 0.5f), new Vector2(1, 0), new Vector2(68, 7), u.azul ? AzulUi : VermelhoUi);
        vida.pivot = new Vector2(0, 0.5f);
        var t = Texto(p, "", 12, TextAnchor.LowerCenter, Color.white, FontStyle.Bold, new Vector2(-40, 0), new Vector2(40, 18));
        t.rectTransform.anchorMin = new Vector2(0, 1);
        t.rectTransform.anchorMax = new Vector2(1, 1);
        t.rectTransform.offsetMin = new Vector2(-40, 0);
        t.rectTransform.offsetMax = new Vector2(40, 18);
        t.gameObject.AddComponent<Outline>().effectColor = Color.black;
        barras[u] = new Barra { rt = p, vida = vida.GetComponent<Image>(), texto = t };
    }

    public void Banner(string texto, bool jogador)
    {
        faixaTurno.text = texto;
        faixaFundo.color = jogador ? new Color(0.08f, 0.2f, 0.45f, 0.9f) : new Color(0.45f, 0.08f, 0.08f, 0.9f);
    }

    public void Log(string linha)
    {
        linhasLog.Add(linha);
        if (linhasLog.Count > 8) linhasLog.RemoveAt(0);
        logTexto.text = string.Join("\n", linhasLog);
        Debug.Log("[Log] " + linha);
    }

    public void Dica(string texto) => dica.text = texto;

    public void Popup(Vector3 mundo, string texto, Color cor)
    {
        var t = Texto(raiz, texto, 26, TextAnchor.MiddleCenter, cor, FontStyle.Bold);
        t.gameObject.AddComponent<Outline>().effectColor = Color.black;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 40);
        popups.Add((rt, mundo, 0f));
    }

    public void Fim(bool vitoria)
    {
        telaFim.SetActive(true);
        textoFim.text = vitoria ? "VITÓRIA\n<size=24>Todos os inimigos foram eliminados. R para jogar de novo</size>"
                                : "DERROTA\n<size=24>A equipe azul caiu. R para tentar de novo</size>";
        textoFim.color = vitoria ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.5f, 0.45f);
    }

    public void MostrarAlvos(List<(Unidade alvo, int chance)> alvos)
    {
        while (rotulosAlvo.Count < alvos.Count)
        {
            var t = Texto(raiz, "", 18, TextAnchor.MiddleCenter, Amarelo, FontStyle.Bold);
            t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            t.rectTransform.sizeDelta = new Vector2(80, 26);
            t.gameObject.AddComponent<Outline>().effectColor = Color.black;
            rotulosAlvo.Add(t);
        }
        for (int i = 0; i < rotulosAlvo.Count; i++)
        {
            bool ativo = i < alvos.Count;
            rotulosAlvo[i].gameObject.SetActive(ativo);
            if (!ativo) continue;
            rotulosAlvo[i].text = alvos[i].chance + "%";
            Posicionar(rotulosAlvo[i].rectTransform, alvos[i].alvo.transform.position + Vector3.up * 2.0f);
        }
    }

    void LateUpdate()
    {
        if (jogo == null) return;

        // cartões
        for (int i = 0; i < cartoes.Count; i++)
        {
            var c = cartoes[i];
            var u = i < jogo.azuis.Count ? jogo.azuis[i] : null;
            c.fundo.gameObject.SetActive(u != null);
            if (u == null) continue;
            bool sel = u == jogo.selecionada;
            string pas = u.Viva ? (u.pa > 0 ? $"<color=#8fb8ff>{u.pa} ações</color>" : "<color=#7a8090>sem ações</color>") : "";
            c.nome.text = $"{u.nome}   <size=12>{pas}</size>";
            string extra = u.vigilancia ? "  · VIGILÂNCIA" : u.defendendo ? "  · DEFENDENDO" : "";
            c.info.text = u.Viva ? $"{u.classe}   HP {u.hp}/{u.hpMax}{extra}" : "<color=#ff7060>ABATIDO</color>";
            c.vida.rectTransform.sizeDelta = new Vector2(210f * u.hp / u.hpMax, 8);
            c.fundo.color = !u.Viva ? new Color(0.2f, 0.05f, 0.05f, 0.8f) : sel ? new Color(0.12f, 0.3f, 0.6f, 0.92f) : Fundo;
        }

        // barra de ações
        var s = jogo.selecionada;
        bool podeAgir = jogo.EsperandoJogador && s != null && s.Viva && s.pa > 0;
        infoSel.text = s != null && s.Viva
            ? $"{s.nome} — {s.classe}   ·   HP {s.hp}/{s.hpMax}   ·   Mira {s.mira}   ·   Dano {s.danoMin}-{s.danoMax}   ·   Ações {s.pa}/2" + (s.medico ? $"   ·   Curas {s.curas}" : "")
            : "";
        foreach (var kv in botoes)
        {
            bool ok = kv.Key switch
            {
                "fim" => jogo.EsperandoJogador,
                "curar" => podeAgir && s.medico && s.curas > 0,
                _ => podeAgir
            };
            kv.Value.b.interactable = ok;
            bool ativo = (kv.Key == "mover" && jogo.modo == Jogo.Modo.Mover) || (kv.Key == "atirar" && jogo.modo == Jogo.Modo.Atirar) ||
                         (kv.Key == "curar" && jogo.modo == Jogo.Modo.Curar);
            kv.Value.img.color = !ok ? new Color(0.12f, 0.13f, 0.15f) : ativo && podeAgir ? new Color(0.2f, 0.45f, 0.85f) : new Color(0.15f, 0.22f, 0.32f);
            kv.Value.txt.color = ok ? Color.white : new Color(0.45f, 0.45f, 0.45f);
            kv.Value.b.gameObject.SetActive(kv.Key != "curar" || (s != null && s.medico));
        }

        // barras de vida sobre as unidades
        foreach (var kv in barras)
        {
            var u = kv.Key;
            bool visivel = u != null && u.gameObject.activeInHierarchy && u.Viva;
            kv.Value.rt.gameObject.SetActive(visivel);
            if (!visivel) continue;
            Posicionar(kv.Value.rt, u.transform.position + Vector3.up * 1.75f);
            kv.Value.vida.rectTransform.sizeDelta = new Vector2(68f * u.hp / u.hpMax, 7);
            kv.Value.texto.text = (u.vigilancia ? "<color=#ffd24d>[V] </color>" : "") + (u.defendendo ? "<color=#8fb8ff>[D] </color>" : "") + u.nome;
        }

        // números que sobem
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.t += Time.deltaTime;
            popups[i] = p;
            Posicionar(p.rt, p.mundo + Vector3.up * (2.1f + p.t * 0.8f));
            if (p.t > 1.6f)
            {
                Destroy(p.rt.gameObject);
                popups.RemoveAt(i);
            }
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

    // --- construção ---

    RectTransform Painel(RectTransform pai, string nome, Vector2 ancora, Vector2 pos, Vector2 tamanho, Color cor)
    {
        var go = new GameObject(nome, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        var img = go.GetComponent<Image>();
        img.color = cor;
        img.raycastTarget = false;
        return rt;
    }

    Text Texto(RectTransform pai, string texto, int tamanho, TextAnchor alinhamento, Color cor, FontStyle estilo,
        Vector2? min = null, Vector2? max = null)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min.HasValue ? new Vector2(min.Value.x, 0) : Vector2.zero;
        rt.offsetMax = max.HasValue ? new Vector2(max.Value.x, min.HasValue ? min.Value.y : 0) : Vector2.zero;
        var t = go.GetComponent<Text>();
        t.font = fonte;
        t.text = texto;
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.color = cor;
        t.fontStyle = estilo;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }
}
