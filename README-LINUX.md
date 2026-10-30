# Hunters PC para Linux x64

Baixe `MetroidPrimeHuntersPC-v0.2.0-linux-x64.tar.gz` na página de releases.
O pacote inclui .NET 10, GLFW e MiniAudio. Requer Linux x86-64 com glibc 2.27+
e OpenGL 3.2 com perfil de compatibilidade. Usa X11 ou XWayland em sessões Wayland.
OpenAL Soft e as bibliotecas do sistema devem estar instaladas.

## Dependências

Fedora:

```bash
sudo dnf install openal-soft libX11 libXcursor libXi libXinerama libXrandr \
  mesa-libGL alsa-lib libicu openssl-libs zlib libstdc++ xorg-x11-server-Xwayland python3
```

Ubuntu 22.04 / 24.04 e Debian 12 / 13:

```bash
sudo apt install libopenal1 libx11-6 libxcursor1 libxi6 libxinerama1 libxrandr2 \
  libgl1 libasound2 libicu-dev libssl-dev zlib1g libstdc++6 xwayland python3
```

No Ubuntu 24.04, caso solicitado, use `libasound2t64` em lugar de `libasound2`.
Não é necessário instalar .NET ou SDK para jogar.

## Instalar e iniciar

```bash
tar -xzf MetroidPrimeHuntersPC-v0.2.0-linux-x64.tar.gz
cd HuntersPC
./install.sh
```

Abra **Metroid Prime Hunters PC** no menu de aplicativos, ou execute
`~/.local/bin/hunters-pc`. A instalação fica em `~/.local/opt/HuntersPC`.
Também é possível executar `./HuntersPC.sh` diretamente da pasta extraída.
O instalador exige Python 3 e não usa sudo. `./install.sh --prefix /outro/local`
permite escolher outro prefixo para os binários e o comando.

São aceitas as ROMs USA revisão 0 (AMHE0) e Europa revisão 1 (AMHP1),
ambas identificadas por tamanho, cabeçalho e SHA-256. Importe sua própria ROM pelo menu ou por linha de comando:

```bash
~/.local/bin/hunters-pc --pc-import '/caminho/Metroid Prime Hunters.nds'
~/.local/bin/hunters-pc
```

ZIP com uma ROM também é aceito. Arquivos 7z devem ser extraídos antes.
Após a abertura e o título, clique ou pressione uma tecla, escolha um dos três
arquivos e entre em **NOVA AVENTURA** ou **CONTINUAR AVENTURA**.
**TESTAR CONTROLES** abre a arena sem alterar a campanha.
Os controles são os mesmos de Windows; consulte `README-PC.md`.

## Dados, saves e atualizações

ROM extraída, `paths.txt`, `Savedata/`, `pc-profile.json`, diagnósticos e capturas
ficam em `${XDG_DATA_HOME:-$HOME/.local/share}/hunterspc`, separados dos binários.
A variável `HUNTERSPC_DATA_DIR` pode escolher outra pasta de dados.
Extraia uma nova release e rode seu `./install.sh` para atualizar os binários.
O instalador não copia dados de jogo nem altera seus saves.

Para desinstalar, remova `~/.local/opt/HuntersPC`, `~/.local/bin/hunters-pc`
e o arquivo `io.github.GenissonEmilio.HuntersPC.desktop` em
`${XDG_DATA_HOME:-$HOME/.local/share}/applications`. Os dados pessoais permanecem.

## Compilar e verificar

Requer .NET SDK 10, Python 3 e acesso ao NuGet:

```bash
dotnet run --project tests/PcChecks/PcChecks.csproj -c Release
python3 -m unittest discover -s tests -p 'test_*package.py'
./scripts/build-linux.sh
./dist/HuntersPC-linux-x64/HuntersPC.sh --pc-smoke "$PWD/evidence/linux-smoke"
python3 scripts/package-linux.py --version v0.2.0
```

`--pc-smoke` verifica menu OpenGL, configurações, redimensionamento e os dois
backends de áudio sem ROM, em janela oculta e sem capturar entrada do desktop.
Após importar a ROM, `--pc-verify "$PWD/evidence/linux-gameplay"` verifica arena,
primeira sala, controles, Morph Ball, mapa e diário. Perfis e saves de teste
ficam isolados na pasta de evidências. Isso não certifica a campanha completa.

`--pc-verify-startup "$PWD/evidence/startup"` confere abertura, título, arquivos,
prólogo e continuação com saves isolados. Use `HUNTERSPC_VERIFY_REALTIME=1` para
reproduzir os vídeos nas durações originais durante esse teste. Consulte
[a validação da inicialização](docs/VALIDACAO-INICIALIZACAO.md).
