# Verificação — HUD, mapa e diário — 29/10/2026

## Resultado desta continuação

- Build Release e publicação Windows x64 autossuficiente concluídos.
- 30 verificações unitárias passaram: perfil, bindings, hitboxes, ROM, filtragem
  dos scans desbloqueados, texto/linhas, projeção, pan, zoom e altura do mapa.
- 54 verificações no runtime real passaram, na arena Combat Hall e em Celestial
  Gateway, primeira sala da aventura. Foram geradas 21 capturas OpenGL.
- O executável autossuficiente publicado também repetiu as 54 verificações e
  as 21 capturas com código de saída 0. Resultado em `local/verification-published/`.
  A ROM já está importada na instalação local `dist/HuntersPC/`.
- Renderização real em Intel Iris Xe, OpenGL 3.2. As capturas registraram
  `GL error: NoError`.

## Cobertura de gameplay e interface

- Barra de nove atalhos de armas: seleção atual destacada, armas indisponíveis
  atenuadas, sem conceder armas por atalho. Captura da HUD normal revisada.
- WASD movimentou o jogador e deltas de mouse giraram a câmera usando os
  bindings nativos. O replay disparou para iniciar a arena pela regra original.
- C iniciou a animação nativa, completou a transformação em Morph Ball e retornou
  à primeira pessoa. A HUD alternativa mostra boost/bomba e esconde a barra de armas.
- M abriu o mapa na mesma janela com geometria real da ROM, marcador da posição,
  salas exploradas e portas atuais; deslocamento, zoom e altura foram exercitados.
- O contador de simulação, a posição e a direção do jogador ficaram estáveis
  enquanto o mapa recebia W e deltas de mouse muito grandes.
- Ao fechar o mapa, deltas simulados de recaptura não causaram salto da câmera.
- J abriu os nove registros iniciais de equipamento, mantendo categorias sem
  descobertas vazias. Só registros marcados no save são listados.
- Diário aceita categorias e rolagem; mostra textos originais e dica adicional de
  controle PC. A leitura manteve a simulação pausada.
- Tab alternou mapa/diário sem retomar o jogo. Um mapa aberto a partir da pausa
  voltou à pausa com Esc; outro Esc retomou o gameplay.
- O botão de voltar do diário foi clicado pelo manipulador interno, usando a
  posição visual escalada em 1920×1080.
- Capturas de mapa em 1024×768 e diário em 1920×1080 foram geradas. Planta e texto
  permaneceram dentro do canvas, com proporção preservada.

## Isolamento

Nenhuma ferramenta de uso do computador precisou enviar entrada ao desktop nesta
continuação. As janelas de verificação permaneceram ocultas, sem capturar o cursor.
O replay usa eventos internos do jogo e não lê a entrada física. Perfis/saves de
teste ficam em `local/verification-field-panels/`; o perfil original não foi alterado.
O resultado completo está em `verification.json` nessa pasta, com capturas e
estado do jogador. Não foi necessário criar/trocar uma área de trabalho do Windows.

## Limites práticos

Isso cobre a arena, uma sala de campanha e os fluxos de interface descritos.
Não certifica a campanha inteira, todos os saves, todos os layouts de sala,
multiplayer online, desempenho sustentado ou outras GPUs. O mapa é local por
sala visitada, sem composição 3D regional. O diário usa o idioma da ROM; as dicas
e a interface PC estão em português.

Persistem os avisos NU1902/NU1903 da dependência ImageSharp herdada. A situação e
o motivo de não substituí-la silenciosamente estão em `ANALISE-PORT-PC.md`.
Não houve supressão dos avisos no projeto. Fonte/ROM/saves permanecem separados;
os dados importados na instalação local não devem integrar um pacote público.

## Histórico

Na primeira sessão, menu gráfico, arena e pausa foram verificados pela interface
Windows. O Esc físico interrompeu aquele uso do computador. Esta continuação foi
autorizada pelo usuário e verificou as mudanças restantes sem disputar o desktop.
