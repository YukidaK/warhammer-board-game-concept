using UnityEditor;
using UnityEditor.SceneManagement;

// O Play sempre inicia pelo menu (escolha de times e cenário), mesmo quando outra cena está aberta no editor.
[InitializeOnLoad]
public static class CenaInicial
{
    static CenaInicial()
    {
        EditorApplication.delayCall += () =>
        {
            EditorSceneManager.playModeStartScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Menu.unity");
        };
    }
}
