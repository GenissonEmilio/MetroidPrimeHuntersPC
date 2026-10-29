# Metroid Prime Hunters PC

Adaptação experimental de **Metroid Prime Hunters** para **Windows e Linux x64**, baseada
em [MphRead, de NoneGiven](https://github.com/NoneGiven/MphRead). A lógica e a
renderização nativas vêm dessa base; esta versão acrescenta uma interface de PC,
configurações, mapa e diário em uma única tela.

**[Downloads para Windows e Linux](https://github.com/GenissonEmilio/MetroidPrimeHuntersPC/releases)** ·
[Guia e controles](README-PC.md) · [Validação](docs/VALIDACAO-PAINEIS-PC.md)

## Instalar e jogar no Windows

1. Baixe o ZIP **Windows x64** na página de releases e extraia a pasta inteira.
2. Abra `HuntersPC.exe`. O runtime .NET e as bibliotecas de áudio já acompanham o pacote.
3. Arraste sua própria ROM **USA revisão 0 ou Europa revisão 1**, `.nds` ou ZIP com uma única ROM, para o menu.
4. Escolha **TESTAR CONTROLES** para uma arena local ou **INICIAR / CONTINUAR** para a aventura.

Requer Windows 10/11 de 64 bits e suporte a OpenGL 3.2. Não é necessário instalar
o .NET para jogar. Não há ROM, BIOS, saves ou dados extraídos do jogo neste repositório
ou nos pacotes públicos.

## Instalar e jogar no Linux

Baixe o pacote `linux-x64.tar.gz` na página de releases, extraia e execute
`./install.sh` dentro de `HuntersPC/`. Abra **Metroid Prime Hunters PC** no menu
de aplicativos ou execute `~/.local/bin/hunters-pc`. A ROM, saves e configurações
ficam em `~/.local/share/hunterspc` (ou `$XDG_DATA_HOME/hunterspc`).
O runtime .NET 10 acompanha o pacote. Consulte o
[guia Linux](README-LINUX.md) para dependências, instalação e compilação.

## Controles

| Entrada | Ação |
| --- | --- |
| WASD / mouse | Movimento / câmera e mira |
| Clique esquerdo / direito | Disparar / zoom |
| Espaço / C | Pular ou boost / alternar forma |
| E / Q | Visor / scan ou ataque alternativo |
| Roda / 1–9 | Trocar / selecionar arma disponível |
| M / J | Mapa local / diário de scans |
| Tab nos painéis / Esc | Alternar mapa e diário / voltar ou pausar |
| F11 / F12 | Tela cheia / captura local |

Mapa, diário, pausa e gameplay usam a mesma janela. O mapa mostra geometria da
sala, posição/direção, portas e salas já visitadas, com zoom e seleção de altura.
O diário lista somente registros desbloqueados no save e usa os textos da ROM.

## Estado da versão

**v0.2.0 — release principal, com Linux x64.** A base de jogo continua em
desenvolvimento. Foram verificados a arena Combat Hall, a primeira sala
da aventura, HUD normal/Morph Ball e os fluxos de mapa/diário, com 37 verificações
unitárias, 11 testes de empacotamento/instalação e 54 no runtime real do Linux. Isso não certifica campanha completa, todos os
saves, todas as salas ou multiplayer online. O mapa é uma planta local por sala,
e não a composição 3D regional original do DS. Leia as limitações da release.

No Linux, execute o instalador da nova versão; os dados ficam separados.
No Windows, para atualizar, extraia o ZIP em uma pasta nova e copie, se quiser manter a
instalação, as pastas `files/` e `Savedata/`, além de `pc-profile.json` e
`rom-import.json`. `paths.txt` guarda caminhos absolutos; é mais simples reimportar
a mesma ROM na pasta nova. Nunca distribua seus dados do jogo ou seus saves.

## Compilar

```powershell
dotnet run --project tests/PcChecks/PcChecks.csproj -c Release
.\scripts\build-pc.ps1
.\scripts\package-windows.ps1
```

Requer .NET SDK 10 ou compatível, Python 3.10+ e PowerShell 7 para os scripts.
`build-pc.ps1` prepara uma instalação local;
`package-windows.ps1` cria um ZIP novo sem dados pessoais ou conteúdo do jogo,
com licenças, manifesto e checksum. Os avisos de dependências herdadas estão
documentados em [Análise técnica](docs/ANALISE-PORT-PC.md).

## Créditos e licença

Este projeto preserva o histórico, os créditos e a licença MIT do **MphRead**.
Não é uma decompilação nova nem um trabalho afiliado à Nintendo. Consulte
[README original](README-UPSTREAM.md), [LICENSE](LICENSE) e
[componentes de terceiros](THIRD-PARTY-PC.md).
