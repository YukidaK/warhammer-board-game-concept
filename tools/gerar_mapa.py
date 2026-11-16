#!/usr/bin/env python3
"""Gera MAPA_DO_CODIGO.md e MAPA_DO_CODIGO.html na raiz do projeto.

Cada caixa dos diagramas Mermaid é clicável e abre o arquivo no VS Code, já
na linha da função (links vscode://file/<caminho>:<linha>). Os caminhos são
absolutos e calculados a partir da pasta do projeto, então basta rodar de novo
se o projeto mudar de lugar:

    python3 tools/gerar_mapa.py
"""
import html
import os
import re
import sys

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def esquema_do_vscode():
    """Protocolo de URL do VS Code instalado: 'vscode' no oficial, 'code-oss' no build livre
    (Arch, por exemplo). Pode ser forçado com: python3 tools/gerar_mapa.py vscode"""
    if len(sys.argv) > 1:
        return sys.argv[1]
    import glob
    import json
    for p in glob.glob("/usr/lib/code*/product.json") + glob.glob("/usr/share/code*/resources/app/product.json") \
            + glob.glob("/opt/visual-studio-code/resources/app/product.json"):
        try:
            return json.load(open(p)).get("urlProtocol", "vscode")
        except (OSError, ValueError):
            pass
    return "vscode"


ESQUEMA = esquema_do_vscode()


def linha(arquivo, padrao):
    """Linha (1-based) da primeira ocorrência do padrão; 1 se não achar."""
    try:
        with open(os.path.join(RAIZ, arquivo), encoding="utf-8") as f:
            for i, l in enumerate(f, 1):
                if re.search(padrao, l):
                    return i
    except FileNotFoundError:
        pass
    return 1


