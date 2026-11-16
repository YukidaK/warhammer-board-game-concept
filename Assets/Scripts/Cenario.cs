using UnityEngine;

// Peça grande que ocupa um retângulo de casas (ex.: capela gótica 3x3, chaminé 2x2).
[System.Serializable]
public class Marco
{
    public GameObject prefab;
    public Vector2Int tamanho = new(2, 2);
}

// Tema do campo de batalha: quais modelos viram muro, meia cobertura, marcos e decoração.
// Sem peças (tema Clássico) o gerador usa os blocos simples.
[CreateAssetMenu(menuName = "Kill Team/Cenario")]
public class Cenario : ScriptableObject
{
    public string nome;
    [TextArea] public string descricao;
    public GameObject[] muros;
    public GameObject[] meiaCobertura;
    public Marco[] marcos;
    public GameObject[] decoracao;
    public Material matMuro, matMeia, matChaoA, matChaoB, matTerreno;
    [Tooltip("escala das peças de decoração em volta do tabuleiro")] public float escalaDecoracao = 1f;
    public Color neblina = new(0.16f, 0.17f, 0.2f);
    public Sprite retrato;
}
