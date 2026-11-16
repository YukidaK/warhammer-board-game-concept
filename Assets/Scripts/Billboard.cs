using UnityEngine;

// Mantém o objeto (sprite ou halo) sempre virado para a câmera.
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }
}
