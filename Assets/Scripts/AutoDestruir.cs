using UnityEngine;

// Remove o efeito da cena depois de alguns segundos.
public class AutoDestruir : MonoBehaviour
{
    public float segundos = 1f;

    void Start() => Destroy(gameObject, segundos);
}