# id: (rótulo, arquivo, regex da linha ou None)
N = {
    # ---------- cenas e fluxo ----------
    "menu": ("Menu.cs<br/>menu inicial", "Assets/Scripts/Menu.cs", r"class Menu"),
    "menuIni": ("Iniciar()<br/>carrega a Batalha", "Assets/Scripts/Menu.cs", r"void Iniciar\("),
    "menuVit": ("MontarVitrine()<br/>modelos 3D nos pedestais", "Assets/Scripts/Menu.cs", r"void MontarVitrine\("),
    "menuUi": ("CriarInterface()<br/>colunas, cenário, botões", "Assets/Scripts/Menu.cs", r"void CriarInterface\("),
    "selecao": ("Selecao.cs<br/>escolhas do menu", "Assets/Scripts/Selecao.cs", r"class Selecao"),
    "jogo": ("Jogo.cs<br/>controla a partida", "Assets/Scripts/Jogo.cs", r"class Jogo"),
    "jogoStart": ("Start()<br/>monta mapa e equipes", "Assets/Scripts/Jogo.cs", r"void Start\("),
    "criarEq": ("CriarEquipes()<br/>jogador x inimigo", "Assets/Scripts/Jogo.cs", r"void CriarEquipes\("),
    "criarUn": ("CriarUnidade()<br/>modelo, atributos, escala", "Assets/Scripts/Jogo.cs", r"Unidade CriarUnidade\("),
    "partida": ("Partida()<br/>laço de turnos", "Assets/Scripts/Jogo.cs", r"IEnumerator Partida\("),
    "turnoJ": ("TurnoJogador()", "Assets/Scripts/Jogo.cs", r"IEnumerator TurnoJogador\("),
    "turnoI": ("TurnoInimigo()", "Assets/Scripts/Jogo.cs", r"IEnumerator TurnoInimigo\("),
    "update": ("Update()<br/>teclado e mouse", "Assets/Scripts/Jogo.cs", r"void Update\("),
    "botao": ("BotaoAcao()<br/>botões da HUD", "Assets/Scripts/Jogo.cs", r"void BotaoAcao\("),
    "mover": ("Mover()", "Assets/Scripts/Jogo.cs", r"IEnumerator Mover\("),
    "atirar": ("Atirar()", "Assets/Scripts/Jogo.cs", r"IEnumerator Atirar\("),
    "chance": ("Chance()<br/>mira, cobertura, flanco", "Assets/Scripts/Jogo.cs", r"public int Chance\("),
    "vigiar": ("Vigiar() e Vigilancias()<br/>tiro de reação", "Assets/Scripts/Jogo.cs", r"IEnumerator Vigiar\("),
    "defender": ("Defender()", "Assets/Scripts/Jogo.cs", r"IEnumerator Defender\("),
    "curar": ("Curar()", "Assets/Scripts/Jogo.cs", r"IEnumerator Curar\("),
    "fimJogo": ("Reiniciar() / VoltarAoMenu()", "Assets/Scripts/Jogo.cs", r"void VoltarAoMenu\("),
    "ia": ("IA.cs · Agir()<br/>decisão do inimigo", "Assets/Scripts/IA.cs", r"static IEnumerator Agir\("),
    # ---------- mapa ----------
    "gerador": ("GeradorDeMapa.cs · Gerar()", "Assets/Scripts/GeradorDeMapa.cs", r"static Grade Gerar\("),
    "distribuir": ("Distribuir()<br/>onde vai cada cobertura e marco", "Assets/Scripts/GeradorDeMapa.cs", r"static Grade Distribuir\("),
    "construir": ("Construir()<br/>coloca as peças 3D", "Assets/Scripts/GeradorDeMapa.cs", r"static void Construir\("),
    "grade": ("Grade.cs<br/>casas, caminhos, visão, cobertura", "Assets/Scripts/Grade.cs", r"class Grade"),
    "peca": ("PecaCenario.cs<br/>tamanho da peça", "Assets/Scripts/PecaCenario.cs", r"class PecaCenario"),
    # ---------- unidades e efeitos ----------
    "unidade": ("Unidade.cs<br/>andar, recuo, morrer", "Assets/Scripts/Unidade.cs", r"class Unidade"),
    "projetil": ("Projetil.cs<br/>Rigidbody, Trail, OnTriggerEnter", "Assets/Scripts/Projetil.cs", r"class Projetil"),
    "halo": ("HaloPulsante.cs", "Assets/Scripts/HaloPulsante.cs", r"class HaloPulsante"),
    "billboard": ("Billboard.cs", "Assets/Scripts/Billboard.cs", r"class Billboard"),
    "autodestruir": ("AutoDestruir.cs", "Assets/Scripts/AutoDestruir.cs", r"class AutoDestruir"),
    "camera": ("CameraTatica.cs<br/>WASD, Q/E, zoom", "Assets/Scripts/CameraTatica.cs", r"class CameraTatica"),
    # ---------- interface ----------
    "hud": ("Hud.cs<br/>interface da batalha", "Assets/Scripts/Hud.cs", r"class Hud"),
    "pontos": ("Pontuacao.cs<br/>placar e legenda", "Assets/Scripts/Pontuacao.cs", r"class Pontuacao"),
    "estilo": ("UiEstilo.cs<br/>cores, fontes, painéis", "Assets/Scripts/UiEstilo.cs", r"class UiEstilo"),
    "gravador": ("Gravador.cs<br/>grava o vídeo", "Assets/Scripts/Gravador.cs", r"class Gravador"),
    # ---------- dados ----------
    "equipe": ("Equipe.cs<br/>Equipe e Membro", "Assets/Scripts/Equipe.cs", r"class Equipe"),
    "cenario": ("Cenario.cs<br/>Cenario e Marco", "Assets/Scripts/Cenario.cs", r"class Cenario"),
    # ---------- editor ----------
    "montar": ("MontarJogo.cs · Executar()<br/>monta as cenas", "Assets/Editor/MontarJogo.cs", r"static void Executar\("),
    "build": ("MontarJogo.cs · Build()<br/>build Linux", "Assets/Editor/MontarJogo.cs", r"static void Build\("),
    "recursos": ("MontarJogoRecursos.cs<br/>texturas e materiais", "Assets/Editor/MontarJogoRecursos.cs", r"GerarTexturas\("),
    "edEquipes": ("CriarEquipes()<br/>6 equipes e atributos", "Assets/Editor/MontarJogoEquipes.cs", r"static Equipe\[\] CriarEquipes\("),
    "giros": ("Giros<br/>direção de cada modelo", "Assets/Editor/MontarJogoEquipes.cs", r"Dictionary<string, float> Giros"),
    "edCenarios": ("CriarCenarios()<br/>4 temas", "Assets/Editor/MontarJogoEquipes.cs", r"static Cenario\[\] CriarCenarios\("),
    "prefab": ("PrefabPeca()<br/>prefabs de cenário", "Assets/Editor/MontarJogoEquipes.cs", r"static GameObject PrefabPeca\("),
    "edMenu": ("MontarMenu()<br/>cena Menu", "Assets/Editor/MontarJogoEquipes.cs", r"static void MontarMenu\("),
    "testes": ("Testes.cs · Partidas()<br/>partidas automáticas", "Assets/Editor/Testes.cs", r"static void Partidas\("),
    "capturas": ("Capturas.cs<br/>prints e vídeo", "Assets/Editor/Capturas.cs", r"class Capturas"),
    "abrir": ("AbrirMenu.cs<br/>abre o Menu no editor", "Assets/Editor/AbrirMenu.cs", r"class AbrirMenu"),
    # ---------- ferramentas ----------
    "lista": ("lista_modelos.txt<br/>origem de cada modelo", "tools/lista_modelos.txt", None),
    "qem": ("decimar_qem.py<br/>STL/OBJ → OBJ leve", "tools/decimar_qem.py", r"def main\("),
    "simpl": ("simplificação por erro quádrico", "tools/decimar_qem.py", r"pyfqmr.Simplify\("),
    "normais": ("normais_com_quina()", "tools/decimar_qem.py", r"def normais_com_quina\("),
    "render": ("render.py<br/>pranchas de preview", "tools/render.py", r"def vista\("),
}

