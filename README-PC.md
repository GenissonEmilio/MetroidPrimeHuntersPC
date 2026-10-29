# Hunters PC — primeira versão jogável

Adaptação experimental de **MphRead**, de NoneGiven, para Windows x64. Inclui
menu gráfico, controles de FPS, configurações, mapa local, diário e pausa sobre a mesma tela.
Não é uma campanha completamente validada. A implementação herdada e as
alterações desta versão estão separadas em [Análise técnica](docs/ANALISE-PORT-PC.md).

## Jogar

Abra `HuntersPC.exe` na pasta extraída do pacote Windows. Se você compilou a
versão local, o caminho padrão é `dist/HuntersPC/HuntersPC.exe`. Arraste sua ROM
`.nds` ou seu ZIP para a janela do menu. O código-fonte e os pacotes públicos não
incluem os dados do jogo; a importação é uma etapa local separada.
Entre em **TESTAR CONTROLES** para a arena ou **INICIAR / CONTINUAR** para a aventura.

- **WASD** move; **mouse** mira; **clique esquerdo** dispara; **Espaço** pula.
- **C** transforma; **E** alterna visor; **Q** faz scan/ataque alternativo.
- **Roda** troca armas; **1–9** selecionam as armas obtidas.
- **Esc** pausa e abre configurações; **F11** alterna tela cheia; **F12** captura.
- **M** abre o mapa local; **J** abre o diário; **Tab** alterna entre esses painéis.

O menu recebe cliques e teclado. Ajuste sensibilidade, inversão de Y, dicas,
volumes e tela cheia em **CONFIGURACOES**. Salve a campanha pela nave antes de sair.

Em uma cópia sem dados, arraste sua ROM USA revisão 0 `.nds` ou seu ZIP para
a janela do menu. Os dados são extraídos uma vez na pasta do executável.
O mapa usa a geometria da ROM, marca sua posição e as portas da sala atual e
permite consultar salas já visitadas. No mapa, WASD desloca a planta, roda/+/-
ajustam o zoom, PgUp/PgDn mudam a altura e Home centraliza. Setas/cliques escolhem
a sala e Q/E alternam entre regiões já exploradas. É uma planta local, não a
recriação do mapa 3D regional original do DS.

O diário mostra somente registros desbloqueados no save atual, separados em
equipamento, bioformas, objetos e história. W/S navegam pelos registros, A/D ou
Q/E mudam a categoria e a roda/PgUp/PgDn rolam o texto. Os textos vêm da ROM;
instruções de equipamento recebem também uma dica dos controles de PC.

Mapa e diário pausam a simulação e liberam o cursor. Feche pela mesma tecla,
Esc ou pelo botão **VOLTAR**; o retorno restaura WASD e mira no mouse. Se o painel
foi aberto pela pausa, Esc retorna ao menu de pausa primeiro.
Remapeamento gráfico e validação completa da campanha ainda são trabalho futuro.

## Compilar

Requer .NET SDK 9 ou compatível e acesso ao NuGet. O executável publicado é
autossuficiente, portanto quem joga não precisa instalar o .NET.

```powershell
.\scripts\build-pc.ps1 -RomPath 'C:\caminho\Metroid Prime - Hunters (USA).zip'
```

Se o SDK estiver fora do PATH, use `-DotnetPath 'C:\caminho\dotnet.exe'`.
Sem `-RomPath`, o script gera uma cópia sem dados do jogo. Ele não limpa uma
instalação anterior nem apaga saves. O conteúdo local não faz parte do código.

```powershell
dotnet run --project tests/PcChecks/PcChecks.csproj -c Release
```

Para testar sem disputar mouse/teclado com quem usa o computador, após importar a
ROM na cópia testada:

```powershell
dotnet HuntersPC.dll --pc-verify 'C:\caminho\resultado-da-verificacao'
```

Esse modo usa janelas OpenGL ocultas e eventos internos do jogo; não injeta entrada
no Windows nem captura o cursor. Salva capturas e `verification.json` na pasta
indicada, e isola ali os dados de teste. Confira a
[verificação dos painéis](docs/VALIDACAO-PAINEIS-PC.md).

O antigo fluxo de console do viewer segue acessível por `HuntersPC.exe --legacy`.
Opções técnicas herdadas de exportação permanecem disponíveis pelo DLL com
`dotnet HuntersPC.dll ...`. Diagnósticos de erro ficam em `hunters-error.log`.

## Créditos

MphRead e seu histórico, autores e licença MIT foram preservados. Leia
[README original](README-UPSTREAM.md), [LICENSE](LICENSE),
[terceiros](THIRD-PARTY-PC.md) e [análise](docs/ANALISE-PORT-PC.md).
Nenhuma ROM ou arte extraída deve ser incluída em um pacote público.
