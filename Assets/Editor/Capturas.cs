using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Roteiros automáticos de captura (batchmode):
//   Capturas.Executar    -> joga uma partida e tira os prints dos momentos principais
//   Capturas.Video       -> joga uma partida gravando os quadros para o vídeo
//   Capturas.Personagens -> renderiza cada modelo em "estúdio" para a prancha de personagens
public static class Capturas
{
    static string etapa;
    static double marca, inicio;
    static bool fezMira, fezTiro, fezInimigo, fezMeio, fezExplosao, video;
    static int turnoVisto;
    static float giroFeito;
    static Vector3 destinoTiro;
    static Projetil projetil;
    static RenderTexture alvo;

    public static void Executar() => Iniciar(false);
    public static void Video() => Iniciar(true);

    static void Iniciar(bool gravar)
    {
        video = gravar;
        Directory.CreateDirectory("Capturas");
        ShaderUtil.allowAsyncCompilation = false;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        EditorSceneManager.OpenScene("Assets/Scenes/Batalha.unity");
        Jogo.seedFixa = 1234;
        Jogo.autoJogador = false;
        etapa = "esperando";
        inicio = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Passo;
        EditorApplication.update += Passo;
        EditorApplication.EnterPlaymode();
    }

    static Jogo J => Object.FindAnyObjectByType<Jogo>();
    static double Agora => video ? Time.time : EditorApplication.timeSinceStartup;

    static void Passo()
    {
        if (!EditorApplication.isPlaying) return;
        var j = J;
        if (j == null || j.grade == null) return;

        if (alvo == null)
        {
            alvo = video ? new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) : new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            alvo.antiAliasing = 4;
            j.cam.targetTexture = alvo;
            if (video)
            {
                Gravador.pasta = "Capturas/video";
                if (Directory.Exists(Gravador.pasta)) Directory.Delete(Gravador.pasta, true);
                j.cam.gameObject.AddComponent<Gravador>().cam = j.cam;
            }
        }

        if (EditorApplication.timeSinceStartup - inicio > 1500) { Salvar("99_tempo_esgotado"); Terminar("tempo esgotado"); return; }

