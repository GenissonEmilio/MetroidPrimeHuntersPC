# Componentes usados pela adaptação de PC

- **MphRead**, NoneGiven, MIT. Base do projeto e lógica/formatos/renderização do jogo.
  https://github.com/NoneGiven/MphRead — licença em `LICENSE` e créditos no README original.
- **OpenTK 4.9.4**, MIT. Janela, OpenGL e entrada.
  https://github.com/opentk/opentk
- **SixLabors.ImageSharp 3.1.12**, Apache-2.0 conforme a concessão para software
  aberto da licença incluída no pacote original. O arquivo completo é preservado.
  https://github.com/SixLabors/ImageSharp/tree/v3.1.12
- **CommunityToolkit.HighPerformance 8.4.2**, MIT.
  https://github.com/CommunityToolkit/dotnet
- **SoundFlow 1.4.1**, MIT, e suas bibliotecas nativas conforme o pacote.
  https://github.com/LSXPrime/SoundFlow
- **System.IO.Hashing / .NET 10**, MIT.
  https://github.com/dotnet/runtime
- **OpenAL Soft 1.25.2**, LGPL-2.0-or-later, biblioteca dinâmica opcional e substituível.
  Fonte correspondente: https://github.com/kcat/openal-soft/tree/1.25.2
  Binário oficial: https://github.com/kcat/openal-soft/releases/tag/1.25.2
  `COPYING` e `LICENSE-pffft` acompanham o build em `licenses/openal-soft/`.

Os avisos dos pacotes NuGet são copiados pelo script de build para `licenses/`.
Os créditos de formatos, áudio e vídeo herdados de MphRead estão em `README-UPSTREAM.md`.
Os três projetos fornecidos como referência de arquitetura não foram copiados.
Código de interface, alfabeto bitmap e desenho do menu desta adaptação foram
criados nesta sessão. Os dados Nintendo são importados da ROM local do usuário.

No Linux, GLFW e MiniAudio acompanham o pacote NuGet; OpenAL Soft é uma
dependência dinâmica do sistema e não é redistribuída no tarball Linux.
O ícone SVG é desenhado em código para esta adaptação, sem arte extraída da ROM.
