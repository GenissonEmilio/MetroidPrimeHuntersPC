# Validação Linux — v0.2.0

Ambiente local: Fedora 44 x86-64, sessão GNOME Wayland com XWayland,
Intel RPL-P / Mesa 26.2.4, OpenGL 4.6 de compatibilidade, OpenAL Soft e .NET 10.
O pacote público inclui o runtime e usa X11/XWayland explicitamente.

## Verificações

- `dotnet run --project tests/PcChecks/PcChecks.csproj -c Release`: 37 verificações,
  incluindo diretórios XDG, isolamento de dados, separadores de arquivos e rejeição
  de cabeçalhos falsos de ambas as revisões da ROM.
- `python3 -m unittest discover -s tests -p 'test_*package.py'`: 11 testes de
  pacotes Windows/Linux, checksums, ausência de ROM/saves, travessia de caminhos,
  links, duplicatas, permissões de execução e atualização preservando dados.
- `HuntersPC.sh --pc-smoke <pasta>`: menu e configurações renderizados em OpenGL,
  persistência, redimensionamento 4:3, contexto OpenAL e backend MiniAudio reais.
- `HuntersPC.sh --pc-verify <pasta>`: 54 verificações e 21 capturas com a ROM
  AMHP1 (Europa revisão 1), arena Combat Hall e primeira sala da aventura.
  Abrange WASD, mouse, Morph Ball, mapa, diário, pausa, retorno e escalas de tela.
- O CI compila e audita os dois sistemas; o Linux verifica também o pacote
  extraído em Ubuntu 22.04, Xvfb, Mesa e dispositivo de áudio virtual.

As evidências de runtime locais ficam em `evidence/`, ignoradas pelo Git.
Os relatórios locais contêm caminhos privados e não acompanham a release.
Nenhum arquivo da ROM ou save de teste é necessário no CI ou distribuído.

## Correção encontrada pelo teste

Ao pausar a simulação para abrir os painéis, `Scene.OnUpdateFrame` deixava de
executar. Esse método também restaurava shader/framebuffer do mundo; assim, o
render seguinte tentava usar uniformes da cena com o shader de composição.
Mesa reportava `GL_INVALID_OPERATION`. O render agora restaura esse estado
antes de limpar e desenhar, independentemente da atualização da simulação.

## Limites

A campanha completa, todas as transições/saves, multiplayer e outras GPUs não
foram certificados. A ROM USA revisão 0 mantém o suporte anterior; a rodada de
gameplay Linux usa a Europa revisão 1 fornecida localmente. Os avisos da
ImageSharp herdada permanecem documentados na análise técnica.

Referências de plataforma: [publicação autossuficiente .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/),
[suporte .NET 10](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
e [dependências GLFW](https://www.glfw.org/docs/3.4/compile.html).
