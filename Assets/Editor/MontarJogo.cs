using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Monta tudo de forma automática: importa os modelos, cria os materiais, a cena da batalha e a build.
// Uso: Unity -batchmode -executeMethod MontarJogo.Executar   (ou menu Kill Team > Montar cena)
public static partial class MontarJogo
{
    static readonly string[] Azuis = { "Sergeant", "Trooper", "Sniper", "Medic_trooper" };
    static readonly string[] Vermelhos = { "Trooper", "Plasma", "Melta", "Volleygun", "Demo", "Recon", "Vox" };

    [MenuItem("Kill Team/Montar cena")]
    public static void Executar()
    {
        ConfigurarModelos();
        Directory.CreateDirectory("Assets/Materiais");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Texturas");
        Directory.CreateDirectory("Assets/Sprites");
        Directory.CreateDirectory("Assets/Animacoes");
        Directory.CreateDirectory("Assets/Prefabs");
        var tex = GerarTexturas();

        var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // luz e ambiente
        var luz = new GameObject("Sol").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(1f, 0.94f, 0.85f);
        luz.intensity = 1.6f;
        luz.shadows = LightShadows.Soft;
        luz.shadowStrength = 0.8f;
        luz.transform.rotation = Quaternion.Euler(52, -35, 0);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.7f);
        RenderSettings.ambientEquatorColor = new Color(0.38f, 0.38f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.16f, 0.15f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.16f, 0.17f, 0.2f);
        RenderSettings.fogStartDistance = 30;
        RenderSettings.fogEndDistance = 70;
        RenderSettings.skybox = null;

        // câmera tática
        var rig = new GameObject("CameraTatica");
        rig.transform.position = new Vector3(11, 0, 4);
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.fieldOfView = 45;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 200;
        camGo.AddComponent<AudioListener>();
        var dados = camGo.AddComponent<UniversalAdditionalCameraData>();
        dados.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        dados.renderPostProcessing = false;
        var ct = rig.AddComponent<CameraTatica>();
        ct.cam = cam;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // controlador do jogo
        var jogoGo = new GameObject("Jogo");
        var jogo = jogoGo.AddComponent<Jogo>();
        var hud = jogoGo.AddComponent<Hud>();
        jogo.pontos = jogoGo.AddComponent<Pontuacao>();
        jogo.cam = cam;
        jogo.camTatica = ct;
        jogo.hud = hud;

        jogo.matAzul = Lit("Azul", new Color(0.16f, 0.36f, 0.85f), 0.35f, 0.55f);
        jogo.matVermelho = Lit("Vermelho", new Color(0.78f, 0.13f, 0.12f), 0.35f, 0.55f);
        jogo.matBaseAzul = Lit("BaseAzul", new Color(0.3f, 0.45f, 0.85f), 0.5f, 0.45f, tex["Metal"]);
        jogo.matBaseVermelha = Lit("BaseVermelha", new Color(0.85f, 0.3f, 0.25f), 0.5f, 0.45f, tex["Metal"]);
        jogo.matChaoA = Lit("ChaoA", new Color(0.62f, 0.61f, 0.58f), 0f, 0.15f, tex["Concreto"]);
        jogo.matChaoB = Lit("ChaoB", new Color(0.53f, 0.52f, 0.5f), 0f, 0.15f, tex["Concreto"], new Vector2(-1, 1));
        jogo.matTerreno = Lit("Terreno", new Color(0.55f, 0.52f, 0.48f), 0f, 0.05f, tex["Terra"], new Vector2(14, 16));
        jogo.matMuro = Lit("Muro", new Color(0.88f, 0.86f, 0.83f), 0f, 0.1f, tex["Pedra"]);
        jogo.matCaixa = Lit("Caixa", Color.white, 0.05f, 0.25f, tex["Madeira"]);
        jogo.matSaco = Lit("Saco", Color.white, 0f, 0.05f, tex["Saco"]);
        var matRocha = Lit("Rocha", new Color(0.6f, 0.58f, 0.55f), 0f, 0.1f, tex["Rocha"]);
        jogo.matMover1 = Unlit("Mover1", new Color(0.2f, 0.55f, 1f, 0.26f));
        jogo.matMover2 = Unlit("Mover2", new Color(1f, 0.8f, 0.2f, 0.2f));
        jogo.matCursor = Unlit("Cursor", new Color(1f, 1f, 1f, 0.75f));
        jogo.matEscudo = Unlit("Escudo", new Color(0.35f, 0.75f, 1f, 0.85f));
        jogo.matAnel = Unlit("Anel", new Color(1f, 0.85f, 0.25f, 1f));
        jogo.matTraco = Unlit("Traco", new Color(1f, 0.9f, 0.45f, 1f));
        var matHalo = Aditivo("Halo", "Universal Render Pipeline/Unlit", new Color(0.45f, 0.75f, 1f, 0.9f), tex["Brilho"]);
        var matFaisca = Aditivo("Faisca", "Universal Render Pipeline/Particles/Unlit", Color.white, tex["Particula"]);
        var matFumaca = Particula("Fumaca", tex["Particula"]);

