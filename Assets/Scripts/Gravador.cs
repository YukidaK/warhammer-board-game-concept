using System.IO;
using UnityEngine;

// Grava os quadros da câmera em JPG (usado para montar o vídeo de apresentação).
// Time.captureFramerate faz o jogo andar 1/30 s por quadro, independente da velocidade do PC.
public class Gravador : MonoBehaviour
{
    public static string pasta;
    public static int quadroAtual = -1;
    public Camera cam;
    RenderTexture rt;
    Texture2D tex;
    int quadro;

    void Start()
    {
        Directory.CreateDirectory(pasta);
        Time.captureFramerate = 30;
        rt = cam.targetTexture;
        tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
    }

    void LateUpdate()
    {
        cam.Render();
        var ant = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = ant;
        quadroAtual = quadro;
        File.WriteAllBytes(Path.Combine(pasta, $"q{quadro++:D5}.jpg"), tex.EncodeToJPG(88));
    }

    void OnDestroy()
    {
        Time.captureFramerate = 0;
        quadroAtual = -1;
    }
}