DIAGRAMAS = [
    ("Visão geral: as duas cenas", """flowchart LR
    subgraph CENA_MENU["Cena Menu"]
        menu --> menuUi
        menu --> menuVit
        menu --> menuIni
    end
    menuIni --> selecao
    selecao --> jogo
    subgraph CENA_BATALHA["Cena Batalha"]
        jogo --> jogoStart
        jogoStart --> gerador
        jogoStart --> criarEq
        jogoStart --> partida
        jogo --> hud
        jogo --> camera
    end
    subgraph DADOS["Dados (ScriptableObjects)"]
        equipe
        cenario
    end
    equipe -.-> menu
    cenario -.-> menu
    equipe -.-> criarEq
    cenario -.-> gerador
    menuUi --> estilo
    hud --> estilo
    fimJogo --> menu"""),

    ("Fluxo de uma partida", """flowchart TD
    partida --> turnoJ
    partida --> turnoI
    turnoJ --> update
    turnoJ --> botao
    update --> acoes
    botao --> acoes
    turnoI --> ia
    ia --> acoes
    subgraph acoes["Ações de cada soldado"]
        mover
        atirar
        vigiar
        defender
        curar
    end
    mover --> unidade
    mover --> vigiar
    atirar --> chance
    chance --> grade
    atirar --> projetil
    projetil --> autodestruir
    atirar --> unidade
    atirar --> pontos
    curar --> pontos
    pontos --> hud
    partida --> fimJogo
    criarEq --> criarUn
    criarUn --> unidade
    unidade --> halo
    unidade --> billboard"""),

    ("Geração do mapa", """flowchart LR
    cenario --> gerador
    gerador --> distribuir
    distribuir --> grade
    gerador --> construir
    construir --> peca
    prefab --> peca
    construir --> grade"""),

    ("Montagem no editor e ferramentas", """flowchart LR
    subgraph TOOLS["Ferramentas Python (tools/)"]
        lista --> qem
        qem --> simpl
        qem --> normais
        render
    end
    qem -->|"OBJ em Assets/Models"| montar
    subgraph EDITOR["Editor da Unity (Assets/Editor)"]
        montar --> recursos
        montar --> edEquipes
        edEquipes --> giros
        montar --> edCenarios
        edCenarios --> prefab
        montar --> edMenu
        montar --> build
        testes
        capturas
        abrir
    end
    edEquipes --> equipe
    edCenarios --> cenario
    capturas --> gravador"""),
]


def url(arquivo, padrao):
    caminho = os.path.join(RAIZ, arquivo)
    n = linha(arquivo, padrao) if padrao else 1
    return f"{ESQUEMA}://file{caminho}:{n}", n


def mermaid(corpo, d, relativo=False):
    # ids com prefixo do diagrama (d1_menu, d2_menu...), para não colidirem na mesma página
    usados = [k for k in N if re.search(rf"(?<![\w]){re.escape(k)}(?![\w\"])", corpo)]
    for k in sorted(usados, key=len, reverse=True):
        corpo = re.sub(rf"(?<![\w]){re.escape(k)}(?![\w\"])", f"d{d}_{k}", corpo)
    linhas = [corpo, ""]
    for k in usados:
        rot, arq, pad = N[k]
        linhas.append(f'    d{d}_{k}["{rot}"]')
    for k in usados:
        rot, arq, pad = N[k]
        u, n = url(arq, pad)
        if relativo:   # no preview do VS Code, link relativo abre o arquivo direto na linha
            u = f"{arq}#L{n}"
        linhas.append(f'    click d{d}_{k} href "{u}" "Abrir {arq}:{n}" _self')
    linhas.append("    classDef default fill:#1c2129,stroke:#e6b855,color:#e8ecf0;")
    return "\n".join(linhas)


