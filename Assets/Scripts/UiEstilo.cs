using UnityEngine;
using UnityEngine.UI;

// Estilo visual único da interface (HUD e menu): paleta, fontes (Barlow Condensed, OFL)
// e painéis com cantos chanfrados gerados em código (9-slice), no clima "militar sombrio".
public static class UiEstilo
{
    // paleta
    public static readonly Color Fundo = new(0.055f, 0.065f, 0.08f, 0.93f);
    public static readonly Color FundoClaro = new(0.11f, 0.125f, 0.15f, 0.96f);
    public static readonly Color Borda = new(1f, 1f, 1f, 0.09f);
    public static readonly Color Ouro = new(0.9f, 0.72f, 0.33f);
    public static readonly Color OuroEscuro = new(0.45f, 0.34f, 0.12f);
    public static readonly Color Azul = new(0.32f, 0.6f, 1f);
    public static readonly Color Vermelho = new(0.92f, 0.3f, 0.25f);
    public static readonly Color Verde = new(0.45f, 0.85f, 0.45f);
    public static readonly Color Texto = new(0.91f, 0.93f, 0.95f);
    public static readonly Color TextoFraco = new(0.55f, 0.6f, 0.68f);
    public static readonly Color Desligado = new(0.32f, 0.34f, 0.38f);

    static Font fTitulo, fForte, fNormal;
    // (comparação com "== null" da Unity: objetos destruídos na troca de cena contam como nulos)
    public static Font Titulo => fTitulo != null ? fTitulo : fTitulo = Carregar("BarlowCondensed-Bold");
    public static Font Forte => fForte != null ? fForte : fForte = Carregar("BarlowCondensed-SemiBold");
    public static Font Normal => fNormal != null ? fNormal : fNormal = Carregar("BarlowCondensed-Medium");

    static Font Carregar(string nome)
    {
        var f = Resources.Load<Font>("Fontes/" + nome);
        if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        f.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        return f;
    }

    // ---------- sprites ----------
    static Sprite sPainel, sContorno, sLosango;

    // retângulo com os cantos superior-esquerdo e inferior-direito cortados
    public static Sprite SpritePainel => sPainel != null ? sPainel : sPainel = Chanfrado(false);
    public static Sprite SpriteContorno => sContorno != null ? sContorno : sContorno = Chanfrado(true);
    public static Sprite SpriteLosango => sLosango != null ? sLosango : sLosango = CriarLosango();

    // texturas criadas em código sobrevivem ao descarregamento de assets na troca de cena
    static Sprite Fixo(Texture2D tex, Sprite s)
    {
        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
        s.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return s;
    }

