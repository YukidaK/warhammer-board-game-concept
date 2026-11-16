using UnityEngine;

// Halo de seleção: brilho aditivo + luz pontual que pulsam sobre o soldado escolhido.
public class HaloPulsante : MonoBehaviour
{
    public Light luz;
    public float escalaBase = 1.6f;
    public float intensidadeBase = 2.5f;

    void Update()
    {
        float k = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f;
        transform.localScale = Vector3.one * escalaBase * k;
        if (luz != null) luz.intensity = intensidadeBase * k;
    }
}
