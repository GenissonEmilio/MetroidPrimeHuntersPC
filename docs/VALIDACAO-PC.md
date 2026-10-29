# Verificação desta entrega — 29/10/2026

**Atualização:** a continuação verificou o HUD e implementou mapa e diário sem
disputar a área de trabalho. Veja [verificação dos painéis](VALIDACAO-PAINEIS-PC.md).
O texto abaixo registra a primeira sessão.

- Build Release original: compilou antes das alterações.
- Build Release com adaptação: compilou.
- Publicação final Windows x64 autossuficiente: concluída em `dist/HuntersPC/`.
- `PcChecks`: 16 verificações passaram. Cobrem limites/recuperação e persistência
  de perfil, cliques em 1280×720, 1920×1080, 1024×768 e 2560×1080, espaços entre
  botões, bindings e rejeição de arquivos com tamanho ou checksum incorretos.
- ZIP fornecido: validado como AMHE0 com SHA-256 esperado e extraído na cópia de teste.
- Menu: verificado visualmente em Intel Iris Xe, OpenGL 3.2. O erro inicial de
  contexto gráfico foi corrigido com `ContextFlags.Default`; a captura posterior
  registrou `GL error: NoError`.
- Arena Combat Hall: abriu com cenário, arma e HUD em tela única.
- Esc abriu a pausa na mesma janela; configurações foram acessadas pelo mouse;
  D aumentou a sensibilidade de 1,0 para 1,1 e o JSON persistiu a mudança;
  Esc voltou do painel e retomou a partida.
- A barra adicional de armas e o descarte da janela inicial antes do gameplay
  foram incluídos na compilação final, mas não receberam uma nova verificação visual.

O teste de uso do computador foi interrompido pelo Esc físico do usuário.
Nenhuma outra ação de uso do computador foi realizada após essa interrupção.
Apenas a compilação/publicação que já estava em andamento foi conferida.

A campanha, save/continue, mapa/diário, comparação de velocidade/sensação de mira
com o DS, desempenho sustentado e matriz completa de entrada/foco ainda não
estão certificados. Os avisos NuGet herdados estão descritos em
`ANALISE-PORT-PC.md`. A cópia publicada precisa de importação local da ROM e
não contém dados do jogo.
