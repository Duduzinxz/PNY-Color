using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

static class AppPaths
{
    public static string Root
    {
        get
        {
            string testRoot=Environment.GetEnvironmentVariable("PNY_COLOR_DATA_DIR");
            if(!string.IsNullOrEmpty(testRoot))return Path.GetFullPath(testRoot);
            string appData=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if(string.IsNullOrEmpty(appData))appData=Environment.GetEnvironmentVariable("APPDATA");
            if(string.IsNullOrEmpty(appData))throw new IOException("User application data folder is unavailable.");
            return Path.Combine(appData,"PNYColor");
        }
    }
    public static string LegacyPreferences {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"cor-salva.txt");}}
}

static class UiLocale
{
    public static readonly string[] Codes={"pt-BR","en","es","fr"};
    public static readonly string[] Names={"Português (Brasil)","English","Español","Français"};
    public static string Code="pt-BR";
    static readonly Dictionary<string,string[]> catalog=new Dictionary<string,string[]>(StringComparer.Ordinal);
    static UiLocale()
    {
        Add("tagline","Controle suas cores.","Control your colors.","Controla tus colores.","Contrôlez vos couleurs.");
        Add("independent","PROJETO INDEPENDENTE","INDEPENDENT PROJECT","PROYECTO INDEPENDIENTE","PROJET INDÉPENDANT");
        Add("nav.color","Cores","Colors","Colores","Couleurs");
        Add("nav.effects","Efeitos","Effects","Efectos","Effets");
        Add("nav.bridge","SignalRGB","SignalRGB","SignalRGB","SignalRGB");
        Add("nav.settings","Preferências","Preferences","Preferencias","Préférences");
        Add("device","SUA PLACA","YOUR GRAPHICS CARD","TU TARJETA GRÁFICA","VOTRE CARTE GRAPHIQUE");
        Add("device.detail","UPRISING · uma zona RGBW","UPRISING · one RGBW zone","UPRISING · una zona RGBW","UPRISING · une zone RGBW");
        Add("device.check","Conexão ao abrir o aplicativo","Connection checked on launch","Conexión al abrir la aplicación","Connexion vérifiée au démarrage");
        Add("color.title","Sua cor, do seu jeito","Your color, your way","Tu color, a tu manera","Votre couleur, à votre façon");
        Add("color.subtitle","Escolha uma cor e veja a prévia antes de aplicar.","Choose a color and preview it before applying.","Elige un color y mira la vista previa antes de aplicarlo.","Choisissez une couleur et prévisualisez-la avant de l’appliquer.");
        Add("preview","PRÉVIA DA COR","COLOR PREVIEW","VISTA PREVIA","APERÇU DE LA COULEUR");
        Add("preview.hint","A seleção só vai para a placa ao aplicar.","Your selection reaches the card when you apply it.","La selección se envía a la tarjeta al aplicarla.","La sélection est envoyée à la carte lors de l’application.");
        Add("pick","Conta-gotas","Eyedropper","Cuentagotas","Pipette");
        Add("hex","Código HEX","HEX code","Código HEX","Code HEX");
        Add("channels","Canais RGB","RGB channels","Canales RGB","Canaux RGB");
        Add("palette","CORES RÁPIDAS","QUICK COLORS","COLORES RÁPIDOS","COULEURS RAPIDES");
        Add("intensity","Intensidade e branco","Brightness and white","Brillo y blanco","Luminosité et blanc");
        Add("brightness","Brilho","Brightness","Brillo","Luminosité");
        Add("white","Canal branco","White channel","Canal blanco","Canal blanc");
        Add("white.hint","Use branco = 0 para cores RGB puras.","Set white to 0 for pure RGB colors.","Usa blanco = 0 para colores RGB puros.","Réglez le blanc sur 0 pour des couleurs RGB pures.");
        Add("apply","Aplicar e salvar","Apply and save","Aplicar y guardar","Appliquer et enregistrer");
        Add("pause","Pausar","Pause","Pausar","Pause");
        Add("restore","Restaurar","Restore","Restaurar","Restaurer");
        Add("read","Ler placa","Read card","Leer tarjeta","Lire la carte");
        Add("effects.title","Dê movimento às suas cores","Bring your colors to life","Da movimiento a tus colores","Animez vos couleurs");
        Add("effects.subtitle","Ajuste o efeito, a velocidade e a segunda cor.","Set your effect, speed and second color.","Ajusta el efecto, la velocidad y el segundo color.","Réglez l’effet, la vitesse et la seconde couleur.");
        Add("effect","Efeito / origem","Effect / source","Efecto / origen","Effet / source");
        Add("speed","Velocidade","Speed","Velocidad","Vitesse");
        Add("second","Segunda cor HEX","Second HEX color","Segundo color HEX","Seconde couleur HEX");
        Add("second.hint","Disponível para a transição entre duas cores.","Available for the two-color transition.","Disponible para la transición entre dos colores.","Disponible pour la transition entre deux couleurs.");
        Add("effects.hint","Os efeitos são experimentais. A pulsação interna da placa pode continuar.","Effects are experimental. The card’s internal pulsing may continue.","Los efectos son experimentales. La pulsación interna puede continuar.","Les effets sont expérimentaux. La pulsation interne peut continuer.");
        Add("effect.0","Cor única","Single color","Color único","Couleur unique");
        Add("effect.1","Arco-íris","Rainbow","Arcoíris","Arc-en-ciel");
        Add("effect.2","Respiração","Breathing","Respiración","Respiration");
        Add("effect.3","Transição entre duas cores","Two-color transition","Transición entre dos colores","Transition entre deux couleurs");
        Add("effect.4","Aurora","Aurora","Aurora","Aurore");
        Add("effect.5","Vela suave","Soft candle","Vela suave","Bougie douce");
        Add("effect.6","Arco-íris com respiração","Breathing rainbow","Arcoíris con respiración","Arc-en-ciel avec respiration");
        Add("effect.7","SignalRGB · ponte local","SignalRGB · local bridge","SignalRGB · puente local","SignalRGB · pont local");
        Add("bridge.title","Suas cores em sincronia","Your colors in sync","Tus colores en sincronía","Vos couleurs synchronisées");
        Add("bridge.subtitle","Receba as cores do SignalRGB neste computador.","Receive SignalRGB colors on this computer.","Recibe los colores de SignalRGB en este equipo.","Recevez les couleurs de SignalRGB sur cet ordinateur.");
        Add("bridge.steps","1   O plugin vem neste executável e é instalado automaticamente.\n\n2   Clique em Usar ponte local para ativar e salvar.\n\n3   Reinicie o SignalRGB após a primeira instalação. Mantenha o PNY Color aberto.","1   This executable includes and automatically installs the plugin.\n\n2   Click Use local bridge to activate and save.\n\n3   Restart SignalRGB after the first installation. Keep PNY Color running.","1   Este ejecutable incluye e instala el plugin automáticamente.\n\n2   Pulsa Usar puente local para activar y guardar.\n\n3   Reinicia SignalRGB tras la primera instalación. Mantén PNY Color abierto.","1   Cet exécutable inclut et installe automatiquement le plugin.\n\n2   Cliquez sur Utiliser le pont local pour activer et enregistrer.\n\n3   Redémarrez SignalRGB après la première installation. Gardez PNY Color ouvert.");
        Add("bridge.select","Usar ponte local","Use local bridge","Usar puente local","Utiliser le pont local");
        Add("bridge.folder","Abrir pasta do plugin","Open plugin folder","Abrir carpeta del plugin","Ouvrir le dossier du plugin");
        Add("bridge.detail","Conexão local · 127.0.0.1:39841\nAté 10 quadros por segundo. Sem dados, a última cor permanece.","Local connection · 127.0.0.1:39841\nUp to 10 frames per second. Without data, the last color is kept.","Conexión local · 127.0.0.1:39841\nHasta 10 fotogramas por segundo. Sin datos, se mantiene el último color.","Connexion locale · 127.0.0.1:39841\nJusqu’à 10 images par seconde. Sans données, la dernière couleur est conservée.");
        Add("settings.title","Deixe o aplicativo com a sua cara","Make the app your own","Personaliza la aplicación","Personnalisez l’application");
        Add("settings.subtitle","Idioma, inicialização e dados salvos.","Language, startup and saved data.","Idioma, inicio y datos guardados.","Langue, démarrage et données enregistrées.");
        Add("language","Idioma do aplicativo","Application language","Idioma de la aplicación","Langue de l’application");
        Add("language.hint","A mudança é imediata e não interrompe o efeito ativo.","Changes apply immediately and keep your active effect running.","El cambio es inmediato y mantiene el efecto activo.","Le changement est immédiat et conserve l’effet actif.");
        Add("startup","Reaplicar minha escolha ao entrar no Windows","Reapply my choice when signing in to Windows","Aplicar mi selección al iniciar Windows","Réappliquer mon choix à l’ouverture de Windows");
        Add("startup.hint","Espera 20 segundos pelo driver. Ative apenas um controlador RGB.","Waits 20 seconds for the driver. Enable only one RGB controller.","Espera 20 segundos al controlador. Activa solo un controlador RGB.","Attend le pilote pendant 20 secondes. Activez un seul contrôleur RGB.");
        Add("data","Dados pessoais","Personal data","Datos personales","Données personnelles");
        Add("data.hint","As preferências ficam na sua conta, separadas da instalação.","Preferences stay in your account, separate from the installation.","Las preferencias se guardan en tu cuenta, fuera de la instalación.","Les préférences restent dans votre compte, séparées de l’installation.");
        Add("data.open","Abrir pasta de preferências","Open preferences folder","Abrir carpeta de preferencias","Ouvrir le dossier des préférences");
        Add("about","PNY Color 2.0 · projeto independente\nControle RGB para a placa validada nesta máquina.","PNY Color 2.0 · independent project\nRGB control for the graphics card validated on this machine.","PNY Color 2.0 · proyecto independiente\nControl RGB para la tarjeta validada en este equipo.","PNY Color 2.0 · projet indépendant\nContrôle RGB de la carte validée sur cet ordinateur.");
        Add("limitation","Esta placa pulsa. Preto e brilho zero retomam o ciclo de fábrica; não apagam os LEDs.","This card pulses. Black and zero brightness restore the factory cycle; they do not turn the LEDs off.","Esta tarjeta pulsa. El negro y el brillo cero restauran el ciclo de fábrica; no apagan los LED.","Cette carte pulse. Le noir et la luminosité zéro rétablissent le cycle d’usine ; ils n’éteignent pas les LED.");
        Add("ready","Pronto para escolher sua cor.","Ready to choose your color.","Listo para elegir tu color.","Prêt à choisir votre couleur.");
        Add("tray.open","Abrir PNY Color","Open PNY Color","Abrir PNY Color","Ouvrir PNY Color");
        Add("tray.pause","Pausar controle","Pause control","Pausar control","Suspendre le contrôle");
        Add("tray.exit","Sair","Exit","Salir","Quitter");
        Add("error.title","Não foi possível concluir","Could not complete the action","No se pudo completar la acción","Impossible de terminer l’action");
        Add("error.detail","Detalhes técnicos:","Technical details:","Detalles técnicos:","Détails techniques :");
        Add("driver.wait","Aguardando o driver após o login...","Waiting for the driver after sign-in...","Esperando al controlador después del inicio...","En attente du pilote après la connexion...");
        Add("saved.loaded","Escolha salva carregada. Clique em Aplicar e salvar para ativar.","Saved selection loaded. Click Apply and save to activate it.","Selección guardada cargada. Pulsa Aplicar y guardar para activarla.","Sélection enregistrée chargée. Cliquez sur Appliquer et enregistrer pour l’activer.");
        Add("paused","Controle pausado. A última cor permanece na placa.","Control paused. The last color stays on the card.","Control pausado. El último color permanece en la tarjeta.","Contrôle suspendu. La dernière couleur reste sur la carte.");
        Add("restored","Cor anterior restaurada. A escolha salva não foi alterada.","Previous color restored. Your saved choice has not changed.","Color anterior restaurado. La selección guardada no ha cambiado.","Couleur précédente restaurée. Votre choix enregistré est conservé.");
        Add("startup.on","Inicialização ativada. O efeito salvo será reaplicado após o login.","Startup enabled. The saved effect will be reapplied after sign-in.","Inicio activado. El efecto guardado se aplicará al iniciar sesión.","Démarrage activé. L’effet enregistré sera réappliqué après la connexion.");
        Add("startup.off","Inicialização automática desativada.","Automatic startup disabled.","Inicio automático desactivado.","Démarrage automatique désactivé.");
        Add("tray.running","O efeito continua perto do relógio. Use Sair para encerrá-lo.","The effect continues in the system tray. Use Exit to stop it.","El efecto continúa en la bandeja. Usa Salir para detenerlo.","L’effet continue dans la zone de notification. Utilisez Quitter pour l’arrêter.");
        Add("pick.cancel","Conta-gotas cancelado. A cor selecionada foi mantida.","Eyedropper cancelled. The selected color was kept.","Cuentagotas cancelado. Se mantiene el color seleccionado.","Pipette annulée. La couleur sélectionnée est conservée.");
        Add("pick.done","Cor capturada: {0}. Clique em Aplicar e salvar para enviar à placa.","Color captured: {0}. Click Apply and save to send it to the card.","Color capturado: {0}. Pulsa Aplicar y guardar para enviarlo a la tarjeta.","Couleur capturée : {0}. Cliquez sur Appliquer et enregistrer pour l’envoyer à la carte.");
        Add("pick.black","Preto capturado, mas esta placa não aceita preto como desligamento. Escolha outra cor.","Black captured, but this card cannot use black to turn off the LEDs. Choose another color.","Negro capturado, pero esta tarjeta no lo admite para apagar los LED. Elige otro color.","Noir capturé, mais cette carte ne l’utilise pas pour éteindre les LED. Choisissez une autre couleur.");
        Add("pick.instructions","Clique: escolher  •  Esc / direito: cancelar","Click: select  •  Esc / right-click: cancel","Clic: elegir  •  Esc / clic derecho: cancelar","Clic : choisir  •  Échap / clic droit : annuler");
        Add("pick.access","Conta-gotas: clique para escolher; Escape cancela","Eyedropper: click to choose; Escape cancels","Cuentagotas: clic para elegir; Escape cancela","Pipette : cliquez pour choisir ; Échap annule");
        Add("bridge.timeout","Sem quadros do SignalRGB há mais de 3 s. Última cor mantida.","No SignalRGB frames for over 3 seconds. Last color kept.","Sin fotogramas de SignalRGB durante más de 3 s. Se mantiene el último color.","Aucune image SignalRGB depuis plus de 3 s. Dernière couleur conservée.");
        Add("bridge.connected","SignalRGB conectado · recebendo cores","SignalRGB connected · receiving colors","SignalRGB conectado · recibiendo colores","SignalRGB connecté · réception des couleurs");
        Add("bridge.wait","Ponte ativa. Aguardando cores do plugin SignalRGB.","Bridge active. Waiting for colors from the SignalRGB plugin.","Puente activo. Esperando colores del plugin SignalRGB.","Pont actif. En attente des couleurs du plugin SignalRGB.");
        Add("bridge.restart","Plugin instalado. Reinicie o SignalRGB para detectar a placa.","Plugin installed. Restart SignalRGB to detect the card.","Plugin instalado. Reinicia SignalRGB para detectar la tarjeta.","Plugin installé. Redémarrez SignalRGB pour détecter la carte.");
        Add("bridge.documents.error","A pasta Documentos do usuário não está disponível.","The user Documents folder is unavailable.","La carpeta Documentos del usuario no está disponible.","Le dossier Documents de l’utilisateur est indisponible.");
        Add("driver.missing","Instale o driver oficial NVIDIA para a RTX 3080 Ti e reinicie o Windows. A biblioteca nvapi64.dll do driver não pôde ser carregada.","Install the official NVIDIA driver for the RTX 3080 Ti and restart Windows. The driver library nvapi64.dll could not be loaded.","Instala el controlador oficial NVIDIA para la RTX 3080 Ti y reinicia Windows. No se pudo cargar nvapi64.dll.","Installez le pilote officiel NVIDIA pour la RTX 3080 Ti et redémarrez Windows. Impossible de charger nvapi64.dll.");
        Add("applied","Cor enviada e salva. A pulsação interna da placa pode continuar.","Color sent and saved. The card’s internal pulsing may continue.","Color enviado y guardado. La pulsación interna puede continuar.","Couleur envoyée et enregistrée. La pulsation interne peut continuer.");
        Add("effect.active","Efeito experimental ativo e salvo.","Experimental effect active and saved.","Efecto experimental activo y guardado.","Effet expérimental actif et enregistré.");
        Add("black.unsupported","Preto e brilho zero retomam o ciclo de fábrica. Escolha outra cor; desligamento ainda não é suportado.","Black and zero brightness restore the factory cycle. Choose another color; turning off LEDs is not supported yet.","El negro y el brillo cero restauran el ciclo de fábrica. Elige otro color; aún no se admite apagar los LED.","Le noir et la luminosité zéro rétablissent le cycle d’usine. Choisissez une autre couleur ; l’extinction n’est pas encore prise en charge.");
        Add("read.done","Cor lida do driver. Nenhuma escrita nesta consulta.","Color read from the driver. This query made no changes.","Color leído del controlador. Esta consulta no realizó cambios.","Couleur lue depuis le pilote. Cette lecture n’a effectué aucun changement.");
        Add("language.done","Idioma atualizado.","Language updated.","Idioma actualizado.","Langue mise à jour.");
        Add("hex.invalid","Informe HEX com seis dígitos, por exemplo #124FFF.","Enter a six-digit HEX code, such as #124FFF.","Introduce un código HEX de seis dígitos, por ejemplo #124FFF.","Saisissez un code HEX à six chiffres, par exemple #124FFF.");
        Diagnostic("Use a versao de 64 bits.","Use the 64-bit version.","Usa la versión de 64 bits.","Utilisez la version 64 bits.");
        Diagnostic("Nao foi possivel abrir a NVAPI do Windows.","Could not load Windows NVAPI.","No se pudo cargar NVAPI de Windows.","Impossible de charger NVAPI de Windows.");
        Diagnostic("NVAPI QueryInterface indisponivel.","NVAPI QueryInterface is unavailable.","NVAPI QueryInterface no está disponible.","NVAPI QueryInterface est indisponible.");
        Diagnostic("Quantidade de GPUs invalida.","Invalid graphics card count.","Cantidad de tarjetas gráficas no válida.","Nombre de cartes graphiques non valide.");
        Diagnostic("Mais de uma placa identica; selecao ambigua.","More than one matching card found; selection is ambiguous.","Se encontró más de una tarjeta idéntica; selección ambigua.","Plusieurs cartes identiques trouvées ; sélection ambiguë.");
        Diagnostic("PNY RTX 3080 Ti UPRISING 1384196E nao encontrada. Nenhuma escrita realizada.","PNY RTX 3080 Ti UPRISING 1384196E not found. No changes were made.","No se encontró la PNY RTX 3080 Ti UPRISING 1384196E. No se realizaron cambios.","PNY RTX 3080 Ti UPRISING 1384196E introuvable. Aucun changement effectué.");
        Diagnostic("Layout de iluminacao diferente do validado. Escrita bloqueada.","Lighting layout differs from the validated layout. Changes are blocked.","El diseño de iluminación difiere del validado. Cambios bloqueados.","La disposition d’éclairage diffère de celle validée. Changements bloqués.");
        Diagnostic("NVAPI nao oferece a funcao ","NVAPI does not provide function ","NVAPI no ofrece la función ","NVAPI ne propose pas la fonction ");
        Diagnostic("Estado RGBW inesperado; nenhuma alteracao sera aplicada.","Unexpected RGBW state; no changes will be applied.","Estado RGBW inesperado; no se aplicarán cambios.","État RGBW inattendu ; aucun changement ne sera appliqué.");
        Diagnostic("O driver aceitou a chamada, mas a leitura retornou outros valores. Clique em Restaurar se necessario.","The driver accepted the call, but readback returned different values. Click Restore if needed.","El controlador aceptó la llamada, pero devolvió otros valores. Pulsa Restaurar si es necesario.","Le pilote a accepté l’appel, mais la lecture a renvoyé d’autres valeurs. Cliquez sur Restaurer si nécessaire.");
        Diagnostic("Cor salva invalida.","Invalid saved color.","Color guardado no válido.","Couleur enregistrée non valide.");
        Diagnostic("Efeito salvo invalido.","Invalid saved effect.","Efecto guardado no válido.","Effet enregistré non valide.");
        Diagnostic("Formato da escolha salva desconhecido.","Unknown saved preference format.","Formato de preferencia guardada desconocido.","Format de préférence enregistrée inconnu.");
        Diagnostic("Ja existe outra entrada PNYColor na inicializacao. Nao foi sobrescrita.","Another PNY Color startup entry already exists. Disable it in Windows Startup Apps before enabling this installation.","Ya existe otra entrada de inicio de PNY Color. Desactívala en las aplicaciones de inicio de Windows antes de activar esta instalación.","Une autre entrée de démarrage PNY Color existe. Désactivez-la dans les applications de démarrage Windows avant d’activer cette installation.");
        Diagnostic("Inicializar NVAPI","Initialize NVAPI","Inicializar NVAPI","Initialiser NVAPI");
        Diagnostic("Enumerar GPUs","Enumerate graphics cards","Enumerar tarjetas gráficas","Énumérer les cartes graphiques");
        Diagnostic("Identificar GPU","Identify graphics card","Identificar tarjeta gráfica","Identifier la carte graphique");
        Diagnostic("Ler zonas","Read zones","Leer zonas","Lire les zones");
        Diagnostic("Ler controladores","Read controllers","Leer controladores","Lire les contrôleurs");
        Diagnostic("Ler cor","Read color","Leer color","Lire la couleur");
        Diagnostic("Aplicar iluminacao","Apply lighting","Aplicar iluminación","Appliquer l’éclairage");
    }
    static void Add(string key,string pt,string en,string es,string fr){catalog.Add(key,new[]{pt,en,es,fr});}
    static void Diagnostic(string pt,string en,string es,string fr){Add("diag."+pt,pt,en,es,fr);}
    public static string D(string message){return catalog.ContainsKey("diag."+message)?Get("diag."+message):message;}
    public static bool Valid(string code){return Array.IndexOf(Codes,code)>=0;}
    public static string Get(string key)
    {
        string[] value;
        if(!catalog.TryGetValue(key,out value))throw new ArgumentException("Missing translation: "+key);
        int index=Array.IndexOf(Codes,Code);return value[Math.Max(0,index)];
    }
    public static string Format(string key,params object[] args){return string.Format(CultureInfo.CurrentCulture,Get(key),args);}
    public static void Initialize()
    {
        try{using(RegistryKey key=Registry.CurrentUser.OpenSubKey("Software\\PNYColor")){string saved=key==null?null:key.GetValue("Language") as string;if(Valid(saved)){Code=saved;return;}}}catch{}
        string culture=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        Code=culture=="pt"?"pt-BR":culture=="es"?"es":culture=="fr"?"fr":"en";
    }
    public static void Save(string code)
    {
        if(!Valid(code))throw new ArgumentException("Invalid language.");
        using(RegistryKey key=Registry.CurrentUser.CreateSubKey("Software\\PNYColor"))key.SetValue("Language",code);
        Code=code;
    }
    public static void SelfTest()
    {
        string prior=Code;
        try
        {
            foreach(string code in Codes)
            {
                Code=code;
                foreach(KeyValuePair<string,string[]> pair in catalog)
                {
                    if(pair.Value.Length!=4||string.IsNullOrWhiteSpace(Get(pair.Key)))throw new Exception("Incomplete translation: "+pair.Key);
                    if(Get(pair.Key).Contains("{0}"))string.Format(Get(pair.Key),"#124FFF");
                }
            }
            if(Valid("invalid"))throw new Exception("Invalid locale accepted.");
            Console.WriteLine("PASS: all UI strings available in Portuguese, English, Spanish and French; placeholders valid.");
        } finally {Code=prior;}
    }
}
