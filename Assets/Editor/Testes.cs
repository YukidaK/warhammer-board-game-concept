using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Testes automáticos dos times e cenários novos (batchmode):
//   Testes.Giros    -> render de frente de cada membro de cada time (Capturas/giros), para conferir o giro
//   Testes.Partidas -> joga partidas automáticas com várias combinações e salva prints (Capturas/testes)
public static class Testes
{
    // ---------- renders de frente ----------
    public static void Giros()
    {
        ShaderUtil.allowAsyncCompilation = false;
        Directory.CreateDirectory("Capturas/giros");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var luz = new GameObject("Sol").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 1.5f;
        luz.transform.rotation = Quaternion.Euler(40, -30, 0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.87f, 0.89f);
        cam.fieldOfView = 30;
        var rt = new RenderTexture(300, 400, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        foreach (var guid in AssetDatabase.FindAssets("t:Equipe", new[] { "Assets/Equipes" }))
        {
            var e = AssetDatabase.LoadAssetAtPath<Equipe>(AssetDatabase.GUIDToAssetPath(guid));
            var prancha = new Texture2D(300 * e.membros.Length, 400, TextureFormat.RGB24, false);
            for (int i = 0; i < e.membros.Length; i++)
            {
                var m = e.membros[i];
                var go = Object.Instantiate(m.modelo);
                go.transform.localScale = Vector3.one * e.escala * m.escala;
                // a unidade do jogador olha para +Z; a câmera fica em +Z olhando para ela
                go.transform.rotation = Quaternion.Euler(0, m.giro, 0);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = e.material;
                cam.transform.position = new Vector3(0, 1.3f, 6.5f);
                cam.transform.LookAt(new Vector3(0, 0.95f, 0));
                cam.Render();
                RenderTexture.active = rt;
                prancha.ReadPixels(new Rect(0, 0, 300, 400), 300 * i, 0);
                RenderTexture.active = null;
                Object.DestroyImmediate(go);
            }
            prancha.Apply();
            File.WriteAllBytes($"Capturas/giros/{e.name}.png", prancha.EncodeToPNG());
            Debug.Log($"[Testes] giros {e.name}");
        }
        EditorApplication.Exit(0);
    }

    // ---------- partidas automáticas ----------
    static readonly (int jogador, int inimigo, int cenario)[] Combos =
    {
        (3, 4, 0),   // Kroot x Pox, Colmeia
        (5, 1, 1),   // Ogros x Anjos, Manufatorum
        (2, 0, 2),   // Vigília x Harakoni, Catedral
        (4, 5, 0),   // Pox x Ogros, Colmeia
        (1, 3, 1),   // Anjos x Kroot, Manufatorum
        (0, 2, 3),   // Harakoni x Vigília, Clássico
    };
    static int combo;
    static string etapa;
    static double marca, inicioCombo;
    static RenderTexture alvo;
    static readonly List<string> resultados = new();

    public static void Partidas()
    {
        Directory.CreateDirectory("Capturas/testes");
        ShaderUtil.allowAsyncCompilation = false;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        combo = -1;
        etapa = "menu";
        Selecao.jogador = 3; Selecao.inimigo = 4; Selecao.cenario = 0;
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        marca = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Passo;
        EditorApplication.update += Passo;
        EditorApplication.EnterPlaymode();
    }

    static void ProximaPartida()
    {
        combo++;
        if (combo >= Combos.Length)
        {
            foreach (var r in resultados) Debug.Log("[Testes] " + r);
            Debug.Log("[Testes] fim");
            EditorApplication.update -= Passo;
            EditorSettings.enterPlayModeOptionsEnabled = false;
            Jogo.autoJogador = false;
            Jogo.seedFixa = -1;
            Time.timeScale = 1;
            EditorApplication.Exit(0);
            return;
        }
        var c = Combos[combo];
        Selecao.jogador = c.jogador; Selecao.inimigo = c.inimigo; Selecao.cenario = c.cenario;
        Jogo.seedFixa = 100 + combo * 17;
        Jogo.autoJogador = true;
        etapa = "abrindo";
        inicioCombo = EditorApplication.timeSinceStartup;
        EditorSceneManager.OpenScene("Assets/Scenes/Batalha.unity");
        EditorApplication.EnterPlaymode();
    }

    static void Passo()
    {
        if (etapa == "saindo")
        {
            if (EditorApplication.isPlaying) return;
            alvo = null;
            ProximaPartida();
            return;
        }
        if (!EditorApplication.isPlaying) return;

        if (etapa == "menu")
        {
            if (EditorApplication.timeSinceStartup - marca < 6) return;
            var menu = Object.FindAnyObjectByType<Menu>();
            if (menu == null) return;
            Salvar(menu.cam, "menu_1");
            menu.EscolherJogador(5);
            menu.EscolherInimigo(1);
            menu.EscolherCenario(2);
            marca = EditorApplication.timeSinceStartup;
            etapa = "menu2";
            return;
        }
        if (etapa == "menu2")
        {
            if (EditorApplication.timeSinceStartup - marca < 1.5) return;
            var menu = Object.FindAnyObjectByType<Menu>();
            if (menu == null) return;
            Salvar(menu.cam, "menu_2");
            EditorApplication.ExitPlaymode();
            etapa = "saindo";
            return;
        }

        var j = Object.FindAnyObjectByType<Jogo>();
        if (j == null || j.grade == null) return;
        string nome = $"{combo}_{j.EquipeJogador.name}_x_{j.EquipeInimiga.name}_{j.CenarioAtual.name}";
        if (EditorApplication.timeSinceStartup - inicioCombo > 400)
        {
            resultados.Add($"{nome}: TEMPO ESGOTADO no turno {j.turno}");
            Salvar(j.cam, nome + "_tempo");
            Time.timeScale = 1;
            EditorApplication.ExitPlaymode();
            etapa = "saindo";
            return;
        }
        switch (etapa)
        {
            case "abrindo":
                var centro = new Vector3((j.grade.largura - 1) * Grade.Tamanho / 2, 0, (j.grade.altura - 1) * Grade.Tamanho / 2);
                j.camTatica.PosicionarJa(centro);
                j.camTatica.DefinirZoom(34f);
                marca = EditorApplication.timeSinceStartup;
                etapa = "geral";
                break;
            case "geral":
                if (EditorApplication.timeSinceStartup - marca < 2) return;
                Salvar(j.cam, nome + "_a_geral");
                // close nas equipes
                j.camTatica.PosicionarJa(j.azuis[0].transform.position + new Vector3(0, 0, 2));
                j.camTatica.DefinirZoom(12f);
                marca = EditorApplication.timeSinceStartup;
                etapa = "close";
                break;
            case "close":
                if (EditorApplication.timeSinceStartup - marca < 1.5) return;
                Salvar(j.cam, nome + "_b_azuis");
                j.camTatica.PosicionarJa(j.vermelhos[0].transform.position + new Vector3(0, 0, -1));
                marca = EditorApplication.timeSinceStartup;
                etapa = "close2";
                break;
            case "close2":
                if (EditorApplication.timeSinceStartup - marca < 1.5) return;
                Salvar(j.cam, nome + "_c_vermelhos");
                Time.timeScale = 6f;
                etapa = "jogando";
                break;
            case "jogando":
                if (!j.Acabou) return;
                Time.timeScale = 1f;
                marca = EditorApplication.timeSinceStartup;
                etapa = "fim";
                break;
            case "fim":
                if (EditorApplication.timeSinceStartup - marca < 2.5) return;
                resultados.Add($"{nome}: {(j.Vitoria ? "VITÓRIA" : "DERROTA")} no turno {j.turno}, {j.pontos.Total} pts, azuis vivos {j.azuis.FindAll(u => u.Viva).Count}, vermelhos vivos {j.vermelhos.FindAll(u => u.Viva).Count}");
                Salvar(j.cam, nome + "_d_fim");
                EditorApplication.ExitPlaymode();
                etapa = "saindo";
                break;
        }
    }

    static void Salvar(Camera cam, string nome)
    {
        if (alvo == null)
        {
            alvo = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        }
        var antes = cam.targetTexture;
        cam.targetTexture = alvo;
        Canvas.ForceUpdateCanvases();
        cam.Render();
        cam.Render();
        RenderTexture.active = alvo;
        var tex = new Texture2D(alvo.width, alvo.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, alvo.width, alvo.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = antes;
        File.WriteAllBytes($"Capturas/testes/{nome}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Debug.Log($"[Testes] print {nome}");
    }
}