        // prefabs do cenário (BoxCollider + material texturizado)
        jogo.prefCasa = PrefabBloco("Casa", jogo.matChaoA);
        jogo.prefMuro = PrefabBloco("Muro", jogo.matMuro);
        jogo.prefCaixa = PrefabBloco("Caixa", jogo.matCaixa);
        jogo.prefSaco = PrefabBloco("SacoDeAreia", jogo.matSaco);
        jogo.prefTerreno = PrefabBloco("Terreno", jogo.matTerreno);
        jogo.prefPedra = PrefabBloco("Pedra", matRocha);
        jogo.prefUnidade = PrefabUnidade(jogo.matBaseAzul);
        jogo.prefFxDisparo = PrefabDisparo(matFaisca);
        jogo.prefFxImpacto = PrefabImpacto(matFaisca, matFumaca);
        jogo.prefProjetil = PrefabProjetil(jogo.matTraco, matFaisca);
        jogo.prefExplosao = PrefabExplosao();
        jogo.prefHalo = PrefabHalo(matHalo);

        // times e cenários feitos com os modelos novos
        jogo.equipes = CriarEquipes();
        jogo.cenarios = CriarCenarios(tex, jogo);

        EditorSceneManager.SaveScene(cena, "Assets/Scenes/Batalha.unity");
        if (File.Exists("Assets/Scenes/SampleScene.unity")) AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
        MontarMenu(jogo.equipes, jogo.cenarios, tex);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Menu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Batalha.unity", true),
        };

        PlayerSettings.productName = "Kill Team Tático";
        PlayerSettings.companyName = "Vinicius";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        AssetDatabase.SaveAssets();
        Debug.Log("[Montar] cenas Menu e Batalha montadas");
    }

    [MenuItem("Kill Team/Gerar build Linux")]
    public static void Build()
    {
        var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Batalha.unity" },
            locationPathName = "Build/KillTeamTatico.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        });
        Debug.Log($"[Build] {r.summary.result} {r.summary.totalSize / 1048576} MB, {r.summary.totalErrors} erros");
        if (Application.isBatchMode) EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    static void ConfigurarModelos()
    {
        foreach (var caminho in Directory.GetFiles("Assets/Models", "*.obj", SearchOption.AllDirectories))
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(caminho.Replace('\\', '/'));
            if (imp == null) { AssetDatabase.ImportAsset(caminho); imp = (ModelImporter)AssetImporter.GetAtPath(caminho); }
            imp.globalScale = 1f;
            imp.useFileScale = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.addCollider = false;
            imp.isReadable = false;
            imp.meshCompression = ModelImporterMeshCompression.Off;
            imp.SaveAndReimport();
        }
        int tris = 0;
        foreach (var nome in Azuis.Concat(Vermelhos).Distinct())
        {
            var go = Modelo(nome);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                tris += mf.sharedMesh.triangles.Length / 3;
                Debug.Log($"[Montar] {nome}: {mf.sharedMesh.vertexCount} vértices, bounds {mf.sharedMesh.bounds}");
            }
        }
        Debug.Log($"[Montar] {tris} triângulos nos modelos");
    }

    static GameObject Modelo(string nome) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Models/{nome}.obj");

    static Material Lit(string nome, Color cor, float metal, float liso, Texture2D textura = null, Vector2? tiling = null)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", cor);
        if (textura != null)
        {
            m.SetTexture("_BaseMap", textura);
            m.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
        }
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Smoothness", liso);
        return Salvar(m, nome);
    }

    static Material Unlit(string nome, Color cor)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", cor);
        if (cor.a < 1f)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
        }
        return Salvar(m, nome);
    }

    static Material Salvar(Material m, string nome)
    {
        string caminho = $"Assets/Materiais/{nome}.mat";
        AssetDatabase.DeleteAsset(caminho);
        AssetDatabase.CreateAsset(m, caminho);
        return m;
    }
}