using UnityEditor;
using UnityEditor.SceneManagement;

// O Play sempre inicia a batalha, mesmo quando outra cena está aberta no editor.
[InitializeOnLoad]
public static class CenaInicial
{
    static CenaInicial()
    {
        EditorApplication.delayCall += () =>
        {
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Batalha.unity");
        };
    }
}