        var centro = new Vector3((j.grade.largura - 1) * Grade.Tamanho / 2, 0, (j.grade.altura - 1) * Grade.Tamanho / 2);
        switch (etapa)
        {
            case "esperando":
                if (!j.EsperandoJogador) return;
                j.camTatica.PosicionarJa(centro);
                j.camTatica.DefinirZoom(video ? 30f : 34f);
                etapa = "geral";
                marca = Agora;
                giroFeito = 0;
                return;

            case "geral":
                if (video)
                {
                    // abertura: a câmera gira devagar mostrando o cenário
                    float alvoGiro = 50f * Mathf.Clamp01((float)(Agora - marca) / 4f);
                    j.camTatica.Girar(alvoGiro - giroFeito);
                    giroFeito = alvoGiro;
                    if (Agora - marca < 4.5) return;
                    j.camTatica.Girar(-giroFeito);
                    j.camTatica.DefinirZoom(20f);
                    j.camTatica.PosicionarJa(j.selecionada.transform.position);
                    etapa = "hover";
                    marca = Agora;
                    return;
                }
                if (Agora - marca < 1.2) return;
                Salvar("01_visao_geral");
                j.camTatica.DefinirZoom(7f);
                j.camTatica.PosicionarJa(j.selecionada.transform.position);
                j.camTatica.Girar(180f);
                etapa = "close";
                marca = Agora;
                return;

            case "close":
                if (Agora - marca < 1.0) return;
                Salvar("02_close_azul");
                j.camTatica.Girar(180f);
                j.camTatica.PosicionarJa(j.vermelhos[0].transform.position);
                j.camTatica.DefinirZoom(7f);
                etapa = "close2";
                marca = Agora;
                return;

            case "close2":
                j.camTatica.PosicionarJa(j.vermelhos[0].transform.position);
                if (Agora - marca < 0.5) return;
                Salvar("03_close_vermelho");
                j.camTatica.DefinirZoom(20f);
                j.camTatica.PosicionarJa(j.selecionada.transform.position);
                etapa = "hover";
                marca = Agora;
                return;

            case "hover":
                if (Agora - marca < 1.5) return;
                // mostra o alcance de movimento e o cursor numa casa com cobertura
                var s = j.selecionada;
                var alcance = j.grade.Alcance(s.cel, s.mobilidade, out _);
                Vector2Int melhor = s.cel;
                int melhorCob = -1;
                foreach (var kv in alcance)
                {
                    int cob = 0;
                    foreach (var d in Grade.Direcoes) cob += j.grade.CoberturaEm(kv.Key, d);
                    if (kv.Key != s.cel && cob > melhorCob) { melhorCob = cob; melhor = kv.Key; }
                }
                j.mouseForcado = melhor;
                etapa = "print_mover";
                marca = Agora;
                return;

            case "print_mover":
                if (Agora - marca < (video ? 1.6 : 0.6)) return;
                if (!video) Salvar("04_movimento");
                j.Hover(true);  // executa o movimento como se fosse um clique
                etapa = "moveu";
                marca = Agora;
                return;

            case "moveu":
                if (!j.EsperandoJogador) return;
                j.mouseForcado = null;
                Jogo.autoJogador = true;
                Time.timeScale = video ? 1f : 3f;
                etapa = "jogando";
                return;

            case "jogando":
                Jogando(j);
                return;

            case "mira":
                if (Agora - marca < (video ? 1.8 : 0.8)) return;
                if (!video) Salvar("05_mira");
                j.mouseForcado = null;
                Jogo.autoJogador = true;
                Time.timeScale = video ? 1f : 3f;
                etapa = "jogando";
                return;

            case "tiro":
                // espera o projétil chegar ao meio do caminho e centraliza nele (rastro + clarão do cano)
                if (projetil != null && projetil.Ativo &&
                    Vector3.Distance(projetil.Origem, projetil.transform.position) < Vector3.Distance(projetil.Origem, projetil.Destino) * 0.55f &&
                    Agora - marca < 8) return;
                if (projetil != null) j.camTatica.PosicionarJa(Vector3.Lerp(projetil.Origem, projetil.transform.position, 0.6f));
                j.camTatica.DefinirZoom(10f);
                etapa = "tiro2";
                marca = Agora;
                return;

            case "tiro2":
                if (Agora - marca < 0.05) return;
                Salvar("06_tiro");
                Time.timeScale = 3f;
                etapa = "jogando";
                return;

            case "explosao":
                if (Agora - marca < 0.15) return;
                Salvar("07_explosao");
                Time.timeScale = 3f;
                etapa = "jogando";
                return;

            case "fim":
                if (Agora - marca < (video ? 4.0 : 1.5)) return;
                if (!video) Salvar("10_fim");
                Terminar(j.Vitoria ? "VITÓRIA" : "DERROTA");
                return;
        }
    }

    static void Jogando(Jogo j)
    {
        if (j.Acabou)
        {
            Time.timeScale = 1f;
            etapa = "fim";
            marca = Agora;
            return;
        }

        // na primeira vez que um soldado tem inimigo à vista, mostra a tela de mira
        if (!fezMira && j.EsperandoJogador)
        {
            foreach (var u in j.azuis)
            {
                if (!u.Viva || u.pa == 0) continue;
                var alvos = j.Alvos(u);
                if (alvos.Count == 0) continue;
                Jogo.autoJogador = false;
                Time.timeScale = 1f;
                j.Selecionar(u);
                j.BotaoAcao("atirar");
                j.mouseForcado = alvos[0].alvo.cel;
                fezMira = true;
                etapa = "mira";
                marca = Agora;
                return;
            }
        }
        if (video) return;

        // tiro: câmera lenta e de perto no primeiro disparo, até as faíscas do impacto aparecerem
        if (!fezTiro)
        {
            var pj = Object.FindAnyObjectByType<Projetil>();
            if (pj != null && pj.Acerto && Vector3.Distance(pj.Origem, pj.Destino) < 13f && Object.FindObjectsByType<Projetil>(FindObjectsSortMode.None).Length == 1 && j.turno >= 1)
            {
                fezTiro = true;
                Time.timeScale = 0.12f;
                var o = pj.Origem;
                var d = pj.Destino;
                destinoTiro = d;
                projetil = pj;
                j.camTatica.PosicionarJa(Vector3.Lerp(o, d, 0.5f));
                j.camTatica.DefinirZoom(Mathf.Clamp(Vector3.Distance(o, d) * 1.35f, 10f, 17f));
                etapa = "tiro";
                marca = Agora;
                return;
            }
        }

        if (!fezExplosao && GameObject.Find("Explosao") != null)
        {
            fezExplosao = true;
            Time.timeScale = 0.25f;
            var e = GameObject.Find("Explosao").transform.position;
            j.camTatica.PosicionarJa(e);
            j.camTatica.DefinirZoom(8f);
            etapa = "explosao";
            marca = Agora;
            return;
        }

        if (j.turno != turnoVisto)
        {
            turnoVisto = j.turno;
            marca = Agora;
        }
        if (!fezInimigo && !j.turnoDoJogador && j.turno >= 2 && Agora - marca > 1.0)
        {
            fezInimigo = true;
            Salvar("08_turno_inimigo");
        }
        if (!fezMeio && j.turno >= 3 && j.turnoDoJogador && Agora - marca > 0.5)
        {
            fezMeio = true;
            Salvar("09_meio");
        }
    }

    static bool ImpactoPerto(Vector3 p)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "FxImpacto" && Vector3.Distance(t.position, p) < 1.5f) return true;
        return false;
    }

    static void Salvar(string nome)
    {
        var j = J;
        var cam = j.cam;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        cam.Render();
        var anterior = RenderTexture.active;
        RenderTexture.active = alvo;
        var tex = new Texture2D(alvo.width, alvo.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, alvo.width, alvo.height), 0, 0);
        tex.Apply();
        RenderTexture.active = anterior;
        File.WriteAllBytes($"Capturas/{nome}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[Capturas] {nome} (turno {j.turno})");
    }

    static void Terminar(string motivo)
    {
        Debug.Log($"[Capturas] fim: {motivo}");
        Time.timeScale = 1f;
        Jogo.autoJogador = false;
        Jogo.seedFixa = -1;
        EditorApplication.update -= Passo;
        EditorSettings.enterPlayModeOptionsEnabled = false;
        EditorApplication.Exit(0);
    }

    // ---------- prancha de personagens ----------

    public static void Personagens()
    {
        ShaderUtil.allowAsyncCompilation = false;
        Directory.CreateDirectory("Capturas/personagens");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var luz = new GameObject("Sol").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.5f;
        luz.color = new Color(1f, 0.96f, 0.9f);
        luz.shadows = LightShadows.Soft;
        luz.transform.rotation = Quaternion.Euler(40, -40, 0);
        var contra = new GameObject("Contraluz").AddComponent<Light>();
        contra.type = LightType.Directional;
        contra.intensity = 0.7f;
        contra.color = new Color(0.7f, 0.8f, 1f);
        contra.transform.rotation = Quaternion.Euler(25, 150, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.5f);

        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.87f, 0.89f);
        cam.fieldOfView = 26;
        var rt = new RenderTexture(600, 800, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt;

        var chao = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chao.transform.localScale = new Vector3(1.6f, 0.02f, 1.6f);
        chao.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/ChaoA.mat");

        var azul = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/Azul.mat");
        var vermelho = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/Vermelho.mat");
        var baseAzul = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/BaseAzul.mat");
        var baseVermelha = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/BaseVermelha.mat");
        var lista = new (string modelo, bool azul, string arquivo)[]
        {
            ("Sergeant", true, "azul_1_sargento"), ("Trooper", true, "azul_2_fuzileiro"), ("Sniper", true, "azul_3_atiradora"),
            ("Medic_trooper", true, "azul_4_medico"), ("Trooper", false, "verm_1_soldado"), ("Plasma", false, "verm_2_plasma"),
            ("Melta", false, "verm_3_melta"), ("Volleygun", false, "verm_4_metralha"), ("Demo", false, "verm_5_demolidor"),
            ("Recon", false, "verm_6_batedor"), ("Vox", false, "verm_7_radio"),
        };
        foreach (var (modelo, ehAzul, arquivo) in lista)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/{modelo}.obj");
            var go = Object.Instantiate(asset);
            go.transform.localScale = Vector3.one * 1.45f;
            go.transform.rotation = Quaternion.Euler(0, ehAzul ? 155 : 205, 0);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = ehAzul ? azul : vermelho;
            chao.GetComponent<Renderer>().sharedMaterial = ehAzul ? baseAzul : baseVermelha;
            cam.transform.position = new Vector3(0, 1.35f, -6.4f);
            cam.transform.LookAt(new Vector3(0, 0.72f, 0));
            cam.Render();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes($"Capturas/personagens/{arquivo}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
            Debug.Log($"[Personagens] {arquivo}");
        }
        EditorApplication.Exit(0);
    }
}