    static Sprite Chanfrado(bool soContorno)
    {
        const int t = 32, c = 9;   // tamanho e corte do canto
        var tex = new Texture2D(t, t, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[t * t];
        for (int y = 0; y < t; y++)
            for (int x = 0; x < t; x++)
            {
                // distância (em px) até a borda da forma, contando os cortes diagonais
                float dEsq = x + 0.5f, dDir = t - x - 0.5f, dBaixo = y + 0.5f, dCima = t - y - 0.5f;
                float d = Mathf.Min(Mathf.Min(dEsq, dDir), Mathf.Min(dBaixo, dCima));
                float diagSE = (dEsq + dCima - c) / 1.4142f;     // canto superior-esquerdo
                float diagID = (dDir + dBaixo - c) / 1.4142f;    // canto inferior-direito
                d = Mathf.Min(d, Mathf.Min(diagSE, diagID));
                float a = Mathf.Clamp01(d);                       // antisserrilhado de 1px
                if (soContorno) a *= 1f - Mathf.Clamp01(d - 1.2f);
                px[y * t + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Fixo(tex, Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12)));
    }

    static Sprite CriarLosango()
    {
        const int t = 16;
        var tex = new Texture2D(t, t, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < t; y++)
            for (int x = 0; x < t; x++)
            {
                float d = (t / 2f - 0.5f) - (Mathf.Abs(x - t / 2f + 0.5f) + Mathf.Abs(y - t / 2f + 0.5f));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(d + 0.5f)));
            }
        tex.Apply();
        return Fixo(tex, Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f)));
    }

    // ---------- construção ----------

    public static RectTransform Retangulo(Transform pai, string nome, Vector2 ancora, Vector2 pos, Vector2 tamanho)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(pai, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = ancora;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
        return rt;
    }

    public static void Esticar(RectTransform rt, float margem = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margem, margem);
        rt.offsetMax = new Vector2(-margem, -margem);
    }

    // Painel chanfrado com contorno fino. Retorna a imagem de fundo.
    public static Image Painel(Transform pai, string nome, Vector2 ancora, Vector2 pos, Vector2 tamanho, Color? cor = null, Color? borda = null)
    {
        var rt = Retangulo(pai, nome, ancora, pos, tamanho);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = SpritePainel;
        img.type = Image.Type.Sliced;
        img.color = cor ?? Fundo;
        img.raycastTarget = false;
        var c = Retangulo(rt, "Contorno", Vector2.zero, Vector2.zero, Vector2.zero);
        Esticar(c);
        var ci = c.gameObject.AddComponent<Image>();
        ci.sprite = SpriteContorno;
        ci.type = Image.Type.Sliced;
        ci.color = borda ?? Borda;
        ci.raycastTarget = false;
        return img;
    }

    // Retângulo simples (barras, faixas, linhas).
    public static Image Bloco(Transform pai, string nome, Vector2 ancora, Vector2 pos, Vector2 tamanho, Color cor)
    {
        var rt = Retangulo(pai, nome, ancora, pos, tamanho);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = cor;
        img.raycastTarget = false;
        return img;
    }

    public static Text Rotulo(Transform pai, string texto, int tamanho, Font fonte, Color cor, TextAnchor alinhamento = TextAnchor.UpperLeft, bool sombra = false)
    {
        var rt = Retangulo(pai, "Texto", new Vector2(0, 1), Vector2.zero, Vector2.zero);
        Esticar(rt);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fonte;
        t.text = texto;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = alinhamento;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        if (sombra)
        {
            var s = rt.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.85f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return t;
    }

    // Posiciona um texto dentro do pai com margens (esq, cima, dir, baixo).
    public static Text Rotulo(Transform pai, string texto, int tamanho, Font fonte, Color cor, TextAnchor alinhamento, float esq, float cima, float dir, float baixo, bool sombra = false)
    {
        var t = Rotulo(pai, texto, tamanho, fonte, cor, alinhamento, sombra);
        t.rectTransform.offsetMin = new Vector2(esq, baixo);
        t.rectTransform.offsetMax = new Vector2(-dir, -cima);
        return t;
    }

    // "Selo" de tecla de atalho: quadradinho com a tecla.
    public static Image Tecla(Transform pai, string tecla, Vector2 ancora, Vector2 pos, float largura = 20)
    {
        var img = Painel(pai, "Tecla", ancora, pos, new Vector2(largura, 20), new Color(0, 0, 0, 0.45f), new Color(1, 1, 1, 0.25f));
        Rotulo(img.transform, tecla, 13, Forte, Texto, TextAnchor.MiddleCenter);
        return img;
    }

    // Botão chanfrado; as cores de estado são controladas por quem usa.
    public static Button Botao(Transform pai, string nome, Vector2 ancora, Vector2 pos, Vector2 tamanho, System.Action aoClicar, out Image fundo, out Image borda)
    {
        fundo = Painel(pai, nome, ancora, pos, tamanho, FundoClaro);
        fundo.raycastTarget = true;
        borda = fundo.transform.Find("Contorno").GetComponent<Image>();
        var b = fundo.gameObject.AddComponent<Button>();
        b.targetGraphic = fundo;
        var cores = b.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1.35f, 1.35f, 1.35f);
        cores.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cores.selectedColor = Color.white;
        cores.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
        cores.colorMultiplier = 1.5f;
        b.colors = cores;
        b.onClick.AddListener(() => aoClicar());
        return b;
    }

    public static Color ComAlfa(Color c, float a) { c.a = a; return c; }
    public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
}