def tabela():
    linhas = ["| Parte | Arquivo (clique para abrir) |", "|---|---|"]
    for k, (rot, arq, pad) in N.items():
        u, n = url(arq, pad)
        nome = rot.replace("<br/>", " — ")
        linhas.append(f"| {nome} | [`{arq}:{n}`]({arq}#L{n}) |")
    return "\n".join(linhas)


def gerar_md():
    partes = [
        "# Mapa do código: Kill Team Tático",
        "",
        "Cada caixa dos diagramas abre o arquivo no **VS Code**, já na linha da função.",
        "",
        "**Como usar no VS Code:**",
        "",
        "1. Abra este arquivo e aperte `Ctrl+Shift+V`. O clique só funciona no *preview*, não no texto do arquivo.",
        "2. Para os diagramas aparecerem desenhados, instale uma vez a extensão *Markdown Preview Mermaid Support*: `code-oss --install-extension bierner.markdown-mermaid`, ou procure por \"Markdown Preview Mermaid\" na aba de extensões.",
        "3. Sem a extensão, use a tabela **Todos os arquivos** no fim: cada link abre o arquivo na linha certa.",
        "",
        "**No navegador:** abra `MAPA_DO_CODIGO.html`, que já desenha os diagramas. Ao clicar, o navegador e o VS Code pedem confirmação. Marque *Allow opening local paths without asking*.",
        "",
        f"Para gerar de novo (se o projeto mudar de pasta ou o código mudar de linha), rode `python3 tools/gerar_mapa.py`. O HTML usa o protocolo `{ESQUEMA}://`; para o VS Code oficial, rode `python3 tools/gerar_mapa.py vscode`.",
        "",
    ]
    for d, (titulo, corpo) in enumerate(DIAGRAMAS, 1):
        partes += [f"## {titulo}", "", "```mermaid", mermaid(corpo, d, relativo=True), "```", ""]
    partes += ["## Todos os arquivos", "", tabela(), ""]
    return "\n".join(partes)


def gerar_html():
    blocos = "\n".join(
        f'<section><h2>{html.escape(t)}</h2><pre class="mermaid">\n{html.escape(mermaid(c, d))}\n</pre></section>'
        for d, (t, c) in enumerate(DIAGRAMAS, 1))
    return f"""<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Mapa do código</title>
<style>
  :root {{ --fundo:#0e1014; --painel:#161a21; --texto:#e8ecf0; --fraco:#8a94a6; --ouro:#e6b855; }}
  body {{ margin:0; background:var(--fundo); color:var(--texto); font-family:system-ui, sans-serif; }}
  main {{ max-width:1200px; margin:0 auto; padding:24px 16px 64px; }}
  h1 {{ color:var(--ouro); letter-spacing:.04em; margin:0 0 4px; }}
  p {{ color:var(--fraco); margin:4px 0 24px; }}
  section {{ background:var(--painel); border:1px solid #ffffff17; border-radius:6px; padding:16px; margin:16px 0; overflow-x:auto; }}
  h2 {{ font-size:1.05rem; color:var(--ouro); margin:0 0 8px; }}
  .mermaid {{ background:transparent; }}
  .mermaid .node {{ cursor:pointer; }}
</style>
</head>
<body>
<main>
<h1>Mapa do código: Kill Team Tático</h1>
<p>Clique numa caixa para abrir o arquivo no VS Code, já na linha da função.</p>
{blocos}
</main>
<script type="module">
  import mermaid from "https://cdn.jsdelivr.net/npm/mermaid@10.9.1/dist/mermaid.esm.min.mjs";
  mermaid.initialize({{ startOnLoad: false, securityLevel: "loose", theme: "dark" }});
  // um diagrama por vez: evita que os desenhos se misturem
  const els = [...document.querySelectorAll(".mermaid")];
  for (let i = 0; i < els.length; i++) {{
    const {{ svg, bindFunctions }} = await mermaid.render("grafico" + (i + 1), els[i].textContent);
    els[i].innerHTML = svg;
    if (bindFunctions) bindFunctions(els[i]);
  }}
</script>
</body>
</html>
"""


if __name__ == "__main__":
    with open(os.path.join(RAIZ, "MAPA_DO_CODIGO.md"), "w", encoding="utf-8") as f:
        f.write(gerar_md())
    with open(os.path.join(RAIZ, "MAPA_DO_CODIGO.html"), "w", encoding="utf-8") as f:
        f.write(gerar_html())
    print("MAPA_DO_CODIGO.md e MAPA_DO_CODIGO.html gerados em", RAIZ)
