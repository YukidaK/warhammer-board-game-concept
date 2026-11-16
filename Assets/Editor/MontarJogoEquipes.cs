using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Times (facções), cenários e menu. Os modelos vêm dos STLs em ~/Downloads/kill_team,
// convertidos por tools/decimar.py para Assets/Models/<Time> e Assets/Models/Cenario.
public static partial class MontarJogo
{
    static GameObject Obj(string caminho)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/{caminho}.obj");
        if (go == null) Debug.LogError($"[Montar] modelo não encontrado: {caminho}");
        return go;
    }

    static Membro M(string nome, string classe, Papel papel, string modelo, int hp, int mira, int dmin, int dmax, float mob, float escala = 1f)
    {
        return new Membro
        {
            nome = nome, classe = classe, papel = papel, modelo = Obj(modelo), hp = hp, mira = mira,
            danoMin = dmin, danoMax = dmax, mobilidade = mob, escala = escala, giro = Giro(modelo)
        };
    }

    // giro em Y para cada modelo olhar para +Z (conferido nos renders de estúdio)
    // (Harakoni, Anjos, Vigília e ogros da Ice Guard já olham para +Z; Kroot e Crabby para -Z; cultista para -X; Pox para +X)
    static readonly Dictionary<string, float> Giros = new()
    {
        ["Sniper"] = -90,
        ["Kroot/KillBroker"] = 180, ["Kroot/Warrior"] = 180, ["Kroot/LongSight"] = 180, ["Kroot/ColdBlood"] = 180,
        ["Kroot/Tracker"] = 180, ["Kroot/Pistolier"] = 180, ["Kroot/Stalker"] = 180, ["Kroot/BowHunter"] = 180,
        ["Kroot/CutSkin"] = 180, ["Kroot/HeavyGunnerSkinner"] = 180, ["Kroot/HeavyGunnerTribalest"] = 180,
        ["Pox/Boss"] = -90, ["Pox/Fighter"] = -90, ["Pox/Gunner1"] = -90, ["Pox/Gunner2"] = -90, ["Pox/Banner"] = -90,
        ["Pox/Crusha"] = -90, ["Pox/Demolisha"] = -90, ["Pox/RokkitRack"] = -90, ["Pox/Bombsquig1"] = -90, ["Pox/Bombsquig2"] = -90,
        ["Ogros/Crabby"] = 180, ["Ogros/Cultista"] = 90,
    };

    static float Giro(string modelo) => Giros.TryGetValue(modelo, out var g) ? g : 0f;

    static Equipe NovaEquipe(string arquivo, string nome, string descricao, Color cor, Material mat, float escala, int qtdInimigos, params Membro[] membros)
    {
        var e = ScriptableObject.CreateInstance<Equipe>();
        e.nome = nome;
        e.descricao = descricao;
        e.cor = cor;
        e.material = mat;
        e.escala = escala;
        e.qtdInimigos = qtdInimigos;
        e.membros = membros;
        string caminho = $"Assets/Equipes/{arquivo}.asset";
        AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(e, caminho);
        return e;
    }

    static Equipe[] CriarEquipes()
    {
        Directory.CreateDirectory("Assets/Equipes");
        // modelos novos já vêm em escala real (0,045 unidade por mm); os Harakoni têm altura 1
        var harakoni = NovaEquipe("Harakoni", "Harakoni Kill Team",
            "Guarda imperial de elite. Soldados comuns, bem equipados e versáteis.",
            new Color(0.55f, 0.5f, 0.36f), Lit("Faccao_Harakoni", new Color(0.5f, 0.47f, 0.34f), 0.25f, 0.35f), 1.45f, 6,
            M("Sgt. Varn", "Sargento", Papel.Lider, "Sergeant", 8, 72, 3, 5, 6f),
            M("Cabo Rhee", "Fuzileiro", Papel.Fuzileiro, "Trooper", 7, 68, 3, 5, 6f),
            M("Kessa", "Atiradora", Papel.Atirador, "Sniper", 6, 80, 4, 7, 5f),
            M("Doc Malek", "Médico", Papel.Medico, "Medic_trooper", 6, 62, 2, 4, 6f),
            M("Plasma", "Plasma", Papel.Pesado, "Plasma", 7, 66, 3, 5, 6f),
            M("Melta", "Melta", Papel.Pesado, "Melta", 8, 63, 3, 6, 5f),
            M("Metralha", "Metralhadora", Papel.Pesado, "Volleygun", 7, 70, 2, 4, 5f),
            M("Demolidor", "Explosivos", Papel.Fuzileiro, "Demo", 8, 63, 3, 5, 6f),
            M("Batedor", "Batedor", Papel.Fuzileiro, "Recon", 6, 68, 2, 4, 7f),
            M("Rádio", "Comunicações", Papel.Fuzileiro, "Vox", 6, 63, 2, 3, 6f));

        var anjos = NovaEquipe("AnjosSombrios", "Anjos Sombrios",
            "Fuzileiros espaciais de manto verde. Poucos, resistentes e certeiros. Como inimigos, vêm só 5.",
            new Color(0.1f, 0.35f, 0.15f), Lit("Faccao_Anjos", new Color(0.13f, 0.42f, 0.2f), 0.35f, 0.45f), 0.82f, 5,
            M("Sgt. Zakariel", "Comandante", Papel.Lider, "Anjos/Comandante", 10, 74, 4, 6, 6f),
            M("Irmão Cassiel", "Bolter", Papel.Fuzileiro, "Anjos/Bolter", 9, 70, 3, 5, 6f),
            M("Irmão Nahum", "Atirador", Papel.Atirador, "Anjos/Atirador", 8, 82, 4, 7, 5f),
            M("Irmão Ithiel", "Apotecário", Papel.Medico, "Anjos/Plasma", 9, 66, 3, 5, 6f),
            M("Irmão Gadriel", "Bolter pesado", Papel.Pesado, "Anjos/BolterPesado", 10, 66, 4, 6, 5f),
            M("Irmão Samiel", "Espada-serra", Papel.CorpoACorpo, "Anjos/Espada", 9, 68, 3, 6, 7f));

        var vigilia = NovaEquipe("VigiliaDaMorte", "Vigília da Morte",
            "Esquadrão de veteranos de capítulos diferentes, cada um com sua arma. Como inimigos, vêm só 5.",
            new Color(0.15f, 0.15f, 0.17f), Lit("Faccao_Vigilia", new Color(0.26f, 0.27f, 0.31f), 0.6f, 0.5f), 0.82f, 5,
            M("Vigia Varek", "Líder", Papel.Lider, "Vigilia/TemplarioNegro", 10, 72, 4, 6, 6f),
            M("Vigia Thane", "Fuzileiro", Papel.Fuzileiro, "Vigilia/PunhoImperial", 9, 70, 3, 5, 6f),
            M("Vigia Lorcan", "Atirador", Papel.Atirador, "Vigilia/TemplarioVermelho", 8, 80, 4, 6, 5f),
            M("Vigia Oren", "Apotecário", Papel.Medico, "Vigilia/Excoriador", 9, 64, 3, 5, 6f),
            M("Vigia Hask", "Pesado", Papel.Pesado, "Vigilia/PunhoCarmesim", 10, 66, 4, 6, 5f),
            M("Vigia Ignis", "Espadachim", Papel.CorpoACorpo, "Vigilia/FilhosDaFenix", 9, 66, 3, 6, 7f));

        var kroot = NovaEquipe("Kroot", "Bando Kroot",
            "Caçadores alienígenas ágeis. Pouca armadura, muita mobilidade.",
            new Color(0.62f, 0.52f, 0.38f), Lit("Faccao_Kroot", new Color(0.58f, 0.47f, 0.33f), 0.05f, 0.3f), 0.92f, 6,
            M("Mestre Kha'rok", "Mestre de caça", Papel.Lider, "Kroot/KillBroker", 8, 70, 3, 5, 7f),
            M("Guerreiro Tuk", "Guerreiro", Papel.Fuzileiro, "Kroot/Warrior", 7, 66, 2, 5, 7f),
            M("Olho-Longo", "Atirador", Papel.Atirador, "Kroot/LongSight", 6, 82, 4, 7, 6f),
            M("Xamã Sangue-Frio", "Xamã", Papel.Medico, "Kroot/ColdBlood", 7, 62, 2, 4, 7f),
            M("Rastreador", "Rastreador", Papel.Fuzileiro, "Kroot/Tracker", 7, 66, 2, 4, 8f),
            M("Pistoleiro", "Pistoleiro", Papel.Fuzileiro, "Kroot/Pistolier", 7, 64, 3, 4, 7f),
            M("Espreitador", "Lâminas", Papel.CorpoACorpo, "Kroot/Stalker", 7, 62, 3, 5, 8f),
            M("Arqueiro", "Arqueiro", Papel.Fuzileiro, "Kroot/BowHunter", 6, 70, 3, 5, 7f),
            M("Corta-Pele", "Retalhador", Papel.CorpoACorpo, "Kroot/CutSkin", 8, 58, 3, 5, 7f),
            M("Artilheiro Esfolador", "Arma pesada", Papel.Pesado, "Kroot/HeavyGunnerSkinner", 8, 62, 3, 6, 6f),
            M("Artilheiro Tribal", "Arma pesada", Papel.Pesado, "Kroot/HeavyGunnerTribalest", 8, 62, 3, 6, 6f));

        var pox = NovaEquipe("PoxWreckas", "Pox Wreckas",
            "Orks pestilentos de cerco, com grots e squigs-bomba. Brutos e imprecisos.",
            new Color(0.42f, 0.5f, 0.2f), Lit("Faccao_Pox", new Color(0.38f, 0.46f, 0.17f), 0.1f, 0.35f), 1.0f, 6,
            M("Chefe Pustulento", "Chefe", Papel.Lider, "Pox/Boss", 10, 64, 3, 6, 6f),
            M("Brigão", "Brigão", Papel.CorpoACorpo, "Pox/Fighter", 9, 58, 3, 5, 6f),
            M("Atirador Podre", "Atirador", Papel.Atirador, "Pox/Gunner1", 7, 70, 3, 6, 5f),
            M("Grot Estandarte", "Curandeiro", Papel.Medico, "Pox/Banner", 5, 60, 1, 3, 7f, 1.3f),
            M("Disparador", "Arma pesada", Papel.Pesado, "Pox/Gunner2", 8, 60, 3, 5, 5f),
            M("Esmagador", "Esmagador", Papel.CorpoACorpo, "Pox/Crusha", 9, 56, 4, 6, 6f),
            M("Demolidor", "Demolidor", Papel.Pesado, "Pox/Demolisha", 8, 58, 3, 6, 5f),
            M("Grot Foguetes", "Foguetes", Papel.Fuzileiro, "Pox/RokkitRack", 5, 62, 3, 5, 7f, 1.3f),
            M("Squig-Bomba", "Squig", Papel.CorpoACorpo, "Pox/Bombsquig1", 5, 52, 4, 7, 8f, 1.2f),
            M("Squig-Bomba Gordo", "Squig", Papel.CorpoACorpo, "Pox/Bombsquig2", 6, 52, 4, 7, 7f, 1.2f));

        var ogros = NovaEquipe("OgrosRenegados", "Ogros Renegados",
            "Ogros desertores e um cultista. Enormes e difíceis de derrubar, mas lentos. Como inimigos, vêm só 4.",
            new Color(0.55f, 0.65f, 0.75f), Lit("Faccao_Ogros", new Color(0.5f, 0.58f, 0.66f), 0.25f, 0.4f), 0.88f, 4,
            M("Crabby", "Campeão", Papel.Lider, "Ogros/Crabby", 14, 60, 4, 7, 5f),
            M("Grum", "Bruto", Papel.Pesado, "Ogros/Ogro1", 13, 58, 4, 6, 5f),
            M("Borg", "Atirador", Papel.Atirador, "Ogros/Ogro2", 12, 68, 3, 6, 5f),
            M("Cultista Vexa", "Curandeira", Papel.Medico, "Ogros/Cultista", 6, 62, 2, 4, 6f, 1.1f),
            M("Thudd", "Esmagador", Papel.CorpoACorpo, "Ogros/Ogro3", 13, 56, 4, 7, 5f));

        return new[] { harakoni, anjos, vigilia, kroot, pox, ogros };
    }

    // ---------- cenários ----------

    // Prefab de peça: raiz com PecaCenario + BoxCollider, filho com o modelo. A peça fica com o lado
    // mais comprido em X (muros) e a base no chão.
    static GameObject PrefabPeca(string modelo, Material mat, bool comprimentoEmX = true)
    {
        string nome = $"{modelo}_{mat.name}";
        var raiz = new GameObject(nome);
        var m = (GameObject)Object.Instantiate(Obj("Cenario/" + modelo));
        m.name = "Modelo";
        m.transform.SetParent(raiz.transform, false);
        var b = new Bounds();
        bool primeiro = true;
        foreach (var r in m.GetComponentsInChildren<MeshRenderer>())
        {
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.On;
            if (primeiro) { b = r.bounds; primeiro = false; } else b.Encapsulate(r.bounds);
        }
        if (comprimentoEmX && b.size.z > b.size.x * 1.05f)
        {
            m.transform.localRotation = Quaternion.Euler(0, 90, 0);
            b = new Bounds(new Vector3(b.center.z, b.center.y, -b.center.x), new Vector3(b.size.z, b.size.y, b.size.x));
        }
        raiz.AddComponent<PecaCenario>().tamanho = b.size;
        var col = raiz.AddComponent<BoxCollider>();
        col.center = b.center;
        col.size = b.size;
        raiz.isStatic = true;
        Directory.CreateDirectory("Assets/Prefabs/Cenario");
        string caminho = $"Assets/Prefabs/Cenario/{nome}.prefab";
        var p = PrefabUtility.SaveAsPrefabAsset(raiz, caminho);
        Object.DestroyImmediate(raiz);
        return p;
    }

    static Marco Mc(GameObject prefab, int x, int y) => new() { prefab = prefab, tamanho = new Vector2Int(x, y) };

    static Cenario NovoCenario(string arquivo, string nome, string descricao, Color neblina, Material muro, Material meia, Material chaoA, Material chaoB, Material terreno,
        GameObject[] muros, GameObject[] meiaCob, Marco[] marcos, GameObject[] decoracao)
    {
        var c = ScriptableObject.CreateInstance<Cenario>();
        c.nome = nome;
        c.descricao = descricao;
        c.neblina = neblina;
        c.matMuro = muro;
        c.matMeia = meia;
        c.matChaoA = chaoA;
        c.matChaoB = chaoB;
        c.matTerreno = terreno;
        c.muros = muros ?? new GameObject[0];
        c.meiaCobertura = meiaCob ?? new GameObject[0];
        c.marcos = marcos ?? new Marco[0];
        c.decoracao = decoracao ?? new GameObject[0];
        string caminho = $"Assets/Cenarios/{arquivo}.asset";
        AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(c, caminho);
        return c;
    }

    static Cenario[] CriarCenarios(Dictionary<string, Texture2D> tex, Jogo jogo)
    {
        Directory.CreateDirectory("Assets/Cenarios");

        // ---- Colmeia em Ruínas: prédios residenciais destruídos ----
        var concreto = Lit("Colmeia_Muro", new Color(0.72f, 0.7f, 0.66f), 0f, 0.12f, tex["Concreto"]);
        var ferrugem = Lit("Colmeia_Barricada", new Color(0.62f, 0.5f, 0.4f), 0.45f, 0.35f, tex["Metal"]);
        var chaoColA = Lit("Colmeia_ChaoA", new Color(0.5f, 0.48f, 0.45f), 0f, 0.12f, tex["Concreto"]);
        var chaoColB = Lit("Colmeia_ChaoB", new Color(0.44f, 0.42f, 0.4f), 0f, 0.12f, tex["Concreto"], new Vector2(-1, 1));
        var terraCol = Lit("Colmeia_Terreno", new Color(0.4f, 0.37f, 0.34f), 0f, 0.05f, tex["Terra"], new Vector2(20, 22));
        var barBlind = PrefabPeca("BarricadaBlindada", ferrugem);
        var barDobr = PrefabPeca("BarricadaDobravel", ferrugem);
        var muroBunker = PrefabPeca("MuroBunker", concreto);
        var muroTasar = PrefabPeca("MuroTasar", concreto);
        var caixote = PrefabPeca("Caixote", ferrugem);
        var tanque = PrefabPeca("Tanque", ferrugem);
        var painel = PrefabPeca("Painel", ferrugem);
        var sala = new GameObject[4];
        for (int i = 0; i < 4; i++) sala[i] = PrefabPeca($"HabSala{i + 1}", concreto, false);
        var habU1 = PrefabPeca("HabU1", concreto, false);
        var habU2 = PrefabPeca("HabU2", concreto, false);
        var torre1 = PrefabPeca("HabTorre1", concreto, false);
        var torre2 = PrefabPeca("HabTorre2", concreto, false);
        var colmeia = NovoCenario("ColmeiaEmRuinas", "Colmeia em Ruínas",
            "Quarteirão residencial bombardeado: salas sem teto, torres partidas e barricadas de sucata.",
            new Color(0.17f, 0.15f, 0.14f), concreto, ferrugem, chaoColA, chaoColB, terraCol,
            new[] { barBlind, muroBunker, muroTasar },
            new[] { barDobr, caixote, tanque, painel },
            new[] { Mc(sala[0], 2, 4), Mc(sala[1], 2, 4), Mc(sala[2], 2, 4), Mc(sala[3], 2, 4), Mc(habU1, 3, 3), Mc(habU2, 3, 3), Mc(torre2, 2, 4) },
            new[] { torre1, torre2, habU1, sala[1], sala[3] });

        // ---- Manufatorum: fábrica com chaminés ----
        var aco = Lit("Fabrica_Muro", new Color(0.52f, 0.5f, 0.46f), 0.55f, 0.4f, tex["Metal"]);
        var latao = Lit("Fabrica_Meia", new Color(0.6f, 0.47f, 0.28f), 0.6f, 0.45f, tex["Metal"]);
        var chaoFabA = Lit("Fabrica_ChaoA", new Color(0.46f, 0.46f, 0.5f), 0.25f, 0.3f, tex["Concreto"]);
        var chaoFabB = Lit("Fabrica_ChaoB", new Color(0.4f, 0.4f, 0.44f), 0.25f, 0.3f, tex["Concreto"], new Vector2(-1, 1));
        var terraFab = Lit("Fabrica_Terreno", new Color(0.3f, 0.28f, 0.27f), 0f, 0.05f, tex["Terra"], new Vector2(20, 22));
        var p1 = PrefabPeca("ParedeFabrica1", aco);
        var p2 = PrefabPeca("ParedeFabrica2", aco);
        var p3 = PrefabPeca("ParedeFabrica3", aco);
        var muroBunkerAco = PrefabPeca("MuroBunker", aco);
        var tanqueP = PrefabPeca("TanquePequeno", latao, false);
        var ch1 = PrefabPeca("Chamine1", aco, false);
        var ch2 = PrefabPeca("Chamine2", aco, false);
        var chAlta = PrefabPeca("ChamineAlta", aco, false);
        var chCone = PrefabPeca("ChamineCone", aco, false);
        var tubo = PrefabPeca("Tubulacao", aco, false);
        var valvula = PrefabPeca("Valvula", aco, false);
        var coluna = PrefabPeca("ColunaFabrica", aco, false);
        var pilar = PrefabPeca("PilarBunker", aco, false);
        var fabrica = NovoCenario("Manufatorum", "Manufatorum",
            "Pátio de uma forja industrial: paredes com relevos mecânicos, tanques e chaminés fumegantes.",
            new Color(0.2f, 0.16f, 0.13f), aco, latao, chaoFabA, chaoFabB, terraFab,
            new[] { p1, p2, p3, muroBunkerAco },
            new[] { tanque, caixote, painel, barDobr },
            new[] { Mc(ch1, 2, 2), Mc(ch2, 2, 2), Mc(chAlta, 2, 2), Mc(tubo, 2, 2), Mc(valvula, 2, 2), Mc(tanqueP, 2, 1), Mc(pilar, 2, 2) },
            new[] { chAlta, ch1, ch2, chCone, tubo, coluna, tanqueP });
        fabrica.escalaDecoracao = 2f;
        EditorUtility.SetDirty(fabrica);

        // ---- Catedral Gótica: capelas e torres de pedra ----
        var pedra = Lit("Catedral_Muro", new Color(0.82f, 0.8f, 0.76f), 0f, 0.15f, tex["Pedra"]);
        var pedraEsc = Lit("Catedral_Meia", new Color(0.6f, 0.57f, 0.52f), 0f, 0.12f, tex["Pedra"]);
        var chaoCatA = Lit("Catedral_ChaoA", new Color(0.66f, 0.64f, 0.6f), 0f, 0.2f, tex["Pedra"]);
        var chaoCatB = Lit("Catedral_ChaoB", new Color(0.58f, 0.56f, 0.53f), 0f, 0.2f, tex["Pedra"], new Vector2(-1, 1));
        var terraCat = Lit("Catedral_Terreno", new Color(0.36f, 0.38f, 0.33f), 0f, 0.05f, tex["Terra"], new Vector2(20, 22));
        var muroTasarP = PrefabPeca("MuroTasar", pedra);
        var muroBunkerP = PrefabPeca("MuroBunker", pedra);
        var barBlindP = PrefabPeca("BarricadaBlindada", pedra);
        var capP = PrefabPeca("CapelaPequena", pedra, false);
        var capG = PrefabPeca("CapelaGrande", pedra, false);
        var torreT = PrefabPeca("TorreTasar", pedra, false);
        var coroa = PrefabPeca("CoroaGotica", pedra, false);
        var catedral = NovoCenario("CatedralGotica", "Catedral Gótica",
            "Jardim de um santuário em ruínas: capelas com estátuas, torres e muros com brasões.",
            new Color(0.17f, 0.19f, 0.24f), pedra, pedraEsc, chaoCatA, chaoCatB, terraCat,
            new[] { muroTasarP, muroBunkerP, barBlindP },
            new[] { barDobr, caixote },
            new[] { Mc(capP, 2, 2), Mc(capG, 3, 3), Mc(torreT, 2, 2), Mc(coroa, 3, 3) },
            new[] { capG, capP, torreT, coroa });
        catedral.escalaDecoracao = 1.2f;
        EditorUtility.SetDirty(catedral);

        // ---- Clássico: os blocos originais ----
        var classico = NovoCenario("Classico", "Clássico",
            "O tabuleiro original: muros de pedra, caixas e sacos de areia.",
            new Color(0.16f, 0.17f, 0.2f), null, null, null, null, null, null, null, null, null);

        return new[] { colmeia, fabrica, catedral, classico };
    }

    // ---------- cena do menu ----------

    static void MontarMenu(Equipe[] equipes, Cenario[] cenarios, Dictionary<string, Texture2D> tex)
    {
        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var luz = new GameObject("Sol").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(1f, 0.94f, 0.85f);
        luz.intensity = 1.5f;
        luz.shadows = LightShadows.Soft;
        luz.transform.rotation = Quaternion.Euler(40, -30, 0);
        var contra = new GameObject("Contraluz").AddComponent<Light>();
        contra.type = LightType.Directional;
        contra.color = new Color(0.6f, 0.75f, 1f);
        contra.intensity = 0.8f;
        contra.transform.rotation = Quaternion.Euler(20, 160, 0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.42f, 0.47f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.08f, 0.09f, 0.11f);
        RenderSettings.fogStartDistance = 12;
        RenderSettings.fogEndDistance = 30;
        RenderSettings.skybox = null;

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.fieldOfView = 36;
        camGo.transform.position = new Vector3(0, 2.2f, -7f);
        camGo.transform.LookAt(new Vector3(0, 1.0f, 1.5f));
        camGo.AddComponent<AudioListener>();
        var dados = camGo.AddComponent<UniversalAdditionalCameraData>();
        dados.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

        var chao = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(chao.GetComponent<Collider>());
        chao.name = "Chao";
        chao.transform.localScale = new Vector3(30, 0.05f, 30);
        chao.transform.position = new Vector3(0, -0.05f, 6);
        chao.GetComponent<Renderer>().sharedMaterial = Lit("Menu_Chao", new Color(0.3f, 0.3f, 0.32f), 0.2f, 0.25f, tex["Concreto"], new Vector2(10, 10));

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var menu = new GameObject("Menu").AddComponent<Menu>();
        menu.equipes = equipes;
        menu.cenarios = cenarios;
        menu.cam = cam;
        menu.matPedestalAzul = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/BaseAzul.mat");
        menu.matPedestalVermelho = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/BaseVermelha.mat");
        EditorSceneManager.SaveScene(cena, "Assets/Scenes/Menu.unity");
    }
}
