# Próxima atualização — abertura e arquivos

- Abertura original completa antes do título, com a marca lida da ROM.
- Clique ou tecla no título abre três arquivos, com checkpoint e progresso reais.
- Novo jogo reproduz prólogo, chegada e câmera da primeira sala; continuar usa
  o arquivo escolhido sem repetir o prólogo.
- Vídeos avançam somente quando ambos os canais foram decodificados, preservam
  todos os quadros e só são pulados por comando explícito.
- Menu, arquivos e configurações ficam na mesma janela; WASD, mouse, mapa e
  diário continuam disponíveis.
- 53 verificações sem ROM e replay de inicialização com saves isolados. Consulte
  `docs/VALIDACAO-INICIALIZACAO.md` para resultados e limites.

# Metroid Prime Hunters PC v0.2.0 — Linux e Windows x64

Release principal, sem marca de pré-release, com suporte nativo a Linux x64.
Baseada em MphRead, de NoneGiven. O estado experimental da recriação do jogo e
os limites de campanha continuam descritos abaixo.

## Downloads

- **Linux:** `MetroidPrimeHuntersPC-v0.2.0-linux-x64.tar.gz`. Extraia e execute
  `./install.sh` em `HuntersPC/`. Abra **Metroid Prime Hunters PC** no menu de
  aplicativos ou execute `~/.local/bin/hunters-pc`. Consulte `README-LINUX.md`
  para dependências de OpenAL/X11 e drivers OpenGL.
- **Windows:** `MetroidPrimeHuntersPC-v0.2.0-windows-x64.zip`. Extraia a pasta
  inteira e abra `HuntersPC.exe`.

Os dois pacotes incluem o runtime .NET 10; não exigem SDK para jogar.
Importe sua própria ROM **USA revisão 0** ou **Europa revisão 1**, `.nds` ou ZIP
com uma ROM. Extraia arquivos 7z antes de importar. Cada revisão é validada por
tamanho, identificação e SHA-256. Nenhuma ROM, BIOS, save, perfil pessoal ou
dado extraído do jogo acompanha os downloads.

## Novidades

- Linux x64 com renderização OpenGL 3.2 em X11/XWayland, GLFW e MiniAudio.
- Instalador por usuário, comando `hunters-pc`, ícone e entrada no menu de aplicativos.
- No Linux, dados ficam em `${XDG_DATA_HOME:-$HOME/.local/share}/hunterspc`,
  separados dos binários. Atualizar o programa preserva a importação e os saves.
- `HUNTERSPC_DATA_DIR` permite escolher outra pasta de dados e isolar testes.
- Importação da ROM Europa revisão 1, além da USA revisão 0 já aceita.
- Correção do estado OpenGL ao abrir mapa, diário e pausa: o shader e framebuffer
  da cena são restaurados mesmo quando a simulação está pausada.
- Compilação, auditoria de tarball e verificação de menu/áudio no CI Linux.
- Manifesto com hashes por arquivo, auditoria contra dados pessoais e checksum do download.

WASD, mira no mouse, HUD em uma tela, configurações, mapa local e diário seguem
disponíveis. M/J abrem os painéis, Tab alterna, Esc volta ou pausa.

## Validação e limites

37 verificações unitárias, 11 testes de empacotamento/instalação e 54 verificações
de gameplay no Linux, com 21 capturas em arena e primeira sala da aventura.
Menu, configurações, redimensionamento e inicialização dos dois backends de áudio
também foram conferidos sem ROM. O replay usa janelas ocultas e não captura o
cursor nem injeta eventos no desktop. A verificação local usa Fedora 44,
Intel/Mesa e a ROM Europa revisão 1; o CI Linux usa Ubuntu 22.04/Mesa.

Isso não certifica campanha completa, todos os saves, todas as salas, outras
GPUs ou multiplayer online. O mapa é uma planta local por sala, sem composição
3D regional original do DS. Os textos do diário seguem o idioma da ROM.
Os avisos NU1902/NU1903 da dependência ImageSharp herdada continuam registrados
em `docs/ANALISE-PORT-PC.md`; não foram suprimidos. O executável Windows não tem
assinatura Authenticode. Saves usam o JSON do MphRead, sem garantia de
compatibilidade com saves de DS.

## Créditos

MphRead / NoneGiven, MIT. Modificações de PC e interface: GenissonEmilio.
Licenças e avisos das bibliotecas acompanham os pacotes em `licenses/`.
