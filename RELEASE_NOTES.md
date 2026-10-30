# Metroid Prime Hunters PC v0.3.0 — abertura completa e gráficos melhorados

Nova versão para **Windows e Linux x64**, baseada em MphRead, de NoneGiven.
Corrige o enquadramento das cutscenes em tela cheia e acrescenta a abertura,
o fluxo de título/arquivos e filtros gráficos ligados por padrão.

## Downloads e instalação

- **Windows:** `MetroidPrimeHuntersPC-v0.3.0-windows-x64.zip`. Extraia a pasta
  inteira e abra `HuntersPC.exe`. Requer Windows 10/11 x64 e OpenGL 3.2.
- **Linux:** `MetroidPrimeHuntersPC-v0.3.0-linux-x64.tar.gz`. Extraia e execute
  `./install.sh` dentro de `HuntersPC/`. Abra pelo menu de aplicativos ou pelo
  comando `hunters-pc`. Consulte o [guia Linux](https://github.com/GenissonEmilio/MetroidPrimeHuntersPC/blob/v0.3.0/README-LINUX.md)
  para dependências de OpenAL, X11/XWayland e drivers OpenGL.

Os dois pacotes incluem o runtime .NET 10 e não exigem SDK para jogar.
Importe sua própria ROM **USA revisão 0** ou **Europa revisão 1**, `.nds` ou ZIP
com uma única ROM. Não há ROM, BIOS, save, perfil pessoal ou dados extraídos
do jogo nos downloads. As auditorias e os checksums SHA-256 acompanham a release.

## Novidades

- **Cutscenes em tela cheia:** viewport, tamanho da cena e framebuffer agora
  acompanham a resolução física, inclusive após redimensionar ou mudar de DPI.
  Corrige a imagem deslocada e a moldura branca; preserva proporções e barras
  pretas dos filmes.
- **Gráficos melhorados por padrão:** texturas com mipmaps, filtragem trilinear,
  anisotropia até 16× conforme a GPU e suavização de bordas na cena 3D. Vídeos
  ampliados usam filtragem linear; textos e atlas do HUD permanecem nítidos.
  **Configurações → Gráficos** permite alternar para o visual original e salva
  a preferência. São filtros sobre as texturas originais, sem pacote de texturas HD.
- **Abertura completa:** todos os 1.243 quadros são reproduzidos antes do título.
  O decoder espera os dois canais e encerra somente após o último quadro.
- **Título e três arquivos:** clique ou tecla no título abre a seleção de saves,
  com checkpoint, octolitos e scans reais. Consultar os arquivos não cria nem
  regrava a campanha. Saves ilegíveis são identificados e preservados.
- **Nova aventura:** reproduz prólogo, pouso e câmera de chegada à primeira sala.
  Continuar usa o arquivo escolhido sem repetir o prólogo. Para rever tudo desde
  o começo, escolha um arquivo vazio. Esc pula a abertura; Esc/Enter pulam vídeos
  da campanha. Cliques de lançamento e teclas de movimento não pulam as cenas.
- Menu, configurações, HUD, mapa local e diário continuam em uma única janela,
  com WASD, câmera no mouse e atalhos M/J. F12 captura a resolução física completa.

Nos filmes com imagens complementares nas duas telas de DS, os canais ficam
lado a lado numa composição cinematográfica, sem moldura de console. Os vídeos
de um canal ficam centralizados. Gameplay e menus usam uma única tela.

## Atualizar preservando o progresso

No Linux, execute o instalador do novo pacote; os dados em
`${XDG_DATA_HOME:-$HOME/.local/share}/hunterspc` ficam separados dos binários.

No Windows, extraia em uma pasta nova. Copie `Savedata/` e `pc-profile.json`
da instalação anterior para manter progresso e ajustes, e reimporte a mesma ROM
na nova pasta. Guarde a instalação anterior até conferir seus arquivos.
Os saves continuam no formato JSON do MphRead.

## Validação e limites

- 57 checks sem ROM e 11 testes de empacotamento/instalação.
- No Windows, 53 verificações de inicialização/gráficos, 54 de gameplay/HUD/mapa/
  diário e 6 de smoke de menu/áudio/shader passaram em janelas ocultas.
- Foram conferidos viewport e pixels dos cantos em 1920×1080, 2560×1080 e
  1024×768, incluindo a reprodução de um viewport deslocado. O sampler real
  da Intel Iris Xe confirmou anisotropia 16×, mipmaps e filtros ativos.
- Os dois pacotes são compilados e auditados no CI. O Linux também executa o
  menu, os backends de áudio e a compilação/link do shader no Mesa, sem ROM.

Isso não certifica toda a campanha, todos os checkpoints, outras GPUs ou
multiplayer online. A interface de título/arquivos é uma adaptação para PC;
não inclui todos os efeitos do menu de DS nem copiar/apagar arquivos. O mapa
continua sendo uma planta local por sala; o diário usa o idioma da ROM.
Os avisos herdados de dependências continuam documentados, sem supressão.
O executável Windows não possui assinatura Authenticode.

Detalhes: [gráficos](https://github.com/GenissonEmilio/MetroidPrimeHuntersPC/blob/v0.3.0/docs/VALIDACAO-GRAFICOS.md),
[inicialização](https://github.com/GenissonEmilio/MetroidPrimeHuntersPC/blob/v0.3.0/docs/VALIDACAO-INICIALIZACAO.md)
e [painéis](https://github.com/GenissonEmilio/MetroidPrimeHuntersPC/blob/v0.3.0/docs/VALIDACAO-PAINEIS-PC.md).

## Créditos

MphRead / NoneGiven, MIT. Modificações de PC e interface: GenissonEmilio.
Histórico, créditos e licença foram preservados. Licenças das bibliotecas
acompanham os pacotes em `licenses/`. Projeto não afiliado à Nintendo.
