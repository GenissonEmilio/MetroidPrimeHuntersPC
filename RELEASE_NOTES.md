# Metroid Prime Hunters PC v0.1.0-alpha.1

Primeira versão experimental para **Windows x64**, baseada em MphRead, de NoneGiven.

## Download

Use `MetroidPrimeHuntersPC-v0.1.0-alpha.1-windows-x64.zip` e extraia a pasta
inteira. Abra `HuntersPC.exe` e importe sua própria ROM USA revisão 0 (`.nds` ou ZIP
com uma única ROM). O pacote inclui o runtime .NET e o OpenAL Soft. Não requer
instalar um SDK para jogar.

Nenhuma ROM, BIOS, save, perfil pessoal ou dado extraído do jogo acompanha o download.

## Incluído

- WASD e mira/câmera pelo mouse; atalhos para ações da antiga tela de toque.
- HUD em uma tela, barra de armas e dicas para Morph Ball.
- Menu gráfico, pausa, sensibilidade, inversão vertical, volumes e tela cheia.
- Mapa local com geometria da ROM, posição, portas, salas exploradas, zoom e altura.
- Diário de scans desbloqueados por categoria, texto com rolagem e dicas de PC.
- M/J abrem os painéis; Tab alterna; Esc ou VOLTAR fecham sem salto de câmera.
- Importação local com validação de tamanho, revisão e SHA-256.

## Validação e limites

30 verificações unitárias e 54 verificações no runtime real passaram, com 21
capturas geradas em arena e primeira sala de campanha. Foram conferidos HUD,
Morph Ball, WASD, deltas de mouse, pausa, retorno e painéis em 16:9 e 4:3.

É uma **alpha**, sem certificação da campanha completa, todos os saves, todas
as salas ou multiplayer online. O mapa é uma planta por sala explorada; não
recria a composição 3D regional do DS. Os textos do diário seguem o idioma da ROM.
Os avisos NU1902/NU1903 da dependência ImageSharp herdada continuam registrados;
veja `docs/ANALISE-PORT-PC.md`. O executável não possui assinatura Authenticode.

Mantenha seus saves ao atualizar. O formato de progresso é o JSON do MphRead;
não há garantia de compatibilidade com saves de DS.

## Créditos

MphRead / NoneGiven, MIT. Modificações de PC e interface: GenissonEmilio.
Licenças das bibliotecas acompanham o pacote em `licenses/`.
