using System.IO;
using UnityEditor;
using UnityEngine;

// Roteiro automático de teste: joga uma partida inteira (equipe azul no piloto automático),
// tira prints dos momentos principais e sai. Uso: Unity -batchmode -executeMethod Capturas.Executar
public static class Capturas
{
    static string etapa;
    static double marca, inicio;
    static bool fezMira, fezTiro, fezInimigo, fezMeio;
    static int turnoVisto;
    static RenderTexture alvo;

    public static void Executar()
    {
        Directory.CreateDirectory("Capturas");
        ShaderUtil.allowAsyncCompilation = false;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Batalha.unity");
        Jogo.seedFixa = 1234;
        Jogo.autoJogador = false;
        etapa = "esperando";
        inicio = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Passo;
        EditorApplication.update += Passo;
        EditorApplication.EnterPlaymode();
    }

    static Jogo J => Object.FindAnyObjectByType<Jogo>();
    static double Agora => EditorApplication.timeSinceStartup;

    static void Passo()
    {
        if (!EditorApplication.isPlaying) return;
        var j = J;
        if (j == null || j.grade == null) return;

        if (alvo == null)
        {
            alvo = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            j.cam.targetTexture = alvo;
        }

        if (Agora - inicio > 600) { Salvar("99_tempo_esgotado"); Terminar("tempo esgotado"); return; }

        switch (etapa)
        {
            case "esperando":
                if (!j.EsperandoJogador) return;
                j.camTatica.DefinirZoom(7f);
                j.camTatica.Girar(180f);
                etapa = "close";
                marca = Agora;
                return;

            case "close":
                if (Agora - marca < 1.0) return;
                Salvar("00_close_azul");
                j.camTatica.Girar(180f);
                j.camTatica.PosicionarJa(j.vermelhos[0].transform.position);
                j.camTatica.DefinirZoom(7f);
                etapa = "close2";
                marca = Agora;
                return;

            case "close2":
                j.camTatica.PosicionarJa(j.vermelhos[0].transform.position);
                if (Agora - marca < 0.5) return;
                Salvar("00_close_vermelho");
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
                if (Agora - marca < 0.6) return;
                Salvar("01_movimento");
                j.Hover(true);  // executa o movimento como se fosse um clique
                etapa = "moveu";
                marca = Agora;
                return;

            case "moveu":
                if (!j.EsperandoJogador) return;
                j.mouseForcado = null;
                Jogo.autoJogador = true;
                Time.timeScale = 3f;
                etapa = "jogando";
                return;

            case "jogando":
                Jogando(j);
                return;

            case "mira":
                if (Agora - marca < 0.8) return;
                Salvar("02_mira");
                j.mouseForcado = null;
                Jogo.autoJogador = true;
                Time.timeScale = 3f;
                etapa = "jogando";
                return;

            case "fim":
                if (Agora - marca < 1.5) return;
                Salvar("06_fim");
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

        if (!fezTiro && GameObject.Find("Traco") != null)
        {
            fezTiro = true;
            Salvar("03_tiro");
        }

        if (j.turno != turnoVisto)
        {
            turnoVisto = j.turno;
            marca = Agora;
        }
        if (!fezInimigo && !j.turnoDoJogador && j.turno >= 2 && Agora - marca > 1.0)
        {
            fezInimigo = true;
            Salvar("04_turno_inimigo");
        }
        if (!fezMeio && j.turno >= 4 && j.turnoDoJogador && Agora - marca > 0.5)
        {
            fezMeio = true;
            Salvar("05_meio");
        }
    }

    static void Salvar(string nome)
    {
        var j = J;
        var cam = j.cam;
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
}
