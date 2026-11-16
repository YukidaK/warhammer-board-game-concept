using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ao abrir o projeto no editor sem nenhuma cena (Untitled), abre a cena do Menu,
// para o botão Play já começar o jogo pela escolha dos times.
[InitializeOnLoad]
public static class AbrirMenu
{
    const string Caminho = "Assets/Scenes/Menu.unity";

    static AbrirMenu()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Caminho) == null) return;
            var ativa = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(ativa.path) && !ativa.isDirty)
                EditorSceneManager.OpenScene(Caminho);
        };
    }
}
