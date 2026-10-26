using UnityEngine;

// Câmera de cima, inclinada: WASD/setas ou borda da tela movem, Q/E giram 90°, roda do mouse dá zoom.
public class CameraTatica : MonoBehaviour
{
    public Camera cam;
    public float inclinacao = 60f;
    public float zoom = 20f, zoomMin = 7f, zoomMax = 34f;
    public float velocidade = 14f;
    public bool bordaDaTela = true;
    public Vector2 limiteMin, limiteMax;

    float giro, giroAlvo;
    float zoomAlvo;
    Vector3 foco;
    bool focando;

    void Start()
    {
        zoomAlvo = zoom;
        Aplicar();
    }

    public void Focar(Vector3 p)
    {
        foco = new Vector3(p.x, 0, p.z);
        focando = true;
    }

    public void PosicionarJa(Vector3 p)
    {
        transform.position = new Vector3(p.x, 0, p.z);
        focando = false;
        Aplicar();
    }

    void Update()
    {
        var mover = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        if (bordaDaTela && Application.isFocused && !Application.isEditor)
        {
            var m = Input.mousePosition;
            if (m.x >= 0 && m.y >= 0 && m.x <= Screen.width && m.y <= Screen.height)
            {
                if (m.x < 6) mover.x = -1;
                if (m.x > Screen.width - 6) mover.x = 1;
                if (m.y < 6) mover.z = -1;
                if (m.y > Screen.height - 6) mover.z = 1;
            }
        }
        if (mover.sqrMagnitude > 0.01f)
        {
            focando = false;
            transform.position += Quaternion.Euler(0, giro, 0) * mover.normalized * velocidade * (zoom / 17f) * Time.unscaledDeltaTime;
        }
        else if (focando)
        {
            transform.position = Vector3.Lerp(transform.position, foco, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
            if ((transform.position - foco).sqrMagnitude < 0.0005f) focando = false;
        }

        var p = transform.position;
        transform.position = new Vector3(Mathf.Clamp(p.x, limiteMin.x, limiteMax.x), 0, Mathf.Clamp(p.z, limiteMin.y, limiteMax.y));

        if (Input.GetKeyDown(KeyCode.Q)) giroAlvo += 90f;
        if (Input.GetKeyDown(KeyCode.E)) giroAlvo -= 90f;
        giro = Mathf.Lerp(giro, giroAlvo, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));

        float roda = Input.mouseScrollDelta.y;
        if (Mathf.Abs(roda) > 0.01f) zoomAlvo = Mathf.Clamp(zoomAlvo - roda * 1.8f, zoomMin, zoomMax);
        zoom = Mathf.Lerp(zoom, zoomAlvo, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));

        Aplicar();
    }

    public void ZoomSuave(float z) => zoomAlvo = Mathf.Clamp(z, zoomMin, zoomMax);

    public void DefinirZoom(float z)
    {
        zoom = zoomAlvo = Mathf.Clamp(z, zoomMin, zoomMax);
        Aplicar();
    }

    public void Girar(float graus)
    {
        giroAlvo += graus;
        giro = giroAlvo;
        Aplicar();
    }

    void Aplicar()
    {
        var rot = Quaternion.Euler(inclinacao, giro, 0);
        cam.transform.SetPositionAndRotation(transform.position - rot * Vector3.forward * zoom, rot);
    }
}
