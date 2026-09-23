# Validação PNY Color 2.1

## Executável e plugin

- O executável foi copiado sozinho para `validation/portable-only`, sem DLLs, plugin externo, preferências ou imagens adjacentes.
- Todos os autotestes passaram nessa cópia: layouts RGBW, preferências, traduções, interface, efeitos, recepção UDP, conta-gotas e empacotamento. Esses testes não escrevem na GPU.
- O teste do pacote instalou o plugin em pasta nova com espaços e acentos, verificou conteúdo idêntico ao recurso, repetição sem regravação, backup de versão diferente e recuperação de arquivo apagado.
- Foram verificadas preferências iniciais com e sem SignalRGB, preservação de escolha existente e de configuração legada.
- A cópia isolada executou `--install-bridge` com sucesso no perfil atual, preservando os bytes das preferências e o hash do plugin já instalado.
- A tela de integração atualizada foi renderizada e inspecionada em português e francês.

## Instalador

- Compilado com Inno Setup 6.7.3 como instalação por usuário.
- Instalação e desinstalação passaram em português, inglês, espanhol e francês, em diretórios com espaços e acentos.
- O hash do executável instalado corresponde ao executável compilado; todos os arquivos previstos estavam presentes.
- O teste usa uma identidade de instalação separada. O passo pós-instalação executa o autoteste do pacote em vez de alterar o plugin real. O comando de preparação real foi validado separadamente com a cópia isolada.
- Idioma e entrada de inicialização existentes foram preservados.

## Limites da verificação

Esta validação ocorreu no Windows atual, não em uma máquina virtual recém-formatada. O pacote contém os arquivos do projeto, inclusive a ponte; não contém o driver NVIDIA nem o aplicativo SignalRGB. Requer Windows 10/11 x64 com .NET Framework disponível.

O controle da GPU permanece restrito à identificação e ao layout anteriormente validados. O empacotamento não corrige a pulsação física já relatada. Não foram adicionados comandos de BIOS, tensão, clocks ou ventoinhas.

Entregas: `PNY-Color-Completo-2.1.0.exe` (instalador) e `PNY-Color-Portatil-2.1.0.exe` (aplicativo único). Hashes em `PNY-Color-2.1-SHA256.txt`.
