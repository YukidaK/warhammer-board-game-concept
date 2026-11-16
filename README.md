# warhammer-board-game-concept

Jogo tático 3D por turnos, no estilo XCOM. No menu você escolhe o seu time (4 soldados, base azul), o time adversário (até 6, base vermelha) e o cenário.

## Times

| Time | Modelos | Estilo |
|---|---|---|
| Harakoni Kill Team | Harakoni (guarda imperial) | equilibrado |
| Anjos Sombrios | First Legion "Gloomy Angels" | fuzileiros resistentes; como inimigo vêm 5 |
| Vigília da Morte | SM Squad Watch | veteranos com armas variadas; como inimigo vêm 5 |
| Bando Kroot | Kroot Kinband | ágeis, pouca vida |
| Pox Wreckas | Unclean Siege Platoon (orks, grots e squigs) | brutos e imprecisos |
| Ogros Renegados | Ice Guard Ogres, Crabby Chaotic Ogre e Chaos Cultist | muita vida, lentos; como inimigo vêm 4 |

Do lado inimigo as unidades têm cerca de 70% da vida e −8 de mira, para equilibrar 4 contra até 6.

## Cenários

| Cenário | Peças |
|---|---|
| Colmeia em Ruínas | hab blocks destruídos, barricadas, caixas e tanques |
| Manufatorum | paredes do Manufactorum, chaminés (Smokestack), tanques |
| Catedral Gótica | capelas da Gothick Folly, torre e muro de Tasar, coroa gótica |
| Clássico | os blocos originais |

O mapa continua sendo sorteado a cada partida. Muros, barricadas e marcos grandes (2×2, 3×3, 2×4) usam os modelos do cenário escolhido.

### Modelos 3D

Os STLs baixados (em `~/Downloads/kill_team`) e os OBJ originais dos Harakoni (`~/Downloads/Harakoni_KT.zip`) foram convertidos com `tools/decimar_qem.py` (requer `pip install numpy pyfqmr`). Ele simplifica a malha por erro quádrico, o mesmo método do Decimate do Blender, para ~30 mil triângulos por figura, gira de Z para Y para cima e gera normais com quinas vivas e UVs. A lista do que foi convertido está em `tools/lista_modelos.txt`. O `tools/render.py` gera pranchas de pré-visualização. O `tools/decimar.py` antigo (vertex clustering) não é mais usado.

A interface (menu e HUD) usa a fonte Barlow Condensed (licença SIL OFL, em `Assets/Resources/Fontes`) e o estilo definido em `Assets/Scripts/UiEstilo.cs`.

## Como iniciar o jogo

### Opção 1: executável pronto (Linux)

```bash
cd ~/Downloads/warhammer-board-game-concept/Build
./KillTeamTatico.x86_64
```

Se aparecer "permissão negada", rode antes:

```bash
chmod +x KillTeamTatico.x86_64
```

### Opção 2: pelo editor da Unity

1. Abra o projeto na **Unity 6000.5.8f1**, pelo Unity Hub (**Add → pasta do projeto**) ou pelo terminal:
   ```bash
   ~/Unity/Hub/Editor/6000.5.8f1/Editor/Unity -projectPath ~/Downloads/warhammer-board-game-concept
   ```
2. Abra a cena `Assets/Scenes/Menu.unity` (ou `Batalha.unity` direto, com Harakoni contra Harakoni).
3. Aperte **Play**.

### Gerar o executável de novo

Se a pasta `Build/` não existir (ela fica fora do git), use o menu **Kill Team → Gerar build Linux** dentro da Unity, ou rode no terminal:

```bash
~/Unity/Hub/Editor/6000.5.8f1/Editor/Unity -batchmode -quit \
  -projectPath ~/Downloads/warhammer-board-game-concept \
  -executeMethod MontarJogo.Build
```

O executável sai em `Build/KillTeamTatico.x86_64`.

> Se as cenas estiverem faltando ou quebradas, use **Kill Team → Montar cena** para recriar o menu, a batalha, os times (`Assets/Equipes`) e os cenários (`Assets/Cenarios`).

## Objetivo

Eliminar todos os inimigos vermelhos. Você perde se os 4 soldados azuis forem abatidos.
O mapa é gerado automaticamente e muda a cada partida.

## Controles

| Tecla / Mouse | Ação |
|---|---|
| Clique no soldado, **Tab** ou cartões à esquerda | Escolher soldado |
| **1** | Mover (casa azul = 1 ação, amarela = 2 ações) |
| **2** | Atirar (mostra a % de acerto em cada inimigo) |
| **3** | Vigilância (atira no primeiro inimigo que se mexer) |
| **4** | Defender (fica mais difícil de ser acertado) |
| **5** | Curar (só o médico, 2 usos) |
| **Clique esquerdo** | Confirmar destino ou alvo |
| **Botão direito / Esc** | Cancelar |
| **Enter** | Encerrar o turno |
| **WASD / setas** | Mover a câmera |
| **Q / E** | Girar a câmera |
| **Roda do mouse** | Zoom |
| **L** | Mostrar/esconder a legenda de pontos |
| **F5** | Novo mapa |
| **R** (no fim da partida) | Jogar de novo |
| **M / Esc** (no fim da partida) | Voltar ao menu |
| **Enter / Esc** (no menu) | Iniciar / Sair |

## Pontuação

Abater inimigo +100, crítico +50, acertar flanqueado +25, tiro de vigilância +40, cura +30, soldado seu abatido −150.
Na vitória: +50 por sobrevivente e +20 por turno poupado (antes do turno 12).

## Dicas

- Cada soldado tem **2 ações** por turno. Atirar, vigiar, defender e curar encerram o turno do soldado.
- Os escudos que aparecem ao passar o mouse numa casa mostram a cobertura dela: **baixo = meia cobertura** (−20% para te acertar) e **alto = cobertura total** (−40%).
- Inimigo sem cobertura do seu lado está **flanqueado**: a chance de acerto sobe e a de crítico também.

## Mapa do código

`MAPA_DO_CODIGO.md` (e `MAPA_DO_CODIGO.html`) mostram em diagramas Mermaid onde está o código de cada parte do jogo. Cada caixa abre o arquivo no VS Code, já na linha da função. Para gerar de novo: `python3 tools/gerar_mapa.py`.
