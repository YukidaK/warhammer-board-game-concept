using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// Recursos gerados por código: texturas procedurais, sprites da explosão e sua animação,
// e os prefabs (cenário, unidade, projétil, partículas, explosão e halo).
public static partial class MontarJogo
{
    // ---------- texturas ----------

    static Dictionary<string, Texture2D> GerarTexturas()
    {
        var r = new Dictionary<string, Texture2D>
        {
            ["Concreto"] = SalvarTextura("Concreto", 256, Concreto),
            ["Pedra"] = SalvarTextura("Pedra", 256, Pedra),
            ["Madeira"] = SalvarTextura("Madeira", 256, Madeira),
            ["Saco"] = SalvarTextura("Saco", 256, Saco),
            ["Terra"] = SalvarTextura("Terra", 256, Terra),
            ["Rocha"] = SalvarTextura("Rocha", 128, Rocha),
            ["Metal"] = SalvarTextura("Metal", 128, Metal),
            ["Particula"] = SalvarTextura("Particula", 64, Particula, true),
            ["Brilho"] = SalvarTextura("Brilho", 128, Brilho, true),
        };
        return r;
    }

    static float Fbm(float x, float y, int oitavas = 4)
    {
        float v = 0, a = 0.5f, f = 1;
        for (int i = 0; i < oitavas; i++)
        {
            v += a * Mathf.PerlinNoise(x * f + 31.7f * i, y * f + 17.3f * i);
            a *= 0.5f;
            f *= 2f;
        }
        return v;
    }

    static float Hash(int x, int y) => Mathf.Abs(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f) % 1f;

    static Color Concreto(float u, float v, int x, int y)
    {
        float n = Fbm(u * 6, v * 6) * 0.35f + Hash(x, y) * 0.08f;
        float rachadura = Mathf.Abs(Mathf.PerlinNoise(u * 3.1f, v * 3.1f) - 0.5f) < 0.012f ? -0.18f : 0f;
        float g = 0.62f + n - 0.18f + rachadura;
        // borda da placa
        if (x < 3 || y < 3 || x > 252 || y > 252) g *= 0.7f;
        return new Color(g, g * 0.98f, g * 0.95f);
    }

    static Color Pedra(float u, float v, int x, int y)
    {
        // blocos de pedra em 4 fileiras, desencontrados
        int fila = y / 64;
        float desloc = (fila % 2) * 64;
        int col = (int)((x + desloc) / 128f);
        float bx = (x + desloc) % 128f, by = y % 64;
        bool argamassa = bx < 4 || by < 4;
        float tom = 0.62f + Hash(col, fila) * 0.18f;
        float n = Fbm(u * 8, v * 8) * 0.25f;
        float g = argamassa ? 0.38f + n * 0.3f : tom + n - 0.12f;
        return new Color(g * 1.02f, g, g * 0.96f);
    }

    static Color Madeira(float u, float v, int x, int y)
    {
        // caixa de madeira: moldura, tábuas horizontais e travessa diagonal
        var claro = new Color(0.66f, 0.47f, 0.27f);
        var escuro = new Color(0.36f, 0.24f, 0.12f);
        bool moldura = x < 22 || x > 233 || y < 22 || y > 233;
        bool diagonal = Mathf.Abs(x - y) < 16;
        int tabua = y / 43;
        float veio = Mathf.PerlinNoise(u * 2 + tabua * 7, v * 40) * 0.5f + Fbm(u * 3, v * 12) * 0.4f;
        bool fresta = y % 43 < 2;
        var c = Color.Lerp(escuro, claro, 0.35f + veio * 0.7f + Hash(tabua, 3) * 0.15f);
        if (moldura || diagonal) c = Color.Lerp(escuro, claro, 0.2f + veio * 0.6f);
        if ((moldura || diagonal) && (x % 70 < 6 && y % 70 < 6)) c = new Color(0.25f, 0.25f, 0.27f); // pregos
        if (fresta && !moldura && !diagonal) c *= 0.45f;
        c.a = 1;
        return c;
    }

