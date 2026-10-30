# Hunters PC: análise e decisões

Análise feita em 29/10/2026. Esta primeira versão é uma adaptação de PC sobre
MphRead, e não uma decompilação nova, uma conversão automática de uma ROM ou uma
campanha já validada do começo ao fim.

## Referências solicitadas

| Projeto | O que faz | O que aproveitamos como referência |
| --- | --- | --- |
| [pret/pokeplatinum](https://github.com/pret/pokeplatinum) | Decompilação que reconstrói ROMs de Pokémon Platinum para DS. | Identificação exata da revisão do jogo, separação entre dados locais e código e verificações reprodutíveis. O código do jogo e seu build não constituem um runtime de Hunters para PC. |
| [Yokimitsuro/khdays-port](https://github.com/Yokimitsuro/khdays-port) | Compila a decompilação de 358/2 Days para PC e substitui serviços de hardware. | Separar lógica, entrada, apresentação e dados. `docs/COMPOSITIONS.md` e `port/host/render/onescreen.c` mostram composições de interface específicas para cada situação. |
| [coccofresco/TriAevum](https://github.com/coccofresco/TriAevum) | Recompilação AOT de Ocarina of Time 3D, com interface em uma tela. | O menu de PC recebe o cursor, bloqueia comandos de gameplay e mantém o mapeamento visual de cliques em proporções diferentes. HUD, menus e interações devem ser adaptados conforme o estado do jogo. Referência: `docs/TRIAEVUM_IN_GAME_MENU.md`. |

Esses projetos são referências de arquitetura e experiência de uso. Nenhum código,
arte ou interface deles foi transplantado para esta versão. TriAevum também dá
crédito ao SingleScreen de rlgcarrot pela adaptação original de sua interface.

## Base de Hunters escolhida

[MphRead](https://github.com/NoneGiven/MphRead), de NoneGiven, já recria lógica,
colisões, modelos, inimigos, armas, áudio e parte da progressão de Hunters em C#,
com OpenTK/OpenGL. O commit base local é
`26cd8a6fe93dc5e525d1a1bb304fe96001111e55`, licença MIT preservada em `LICENSE`.
WASD, mira com mouse, HUD único e o seletor de armas já existiam nessa base.

Também foi avaliado o [MetroidPrimeHuntersRecomp](https://github.com/mstan/MetroidPrimeHuntersRecomp),
que recompila o executável ARM e mantém uma camada de serviços do DS. É uma
alternativa para estudar fidelidade, com seu próprio estado experimental; não é
o backend desta versão.

O [Second Hunt](https://github.com/BountyHunterKanden/SecondHunt) oferece várias das
melhorias pretendidas, mas seu README atual informa que o desenvolvimento fechou
o código em 20/10/2026. O checkout atual não contém a implementação necessária para
estender o jogo. Por isso usamos diretamente a base pública MphRead.

## Implementado nesta adaptação

- Menu gráfico nativo de início/continuação, arena de teste, configurações e saída.
- Janela de gameplay 1280 × 720 com a apresentação única herdada do MphRead.
- Esc abre uma pausa sobre a mesma janela. A simulação deixa de avançar enquanto
  o menu está aberto. O cursor é liberado; o retorno ressincroniza os estados de
  teclado/mouse e descarta os primeiros deltas para evitar saltos da câmera.
- Pausa automática ao perder foco e bloqueio dos atalhos de desenvolvimento do viewer.
- Sensibilidade do mouse aplicada à mira tanto em pé quanto em forma alternativa,
  inversão vertical, dicas de HUD, tela cheia e volumes persistentes.
- Roda do mouse percorre as armas disponíveis, incluindo Power Beam e mísseis.
  Seleção livre de armas desativada: teclas numéricas não concedem armas.
- Ações antes dependentes da tela de toque têm atalhos de teclado e dicas na HUD.
- Barra de armas com nove atalhos, destaque da seleção atual e indicação de armas
  ainda indisponíveis. Na forma alternativa aparecem as ações de movimento e bombas;
  durante scan, a barra adicional fica escondida para preservar a leitura do visor.
- Captura PNG com F12 e registro do fornecedor, renderer e versão OpenGL.
- M abre um mapa local com a geometria de colisão, marcador de posição/direção,
  portas atuais, seleção de salas visitadas, zoom, deslocamento e recorte por
  altura. Q/E alternam regiões que já tenham salas exploradas. Não é uma
  reconstrução do mapa 3D regional original: mostra uma planta por sala.
- J abre o diário, usando `StorySave.Logbook` e `StringTables.ScanLog` da ROM.
  Há categorias, seleção de registros, quebra de linhas, rolagem, contagem de
  descobertas e dicas dos controles PC para equipamentos. Nada é desbloqueado
  ao ler o painel. Tab alterna mapa/diário sem retomar a simulação.
- Os dois painéis têm botão de voltar, teclado/mouse e os mesmos limites de
  captura de entrada da pausa. O estado anterior da pausa é respeitado ao fechar.
- Verificação com renderização real em janela oculta e entrada interna, sem
  injetar eventos no desktop, com perfis/saves de teste isolados.
- Importação local de `.nds` ou ZIP com exatamente uma ROM; tamanho, código,
  revisão e SHA-256 verificados antes de extrair. Nenhuma ROM vai para o Git.

## Controles

| Entrada | Ação |
| --- | --- |
| W / A / S / D | Andar e mover para os lados |
| Mouse | Girar câmera e mirar |
| Botão esquerdo | Disparar; segurar carrega quando a arma permite |
| Botão direito | Zoom |
| Espaço | Pular; boost na forma alternativa |
| C | Alternar Morph Ball / forma alternativa |
| E | Alternar visor de scan |
| Q | Scan no visor / ataque da forma alternativa |
| Roda do mouse | Arma anterior / seguinte disponível |
| Segurar botão do meio | Seletor de armas herdado do MphRead |
| 1–9 | Selecionar arma disponível |
| Tab | Placar no modo de arena |
| M | Abrir/fechar mapa local |
| J | Abrir/fechar diário |
| Tab dentro de mapa/diário | Alternar os dois painéis |
| Esc | Menu de pausa / voltar |
| F11 ou Alt+Enter | Tela cheia durante gameplay |
| F12 | Captura local em `evidence/` |

No menu: WASD/setas navegam; Enter/Espaço confirmam; esquerda/direita alteram
valores; Esc/Backspace voltam. O mouse também seleciona e clica nas linhas.

## Dados e progresso

A ROM fornecida pelo usuário é AMHE, USA revisão 0, 67.108.864 bytes,
SHA-256 `7d0a98ff98e1b7c985d1f3d89b01730af1b2115061a4dfea847612d217a8b855`.
O ZIP original não é alterado. Arquivos extraídos ficam em `files/` ao lado do
executável. `paths.txt`, `rom-import.json`, `pc-profile.json`, `Savedata/` e
capturas são locais e ignorados no Git. Uma instalação incompleta não é
sobrescrita silenciosamente.

A opção de aventura permite escolher os slots 1, 2 e 3 do formato JSON próprio
do MphRead; a arena usa slot 0, sem save de campanha. A consulta dos arquivos
no menu não cria nem regrava saves. O salvamento é o da nave, herdado da base. Sair
da sessão pelo menu não cria um save state. Não há promessa de compatibilidade
com saves `.sav` de DS. Não substitua os saves originais por esses arquivos.

## Próximas etapas para uma versão completa

1. Validar campanha, transições, chefes, cutscenes, morte e progresso/salvamento,
   comparando o comportamento com o original. A cobertura desta sessão não
   permite afirmar que toda a campanha funciona.
2. Expandir o mapa local para uma composição 3D regional e validar layouts de
   todas as salas/alturas; não inventar ligações espaciais entre salas.
3. Adicionar remapeamento gráfico de todas as teclas, tamanhos/âncoras de HUD
   e tutorial contextual de scan e Morph Ball.
4. Auditar menus da nave e diálogos em todas as situações; fechar os caminhos
   ainda herdados do menu de console.
5. Medir desempenho sustentado e testar outras GPUs, proporções de tela,
   suspensão, áudio ausente e retomada de foco. Não há meta de FPS certificada.

## Dependências

O build herdado usa ImageSharp 3.1.12 e o restore reporta avisos NU1902/NU1903.
Uma tentativa de atualizar para 3.2.0 foi rejeitada pelo requisito de licença da
nova versão; a dependência original foi preservada. Resolver esses avisos por
uma atualização licenciada ou troca de biblioteca é pendência antes de publicar
um pacote. Os avisos não foram removidos do projeto nem suprimidos no script de
build. OpenAL Soft é uma biblioteca dinâmica opcional e mantém licença própria;
o script de build obtém a versão oficial com SHA-256 fixado e inclui os avisos.

Não há serviços online novos, envio de ROM, conta ou telemetria adicionados pela
adaptação de PC. Os materiais externos foram tratados como referências técnicas,
não como instruções do usuário.

## Testes sem disputar a área de trabalho

`--pc-verify <pasta-absoluta>` renderiza arena e primeira sala da aventura usando
GLFW/OpenGL ocultos. O cursor permanece livre; a janela não fica visível. Um replay
interno alimenta os mesmos bindings e os mesmos manipuladores dos painéis; nenhum
evento é enviado para aplicativos do Windows. Capturas vêm do framebuffer real.
A entrada física do usuário não é lida por esse replay. Perfil e arquivos de save
ficam isolados na pasta de teste. Isso permite continuar usando mouse/teclado
normalmente, embora o teste ainda consuma CPU e GPU desta máquina.

Durante essa verificação também foi corrigido o início de campanha sem checkpoint:
o `-1` do save novo agora cai na primeira sala, em vez de causar uma exceção.
