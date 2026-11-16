using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Roteiro para os prints do editor Unity (com interface). Em cada etapa seleciona um asset ou objeto,
// mostra a janela certa e pede a captura de tela por arquivo (Capturas/editor/pedido.txt);
// um script externo (grim) tira o print e responde em Capturas/editor/feito.txt.
public static class PrintsEditor
{
    const string Pasta = "Capturas/editor";
    static int passo;
    static double marca;
    static bool esperando;
    static GameObject temporario;
    static bool soRefazer;

    // refaz só os prints do prefab Unidade e da janela Animation
    public static void Refazer()
    {
        Iniciar();
        soRefazer = true;
        passo = 1;
    }

    [MenuItem("Kill Team/Prints do editor")]
    public static void Iniciar()
    {
        Directory.CreateDirectory(Pasta);
        File.Delete($"{Pasta}/pedido.txt");
        File.Delete($"{Pasta}/feito.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/Batalha.unity");
        // sem domain reload ao dar Play, para o roteiro continuar rodando dentro do play mode
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        passo = 0;
        marca = EditorApplication.timeSinceStartup + 12;   // espera o editor terminar de abrir
        EditorApplication.update -= Passo;
        EditorApplication.update += Passo;
    }

    static T Asset<T>(string caminho) where T : Object => AssetDatabase.LoadAssetAtPath<T>(caminho);

    static void Mostrar(Object o)
    {
        Selection.activeObject = o;
        EditorGUIUtility.PingObject(o);
    }

    static void Pedir(string nome)
    {
        File.WriteAllText($"{Pasta}/pedido.txt", nome);
        esperando = true;
        Debug.Log($"[PrintsEditor] pedido {nome}");
    }

    static void Passo()
    {
        if (EditorApplication.timeSinceStartup < marca) return;
        if (esperando)
        {
            if (!File.Exists($"{Pasta}/feito.txt")) return;
            File.Delete($"{Pasta}/feito.txt");
            esperando = false;
            passo++;
            if (soRefazer && passo == 2) passo = 6;
            if (soRefazer && passo == 7) passo = 10;
            marca = EditorApplication.timeSinceStartup + 1.5;
            return;
        }

        switch (passo)
        {
            case 0:   // cena aberta, objeto Jogo com os scripts e as referências aos prefabs
                EditorWindow.FocusWindowIfItsOpen(typeof(SceneView));
                Mostrar(GameObject.Find("Jogo"));
                SceneView.lastActiveSceneView?.FrameSelected();
                Prepara("e01_cena_jogo");
                break;
            case 1:
                var unidade = Asset<GameObject>("Assets/Prefabs/Unidade.prefab");
                // recolhe o script no Inspector para o Collider e o Rigidbody caberem no print
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(unidade.GetComponent<Unidade>(), false);
                Mostrar(unidade);
                ActiveEditorTracker.sharedTracker.ForceRebuild();
                Prepara("e02_prefab_unidade");
                break;
            case 2:
                Mostrar(Asset<GameObject>("Assets/Prefabs/Projetil.prefab"));
                Prepara("e03_prefab_projetil");
                break;
            case 3:
                Mostrar(Asset<GameObject>("Assets/Prefabs/FxImpacto.prefab"));
                Prepara("e04_particulas");
                break;
            case 4:
                Mostrar(Asset<Material>("Assets/Materiais/Caixa.mat"));
                Prepara("e05_material");
                break;
            case 5:
                Mostrar(Asset<GameObject>("Assets/Prefabs/Halo.prefab"));
                Prepara("e06_halo");
                break;
            case 6:   // janela Animation com o clip de sprites da explosão
                temporario = (GameObject)PrefabUtility.InstantiatePrefab(Asset<GameObject>("Assets/Prefabs/Explosao.prefab"));
                temporario.transform.position = new Vector3(11, 1.5f, 4);
                Selection.activeGameObject = temporario;
                var tipo = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationWindow");
                // abre a janela Animation como aba ao lado da Scene view
                var getWindow = System.Array.Find(typeof(EditorWindow).GetMethods(), m => m.Name == "GetWindow" && m.IsGenericMethod &&
                    m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[1].ParameterType == typeof(System.Type[]));
                getWindow.MakeGenericMethod(tipo).Invoke(null, new object[] { "Animation", new[] { typeof(SceneView) } });
                SceneView.lastActiveSceneView?.FrameSelected();
                Prepara("e07_animacao", 3);
                break;
            case 7:   // play mode: mapa gerado, unidade selecionada com Rigidbody/Collider no Inspector
                if (temporario != null) Object.DestroyImmediate(temporario);
                var anim = Resources.FindObjectsOfTypeAll(typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationWindow"));
                foreach (var w in anim) ((EditorWindow)w).Close();
                EditorApplication.EnterPlaymode();
                passo = 8;
                marca = EditorApplication.timeSinceStartup + 10;
                break;
            case 8:
                Mostrar(GameObject.Find("Sgt. Varn"));
                EditorWindow.FocusWindowIfItsOpen(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
                Prepara("e08_play_game");
                break;
            case 9:
                var muro = GameObject.Find("Mapa/Muro");
                Mostrar(muro != null ? muro : GameObject.Find("Mapa"));
                EditorWindow.FocusWindowIfItsOpen(typeof(SceneView));
                var sv = SceneView.lastActiveSceneView;
                if (sv != null)
                {
                    sv.LookAt(new Vector3(11, 0, 15), Quaternion.Euler(50, 30, 0), 18f);
                    sv.Repaint();
                }
                Prepara("e09_play_cena", 3);
                break;
            case 10:
                EditorApplication.ExitPlaymode();
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorApplication.update -= Passo;
                File.WriteAllText($"{Pasta}/fim.txt", "ok");
                Debug.Log("[PrintsEditor] fim");
                break;
        }
    }

    // dá um tempo para as janelas redesenharem antes de pedir o print
    static string pendente;
    static void Prepara(string nome, double segundos = 2)
    {
        pendente = nome;
        marca = EditorApplication.timeSinceStartup + segundos;
        EditorApplication.update -= Pedido;
        EditorApplication.update += Pedido;
        EditorApplication.update -= Passo;
    }

    static void Pedido()
    {
        if (EditorApplication.timeSinceStartup < marca) return;
        EditorApplication.update -= Pedido;
        EditorApplication.update += Passo;
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Pedir(pendente);
    }
}