    static Color Saco(float u, float v, int x, int y)
    {
        float trama = (Mathf.Sin(x * 1.6f) * Mathf.Sin(y * 1.6f)) * 0.06f;
        float n = Fbm(u * 5, v * 5) * 0.22f;
        float costura = (y % 85 < 3) ? -0.15f : 0f;
        float g = 0.58f + trama + n + costura;
        return new Color(g * 1.05f, g * 0.94f, g * 0.7f);
    }

    static Color Terra(float u, float v, int x, int y)
    {
        float n = Fbm(u * 5, v * 5) * 0.4f;
        float pedrisco = Hash(x / 3, y / 3) > 0.97f ? 0.15f : 0f;
        float g = 0.32f + n + pedrisco;
        return new Color(g * 1.05f, g * 0.95f, g * 0.8f);
    }

    static Color Rocha(float u, float v, int x, int y)
    {
        float n = Fbm(u * 4, v * 4, 5);
        float g = 0.35f + n * 0.5f;
        return new Color(g, g * 0.98f, g * 0.95f);
    }

    static Color Metal(float u, float v, int x, int y)
    {
        float escovado = Mathf.PerlinNoise(u * 2, v * 60) * 0.15f;
        float g = 0.7f + escovado;
        float dx = u - 0.5f, dy = v - 0.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (Mathf.Abs(d - 0.42f) < 0.02f) g *= 0.6f; // aro
        return new Color(g, g, g);
    }

