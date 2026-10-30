# Inicialização, título e arquivos de aventura

Verificação local no Windows x64, com .NET 10 e os dados importados da ROM
USA revisão 0. Nenhuma ROM, imagem extraída ou save foi adicionado ao Git.

## Comportamento implementado

- A abertura original antecede o título. O logo vem de `logofinal`, na ROM.
- Clique ou tecla no título abre os três arquivos. Cada entrada mostra o
  checkpoint, os octolitos e os scans do respectivo JSON do MphRead.
- Escolher um arquivo abre as opções de aventura, arena e configurações.
  A consulta não instancia uma campanha nem cria ou regrava saves.
- Nova aventura reproduz `StoryIntro`, `CALanding` e a câmera nativa
  `unit2_land_intro`, antes de entregar o controle em Celestial Gateway.
- Continuar carrega o arquivo escolhido sem repetir o prólogo de um novo jogo.
- Esc pula a abertura. Esc/Enter pulam vídeos da campanha. O clique usado para
  iniciar e os comandos de movimento não pulam automaticamente essas cenas.

Nos vídeos que possuem imagens complementares nas duas telas originais,
ambos os canais são apresentados lado a lado numa composição cinematográfica,
com proporção preservada e sem moldura de DS. Vídeos com um canal são
centralizados. Menus, HUD e gameplay usam uma única tela.

## Verificações

`dotnet run --project tests/PcChecks/PcChecks.csproj -c Release`:
53 verificações sem ROM, incluindo navegação título/arquivos, escolha de slot,
hitboxes em 16:9, 4:3 e ultrawide, consulta de saves e relógio de reprodução.

`HuntersPC.exe --pc-verify-startup <pasta-absoluta>` cria uma pasta de estado
isolada e executa o frontend e o renderer reais em janelas OpenGL ocultas.
Não usa eventos de teclado/mouse do sistema nem captura o cursor.

O replay verifica a abertura até o título, navegação por teclado, clique no
arquivo 3 após redimensionar para 1024×768, configurações, nova aventura no
arquivo 1, preservação byte a byte do arquivo 3, continuação e retorno ao título.
Cada captura também confere `GL error: NoError`.

Com `HUNTERSPC_VERIFY_REALTIME=1`, 33 verificações passaram no fluxo completo
sem acelerar o relógio dos vídeos:

| Vídeo | Quadros apresentados | Tempo observado |
| --- | ---: | ---: |
| Opening | 1243 / 1243 | 84,30 s |
| StoryIntro | 150 / 150 | 10,18 s |
| CALanding, nova aventura | 60 / 60 | 4,19 s |
| CALanding, continuação | 60 / 60 | 4,16 s |

Os tempos incluem a transição de saída observada pelo replay. Nenhum vídeo foi
pulado. O relógio acelerado padrão do teste percorre os mesmos quadros, apenas
reduzindo a espera; ele não altera a reprodução normal do jogo.

Na build Windows local final passaram também as 32 verificações do replay
acelerado (incluindo retorno ao título com música), as 5 do smoke de menu/áudio
e as 54 de gameplay/HUD/mapa/diário. O hash do save original do jogador permaneceu
igual antes e depois de compilar e executar os testes. Os 4 testes de auditoria
do pacote Windows também passaram.

## Limites

A tela de título e a seleção de arquivos são adaptações da interface para PC,
não uma reprodução de todos os efeitos e modelos animados do menu de DS.
Multiplayer e gerenciamento de copiar/apagar arquivos não foram acrescentados.
O fluxo inicial foi exercitado no Windows com a ROM USA; isso não certifica
a campanha inteira, todos os checkpoints, outras GPUs ou todas as revisões.
Os testes de Linux anteriores estão em [Validação Linux](VALIDACAO-LINUX.md).
