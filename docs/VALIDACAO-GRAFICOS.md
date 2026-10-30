# Tela cheia e filtragem gráfica

## Correção do enquadramento

A abertura atualizava o tamanho da cena sem atualizar o viewport OpenGL. Uma
mudança para tela cheia podia deixar o desenho limitado ao viewport anterior,
com áreas brancas e a imagem deslocada. O tamanho também vinha das coordenadas
da janela, que podem diferir dos pixels do framebuffer em uma tela com HiDPI.

Frontend e gameplay agora sincronizam o framebuffer, o viewport e os recursos
da cena a cada frame. O renderer também restaura seu viewport antes dos passes,
inclusive depois de overlays. A minimização com área zero não realoca texturas.
As capturas F12 usam a resolução física completa. As proporções e barras pretas
dos filmes são preservadas.

## Gráficos melhorados

`EnhancedGraphics` é ligado por padrão, inclusive ao ler um perfil antigo sem
essa propriedade. **CONFIGURACOES → GRAFICOS** alterna entre melhorados e
originais durante o jogo e persiste a escolha.

- Texturas de modelos/cenários usam mipmaps, minificação trilinear e ampliação
  linear. As cadeias são geradas uma vez por textura e invalidadas ao regravá-la.
- Anisotropia chega a 16×, limitada ao suporte anunciado pela GPU. A consulta e
  o parâmetro de anisotropia só são usados se a extensão correspondente existir;
  a filtragem trilinear continua funcionando sem ela.
- Um filtro de bordas no shader de apresentação suaviza a cena 3D. Não há
  filtragem global sobre os textos e atlas do HUD, menus, mapa ou diário.
- Filmes usam filtragem linear sem gerar mipmaps a cada quadro. O filtro de
  bordas da cena 3D fica desligado enquanto um filme cobre o jogo.

Não foram adicionadas texturas HD, novos modelos ou detalhes ausentes dos
arquivos originais. A filtragem preserva a paleta e a iluminação existentes.
O parâmetro de anisotropia segue a
[especificação Khronos](https://registry.khronos.org/OpenGL/extensions/EXT/EXT_texture_filter_anisotropic.txt).

## Verificação local

Windows x64, Intel Iris Xe, OpenGL 3.2, ROM USA revisão 0. O replay de
`--pc-verify-startup` roda em janelas ocultas, sem entrada do desktop:

- Injeta o viewport deslocado 137,166/1280×720 para reproduzir a falha e muda
  a janela entre 1920×1080, 2560×1080 e 1024×768 durante o vídeo.
- Confere viewport e tamanho da cena contra o framebuffer; lê os pixels dos
  cantos para detectar a moldura branca e captura cada resolução.
- Percorre todos os 1.243 quadros da abertura, testa título/arquivos/continuação,
  alterna gráficos por teclado e clique após redimensionar e confere persistência.
- Compara a mesma cena congelada no modo original e melhorado. Consulta o sampler
  real da GPU e o nível 1 da textura para verificar os mipmaps.

Resultado: 53 verificações passaram no replay. A GPU aplicou anisotropia 16×,
69 cadeias de mipmaps, `LinearMipmapLinear` e `Linear`, sem erro OpenGL.
Os 57 checks sem ROM também passaram. As capturas e saves de teste ficam em
`local/`, fora do Git e dos pacotes públicos.

O smoke sem ROM também compila e faz o link do shader de apresentação/AA no
contexto gráfico real, permitindo verificá-lo no Mesa do CI Linux.

As resoluções de tela cheia foram reproduzidas com janelas ocultas para não
interromper o desktop. Isso não mede todas as GPUs, monitores, escalas de DPI
ou o custo de renderização em toda a campanha. O modo original permite reduzir
o custo dos filtros em hardware mais limitado.
