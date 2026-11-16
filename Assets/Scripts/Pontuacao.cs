using UnityEngine;

// Pontuação da partida (Score). Cada ação da equipe azul dispara o evento AoPontuar,
// que a HUD escuta para atualizar o placar e mostrar a mensagem na tela.
public class Pontuacao : MonoBehaviour
{
    public const int Abate = 100, Critico = 50, Flanco = 25, Cura = 30, Reacao = 40, SoldadoPerdido = -150;
    public const int BonusPorSobrevivente = 50, BonusPorTurnoPoupado = 20, TurnosReferencia = 12;

    public static readonly (string acao, int pontos)[] Legenda =
    {
        ("Abater inimigo", Abate),
        ("Acerto crítico", Critico),
        ("Acertar flanqueado", Flanco),
        ("Tiro de vigilância", Reacao),
        ("Curar aliado", Cura),
        ("Soldado abatido", SoldadoPerdido),
        ("Vitória: por sobrevivente", BonusPorSobrevivente),
        ("Vitória: por turno poupado", BonusPorTurnoPoupado),
    };

    public int Total { get; private set; }
    public event System.Action<int, string, Vector3> AoPontuar;

    public void Adicionar(int pontos, string motivo, Vector3 onde)
    {
        Total = Mathf.Max(0, Total + pontos);
        AoPontuar?.Invoke(pontos, motivo, onde);
        Debug.Log($"[Pontos] {(pontos >= 0 ? "+" : "")}{pontos} {motivo} = {Total}");
    }
}
