# PNY Color 2.1 — pacote completo

## Instalar depois de formatar

1. Instale o driver oficial NVIDIA da RTX 3080 Ti e reinicie o Windows.
2. Para sincronizar com o SignalRGB, instale o SignalRGB.
3. Feche o PNY Color anterior, inclusive o ícone perto do relógio.
4. Execute **PNY-Color-Completo-2.1.0.exe**. A instalação é para seu usuário, sem exigir administrador.
5. Abra o PNY Color e reinicie o SignalRGB. A placa aparece como **PNY RTX 3080 Ti — ponte local**, em Outros dispositivos.

O instalador inclui todos os arquivos do projeto e instala o plugin automaticamente em **Documentos/WhirlwindFX/Plugins**. Não é preciso copiar arquivos manualmente. A pasta Documentos é obtida do Windows, inclusive quando redirecionada.

O driver NVIDIA e o aplicativo SignalRGB são dependências externas e não estão incluídos. O SignalRGB só é necessário para sincronização; as cores e efeitos próprios do PNY Color funcionam sem ele. Requer Windows 10/11 de 64 bits com o .NET Framework do Windows disponível. O pacote não baixa componentes durante a instalação.

## Executável portátil

O **PnyColor.exe** também contém o plugin dentro dele. Pode ser copiado sozinho: ao abrir, instala ou repara o plugin na pasta do usuário. Uma versão diferente do plugin é preservada em `.bak` antes da substituição.

Na primeira configuração, se o SignalRGB for detectado, a ponte é selecionada automaticamente. Preferências existentes são preservadas. Para ativar depois, abra SignalRGB e clique em **Usar ponte local**; o botão instala/repara, ativa e salva a integração.

Mantenha o PNY Color aberto (pode ficar na bandeja) para receber as cores. O modo SignalRGB salvo é retomado ao abrir o programa. A inicialização com o Windows continua opcional em Preferências. Após a primeira instalação ou atualização do plugin, reinicie o SignalRGB.

## Cores, dados e limitações

- Selecione a cor e clique em **Aplicar e salvar**. Para cores RGB puras, use canal branco 0.
- Compatibilidade restrita à **PNY RTX 3080 Ti UPRISING**, PCI `220810DE / 1384196E`, com o layout validado.
- **A pulsação existente da placa não foi corrigida nesta versão.** Preto e brilho zero retomam o ciclo de fábrica; não desligam os LEDs. Os efeitos são experimentais.
- O aplicativo não altera BIOS, clocks, tensão ou ventoinhas. A integração usa UDP em `127.0.0.1:39841`.
- Preferências e log ficam em `%APPDATA%/PNYColor`. A preferência portátil antiga `cor-salva.txt` é preservada e pode ser migrada.
- A desinstalação preserva preferências e o plugin em Documentos. Sem PNY Color aberto, esse plugin não controla a placa. A entrada de inicialização só é removida se pertencer à instalação desinstalada.

## English

Run **PNY-Color-Completo-2.1.0.exe**. The installer and portable **PnyColor.exe** include the SignalRGB bridge and install/repair it automatically. Restart SignalRGB after the first installation; keep PNY Color running. Saved bridge mode resumes on launch. NVIDIA drivers and SignalRGB itself are separate dependencies. Existing card pulsing is not fixed by this release.

## Español

Ejecuta **PNY-Color-Completo-2.1.0.exe**. El instalador y **PnyColor.exe** incluyen e instalan/reparan automáticamente el plugin. Reinicia SignalRGB tras la primera instalación y mantén PNY Color abierto. El controlador NVIDIA y SignalRGB se instalan por separado. Esta versión no corrige la pulsación existente.

## Français

Exécutez **PNY-Color-Completo-2.1.0.exe**. L’installateur et **PnyColor.exe** incluent et installent/réparent automatiquement le plugin. Redémarrez SignalRGB après la première installation et gardez PNY Color ouvert. Le pilote NVIDIA et SignalRGB s’installent séparément. Cette version ne corrige pas la pulsation existante.

## Recompilar e verificar

`build.ps1` compila com o .NET Framework do Windows e incorpora o plugin. `build-installer.ps1 -Compiler <caminho-do-ISCC.exe>` gera o instalador com Inno Setup 6.7.3.

`PnyColor.exe --self-test` verifica controlador sem escrever na GPU, configurações, idiomas, interface, efeitos, ponte UDP e empacotamento. `--packaging-self-test` verifica o plugin e as preferências iniciais em diretórios temporários. `--install-bridge` prepara a integração do usuário sem acessar a GPU.
