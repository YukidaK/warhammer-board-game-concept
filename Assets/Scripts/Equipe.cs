using UnityEngine;

public enum Papel { Fuzileiro, Lider, Atirador, Medico, Pesado, CorpoACorpo }

// Um integrante do time: modelo 3D e atributos de jogo.
[System.Serializable]
public class Membro
{
    public string nome;
    public string classe;
    public Papel papel;
    public GameObject modelo;
    public int hp = 6, mira = 65, danoMin = 2, danoMax = 4;
    public float mobilidade = 6f;
    [Tooltip("multiplica a escala do time (ex.: squigs menores)")] public float escala = 1f;
    [Tooltip("giro em Y para o modelo olhar para a frente (+Z)")] public float giro;
}

// Um time jogável (facção). Criado pelo editor em Assets/Equipes e escolhido no menu.
[CreateAssetMenu(menuName = "Kill Team/Equipe")]
public class Equipe : ScriptableObject
{
    public string nome;
    [TextArea] public string descricao;
    public Color cor = Color.white;
    public Material material;
    public float escala = 1f;
    [Tooltip("quantos entram em campo quando este time é o inimigo")] public int qtdInimigos = 6;
    public Sprite retrato;
    [Tooltip("os 4 primeiros formam o esquadrão do jogador")] public Membro[] membros;
}