    static Color Particula(float u, float v, int x, int y)
    {
        float d = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f);
        float a = Mathf.Pow(1f - d, 2f);
        return new Color(1, 1, 1, a);
    }

    static Color Brilho(float u, float v, int x, int y)
    {
        float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
        float glow = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f) * 0.6f;
        float anel = Mathf.Exp(-Mathf.Pow((d - 0.62f) / 0.05f, 2f));
        return new Color(1, 1, 1, Mathf.Clamp01(glow + anel));
    }

    static Texture2D SalvarTextura(string nome, int tamanho, System.Func<float, float, int, int, Color> f, bool alfa = false)
    {
        var t = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        for (int y = 0; y < tamanho; y++)
            for (int x = 0; x < tamanho; x++)
                t.SetPixel(x, y, f(x / (float)tamanho, y / (float)tamanho, x * 256 / tamanho, y * 256 / tamanho));
        string caminho = $"Assets/Texturas/{nome}.png";
        File.WriteAllBytes(caminho, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(caminho);
        var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
        imp.wrapMode = alfa ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        imp.alphaIsTransparency = alfa;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 4;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
    }

    // ---------- sprites da explosão (8 quadros) + AnimationClip + Animator ----------

    static Sprite[] GerarSpritesExplosao()
    {
        const int n = 8, tam = 128;
        var sprites = new Sprite[n];
        for (int k = 0; k < n; k++)
        {
            float t = k / (float)(n - 1);
            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            float raio = 0.28f + 0.62f * Mathf.Pow(t, 0.6f);
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                {
                    float u = x / (float)tam - 0.5f, v = y / (float)tam - 0.5f;
                    float d = Mathf.Sqrt(u * u + v * v) * 2f;
                    float ruido = Fbm(x * 0.045f + k * 0.9f, y * 0.045f - k * 0.6f) - 0.45f;
                    float borda = raio * (0.85f + ruido * 0.6f);
                    if (d > borda) { tex.SetPixel(x, y, Color.clear); continue; }
                    float calor = Mathf.Clamp01((1f - d / borda) * 1.6f - t * 1.1f + ruido * 0.5f);
                    var fumaca = new Color(0.22f, 0.2f, 0.19f);
                    var fogo = Color.Lerp(new Color(0.95f, 0.35f, 0.05f), new Color(1f, 0.95f, 0.7f), calor);
                    var c = Color.Lerp(fumaca, fogo, Mathf.Clamp01(calor * 2.2f));
                    float a = Mathf.Clamp01((borda - d) / 0.08f) * (t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f * 0.85f);
                    c.a = a;
                    tex.SetPixel(x, y, c);
                }
            string caminho = $"Assets/Sprites/Explosao_{k}.png";
            File.WriteAllBytes(caminho, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(caminho);
            var imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = tam;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            sprites[k] = AssetDatabase.LoadAssetAtPath<Sprite>(caminho);
        }
        return sprites;
    }

    static AnimatorController AnimacaoExplosao(Sprite[] sprites)
    {
        var clip = new AnimationClip { name = "Explosao", frameRate = 12 };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var chaves = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            chaves[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, chaves);
        var cfg = AnimationUtility.GetAnimationClipSettings(clip);
        cfg.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, cfg);
        AssetDatabase.DeleteAsset("Assets/Animacoes/Explosao.anim");
        AssetDatabase.CreateAsset(clip, "Assets/Animacoes/Explosao.anim");
        AssetDatabase.DeleteAsset("Assets/Animacoes/Explosao.controller");
        return AnimatorController.CreateAnimatorControllerAtPathWithClip("Assets/Animacoes/Explosao.controller", clip);
    }

    // ---------- materiais de efeito ----------

    static Material Aditivo(string nome, string shader, Color cor, Texture2D tex)
    {
        var m = new Material(Shader.Find(shader));
        m.SetColor("_BaseColor", cor);
        m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", 2);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetOverrideTag("RenderType", "Transparent");
        return Salvar(m, nome);
    }

    static Material Particula(string nome, Texture2D tex)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Surface", 1);
        m.SetFloat("_Blend", 0);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetOverrideTag("RenderType", "Transparent");
        return Salvar(m, nome);
    }

    // ---------- prefabs ----------

    static GameObject SalvarPrefab(GameObject go, string nome)
    {
        string caminho = $"Assets/Prefabs/{nome}.prefab";
        var p = PrefabUtility.SaveAsPrefabAsset(go, caminho);
        Object.DestroyImmediate(go);
        return p;
    }

    static GameObject PrefabBloco(string nome, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);   // já vem com BoxCollider
        go.name = nome;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.On;
        r.receiveShadows = true;
        go.isStatic = true;
        return SalvarPrefab(go, nome);
    }

    static GameObject PrefabUnidade(Material matBase)
    {
        var go = new GameObject("Unidade");
        go.AddComponent<Unidade>();
        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.75f, 0);
        col.radius = 0.36f;
        col.height = 1.5f;
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 80f;
        rb.isKinematic = true;          // enquanto vivo, quem move é o script; ao morrer, a física
        rb.useGravity = true;
        rb.linearDamping = 0.4f;
        rb.angularDamping = 1.2f;
        rb.interpolation = RigidbodyInterpolation.None;    // o script move o Transform direto
        var b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(b.GetComponent<Collider>());
        b.name = "Base";
        b.transform.SetParent(go.transform, false);
        b.transform.localScale = new Vector3(0.95f, 0.025f, 0.95f);
        b.transform.localPosition = Vector3.up * 0.025f;
        b.GetComponent<Renderer>().sharedMaterial = matBase;
        return SalvarPrefab(go, "Unidade");
    }

    static GameObject PrefabProjetil(Material mat, Material matRastro)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projetil";
        go.transform.localScale = Vector3.one * 0.14f;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.6f;              // 0,22 u no mundo, para não atravessar nada entre dois passos da física
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = matRastro;
        trail.time = 0.2f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0, 0.18f), new Keyframe(1, 0f));
        trail.colorGradient = Gradiente(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.45f, 0.1f), 1f, 0f);
        trail.shadowCastingMode = ShadowCastingMode.Off;
        go.AddComponent<Projetil>();
        return SalvarPrefab(go, "Projetil");
    }

    static Gradient Gradiente(Color a, Color b, float alfaA, float alfaB)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(a, 0), new GradientColorKey(b, 1) },
                  new[] { new GradientAlphaKey(alfaA, 0), new GradientAlphaKey(alfaB, 1) });
        return g;
    }

    static ParticleSystem NovoSistema(GameObject go, Material mat, int qtd, Vector2 vida, Vector2 velocidade, Vector2 tamanho,
        Color corA, Color corB, float gravidade)
    {
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidade.x, velocidade.y);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanho.x, tamanho.y);
        main.startColor = new ParticleSystem.MinMaxGradient(corA, corB);
        main.gravityModifier = gravidade;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        var em = ps.emission;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)qtd) });
        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.1f)));
        var cor = ps.colorOverLifetime;
        cor.enabled = true;
        cor.color = new ParticleSystem.MinMaxGradient(Gradiente(Color.white, Color.white, 1f, 0f));
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mat;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    static GameObject PrefabDisparo(Material matFaisca)
    {
        var go = new GameObject("FxDisparo");
        var ps = NovoSistema(go, matFaisca, 22, new Vector2(0.08f, 0.2f), new Vector2(2f, 8f), new Vector2(0.2f, 0.5f),
            new Color(1f, 0.95f, 0.6f), new Color(1f, 0.55f, 0.15f), 0f);
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 16f;
        forma.radius = 0.02f;
        // clarão: luz pontual rápida no cano
        var luz = new GameObject("Clarao").AddComponent<Light>();
        luz.transform.SetParent(go.transform, false);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.75f, 0.35f);
        luz.range = 3f;
        luz.intensity = 4f;
        luz.gameObject.AddComponent<AutoDestruir>().segundos = 0.06f;
        go.AddComponent<AutoDestruir>().segundos = 0.6f;
        return SalvarPrefab(go, "FxDisparo");
    }

    static GameObject PrefabImpacto(Material matFaisca, Material matFumaca)
    {
        var go = new GameObject("FxImpacto");
        var ps = NovoSistema(go, matFaisca, 36, new Vector2(0.25f, 0.6f), new Vector2(3f, 8f), new Vector2(0.12f, 0.3f),
            new Color(1f, 0.9f, 0.5f), new Color(1f, 0.45f, 0.1f), 1.6f);
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 55f;
        forma.radius = 0.05f;
        var fumacaGo = new GameObject("Poeira");
        fumacaGo.transform.SetParent(go.transform, false);
        var fum = NovoSistema(fumacaGo, matFumaca, 10, new Vector2(0.7f, 1.3f), new Vector2(0.4f, 1.4f), new Vector2(0.5f, 1.0f),
            new Color(0.55f, 0.52f, 0.48f, 0.7f), new Color(0.35f, 0.33f, 0.3f, 0.6f), -0.05f);
        var tam = fum.sizeOverLifetime;
        tam.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 1.6f)));
        var f2 = fum.shape;
        f2.shapeType = ParticleSystemShapeType.Sphere;
        f2.radius = 0.1f;
        go.AddComponent<AutoDestruir>().segundos = 1.4f;
        return SalvarPrefab(go, "FxImpacto");
    }

    static GameObject PrefabExplosao()
    {
        var sprites = GerarSpritesExplosao();
        var controlador = AnimacaoExplosao(sprites);
        var go = new GameObject("Explosao");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        go.transform.localScale = Vector3.one * 2.4f;
        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = controlador;
        go.AddComponent<Billboard>();
        go.AddComponent<AutoDestruir>().segundos = sprites.Length / 12f + 0.05f;
        return SalvarPrefab(go, "Explosao");
    }

    static GameObject PrefabHalo(Material matHalo)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.name = "Halo";
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = matHalo;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(go.transform, false);
        luz.type = LightType.Point;
        luz.color = new Color(0.45f, 0.7f, 1f);
        luz.range = 4.5f;
        luz.intensity = 3f;
        luz.shadows = LightShadows.None;
        var h = go.AddComponent<HaloPulsante>();
        h.luz = luz;
        h.escalaBase = 1.1f;
        h.intensidadeBase = 3f;
        go.AddComponent<Billboard>();
        return SalvarPrefab(go, "Halo");
    }
}
