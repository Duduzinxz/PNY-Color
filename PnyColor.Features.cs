using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

enum LightEffect { Estatica, ArcoIris, Respiracao, Transicao, Aurora, Vela, RespiracaoArcoIris, SignalRGB }
sealed class ColorPreferences
{
    public Color Color=Color.White, Second=Color.FromArgb(0,128,255);
    public byte White=0, Brightness=100;
    public int Speed=4;
    public LightEffect Effect=LightEffect.Estatica;
    public static string FilePath {get{return Path.Combine(AppPaths.Root,"cor-salva.txt");}}
    public string Serialize() {return "v2|"+ColorWindow.ToHex(Color)+"|"+White+"|"+Brightness+"|"+(int)Effect+"|"+Speed+"|"+ColorWindow.ToHex(Second);}
    public static ColorPreferences Parse(string text)
    {
        string[] f=text.Trim().Split('|'); ColorPreferences p=new ColorPreferences(); int w,b,e,s;
        if(f.Length==3)
        {
            p.Color=ColorWindow.Parse(f[0]);
            if(!int.TryParse(f[1],out w)||!int.TryParse(f[2],out b)||w<0||w>255||b<0||b>100)throw new Exception(UiLocale.D("Cor salva invalida."));
        }
        else if(f.Length==7 && f[0]=="v2")
        {
            p.Color=ColorWindow.Parse(f[1]);p.Second=ColorWindow.Parse(f[6]);
            if(!int.TryParse(f[2],out w)||!int.TryParse(f[3],out b)||!int.TryParse(f[4],out e)||!int.TryParse(f[5],out s)||w<0||w>255||b<0||b>100||e<0||e>7||s<1||s>10)throw new Exception(UiLocale.D("Efeito salvo invalido."));
            p.Effect=(LightEffect)e;p.Speed=s;
        }
        else throw new Exception(UiLocale.D("Formato da escolha salva desconhecido."));
        p.White=(byte)w;p.Brightness=(byte)b;return p;
    }
    public static ColorPreferences Load() {return Parse(File.ReadAllText(File.Exists(FilePath)?FilePath:AppPaths.LegacyPreferences));}
    public void Save()
    {
        Directory.CreateDirectory(AppPaths.Root);
        string temp=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
        File.WriteAllText(temp,Serialize());
        try {if(File.Exists(FilePath))File.Replace(temp,FilePath,FilePath+".bak");else File.Move(temp,FilePath);}
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
}
struct ColorFrame
{
    public Color Color; public byte White,Brightness;
    public string Key {get{return Color.ToArgb()+":"+White+":"+Brightness;}}
}
static class EffectMath
{
    public static ColorFrame Frame(ColorPreferences p,double seconds)
    {
        if(p.Speed<1||p.Speed>10||p.Brightness<1||p.Brightness>100||seconds<0||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentException("Parametro de efeito invalido. Brilho zero ativa o padrao de fabrica nesta placa.");
        double phase=(seconds/(2.0*(11-p.Speed)))%1.0;
        ColorFrame f=new ColorFrame {Color=p.Color,White=p.White,Brightness=p.Brightness};
        switch(p.Effect)
        {
            case LightEffect.Estatica:break;
            case LightEffect.ArcoIris:f.Color=Hue(phase);f.White=0;break;
            case LightEffect.Respiracao:f.Brightness=(byte)Math.Round(p.Brightness*(0.55+0.45*Math.Cos(phase*Math.PI*2)));break;
            case LightEffect.Transicao:
                double t=(1-Math.Cos(phase*Math.PI*2))/2;
                f.Color=Color.FromArgb(Mix(p.Color.R,p.Second.R,t),Mix(p.Color.G,p.Second.G,t),Mix(p.Color.B,p.Second.B,t));f.White=0;break;
            case LightEffect.Aurora:
                f.Color=Color.FromArgb((int)(30+80*(1+Math.Sin(phase*Math.PI*2))/2),(int)(75+140*(1+Math.Sin(phase*Math.PI*2+2))/2),(int)(170+85*(1+Math.Sin(phase*Math.PI*2+4))/2));f.White=0;break;
            case LightEffect.Vela:
                double warmth=0.5+0.25*Math.Sin(seconds*p.Speed*0.7)+0.15*Math.Sin(seconds*p.Speed*1.13)+0.1*Math.Sin(seconds*p.Speed*0.23);
                f.Color=Color.FromArgb(255,(int)(90+60*warmth),15);f.White=0;f.Brightness=(byte)Math.Round(p.Brightness*(0.72+0.28*warmth));break;
            case LightEffect.RespiracaoArcoIris:
                f.Color=Hue(phase);f.White=0;f.Brightness=(byte)Math.Round(p.Brightness*(0.55+0.45*Math.Cos(phase*Math.PI*4)));break;
            case LightEffect.SignalRGB:throw new InvalidOperationException("SignalRGB exige um quadro da ponte local.");
            default:throw new ArgumentException("Efeito desconhecido.");
        }
        if(f.Brightness==0)f.Brightness=1;
        return f;
    }
    static int Mix(int a,int b,double t) {return (int)Math.Round(a+(b-a)*t);}
    static Color Hue(double phase)
    {
        double h=phase*6;int sector=(int)h;int up=(int)Math.Round((h-sector)*255),down=255-up;
        switch(sector%6) {case 0:return Color.FromArgb(255,up,0);case 1:return Color.FromArgb(down,255,0);case 2:return Color.FromArgb(0,255,up);case 3:return Color.FromArgb(0,down,255);case 4:return Color.FromArgb(up,0,255);default:return Color.FromArgb(255,0,down);}
    }
}
static class StartupSetting
{
    const string Key="Software\\Microsoft\\Windows\\CurrentVersion\\Run", Name="PNYColor";
    public static string Command {get{return "\""+Application.ExecutablePath+"\" --startup";}}
    public static bool Enabled {get{using(RegistryKey key=Registry.CurrentUser.OpenSubKey(Key))return key!=null && string.Equals(key.GetValue(Name) as string,Command,StringComparison.OrdinalIgnoreCase);}}
    public static void Set(bool enabled)
    {
        using(RegistryKey key=Registry.CurrentUser.CreateSubKey(Key))
        {
            string current=key.GetValue(Name) as string;
            if(current!=null && !string.Equals(current,Command,StringComparison.OrdinalIgnoreCase))throw new Exception(UiLocale.D("Ja existe outra entrada PNYColor na inicializacao. Nao foi sobrescrita."));
            if(enabled)key.SetValue(Name,Command);else if(current!=null)key.DeleteValue(Name,false);
        }
    }
}
sealed partial class EffectsWindow : Form
{
    PnyLighting lighting;
    SignalBridge bridge;
    long bridgeSequence=-1;
    bool bridgeTimedOut;
    byte[] previous;
    ColorPreferences active;
    string lastFrame;
    TextBox hex,secondHex;
    NumericUpDown red,green,blue,white,brightness,speed;
    ComboBox effects;
    Panel preview;
    Label status;
    CheckBox startup;
    Button restore;
    NotifyIcon tray;
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=100};
    readonly Stopwatch elapsed=new Stopwatch();
    readonly Stopwatch bootWait=Stopwatch.StartNew();
    readonly EventWaitHandle showEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\PNYColor.Show.v2");
    bool syncing,exiting,bootPending;
    int bootAttempts;
    double nextAttempt=20;
    public EffectsWindow(bool fromStartup, bool designPreview=false)
    {
        bootPending=fromStartup;
        using(Stream iconStream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PNYColor.Icon.ico"))
        {
            if(iconStream==null)throw new InvalidOperationException("Application icon is missing.");
            using(Icon embeddedIcon=new Icon(iconStream,new Size(32,32)))Icon=(Icon)embeddedIcon.Clone();
        }
        BuildUi();
        FormClosed+=delegate
        {
            timer.Stop();timer.Dispose();StopActive();showEvent.Dispose();
            if(tray!=null){tray.Visible=false;tray.Dispose();}
            if(lighting!=null)lighting.Dispose();
            if(Icon!=null)Icon.Dispose();
        };
        if(designPreview)return;
        startup.CheckedChanged+=delegate
        {
            if(syncing)return;bool requested=startup.Checked;
            try{if(requested)ReadSelection().Save();StartupSetting.Set(requested);SetStatus(requested?"startup.on":"startup.off");}
            catch(Exception e){syncing=true;startup.Checked=!requested;syncing=false;Error(e,false);}
        };
        ContextMenuStrip menu=new ContextMenuStrip {BackColor=Color.FromArgb(23,26,36),ForeColor=ForeColor};
        trayOpen=menu.Items.Add(UiLocale.Get("tray.open"),null,delegate{ShowWindow();});
        trayPause=menu.Items.Add(UiLocale.Get("tray.pause"),null,delegate{PauseControl();});
        trayExit=menu.Items.Add(UiLocale.Get("tray.exit"),null,delegate{exiting=true;Close();});
        tray=new NotifyIcon {Icon=Icon,Text="PNY Color",ContextMenuStrip=menu,Visible=true};
        tray.DoubleClick+=delegate{ShowWindow();};FormClosed+=delegate{menu.Dispose();};
        Resize+=delegate{if(WindowState==FormWindowState.Minimized)Hide();};
        Shown+=delegate
        {
            bool bridgeChanged=false;
            try {bridgeChanged=BridgePackage.Prepare();}
            catch(Exception e){Error(e,false);}
            if(fromStartup){Hide();SetStatus("driver.wait");}
            else Run(delegate
            {
                Connect();ShowState();
                if(File.Exists(ColorPreferences.FilePath)||File.Exists(AppPaths.LegacyPreferences))
                {
                    ColorPreferences saved=ColorPreferences.Load();ShowPreferences(saved);SetStatus("saved.loaded");
                    if(saved.Effect==LightEffect.SignalRGB){Start(saved);SetStatus(bridgeChanged?"bridge.restart":"bridge.wait");}
                }
            });
            timer.Start();
        };
        FormClosing+=delegate(object sender,FormClosingEventArgs e)
        {
            if(!exiting&&e.CloseReason==CloseReason.UserClosing&&active!=null&&active.Effect!=LightEffect.Estatica)
            {e.Cancel=true;Hide();tray.ShowBalloonTip(3000,"PNY Color",UiLocale.Get("tray.running"),ToolTipIcon.Info);}
        };
        timer.Tick+=Tick;
    }
    void PickScreenColor()
    {
        Run(delegate
        {
            Color picked;
            if(!ScreenColorPicker.Pick(this,out picked)){SetStatus("pick.cancel");return;}
            SetColor(picked);white.Value=0;effects.SelectedIndex=0;
            status.Text=UiLocale.Format("pick.done",ColorWindow.ToHex(picked));
            if(picked.R==0&&picked.G==0&&picked.B==0)SetStatus("pick.black");
        });
    }
    void Connect(){if(lighting==null)lighting=new PnyLighting();}
    void Tick(object sender,EventArgs args)
    {
        if(showEvent.WaitOne(0))ShowWindow();
        if(bootPending)
        {
            if(bootWait.Elapsed.TotalSeconds<nextAttempt)return;
            try{Connect();ColorPreferences p=ColorPreferences.Load();ShowPreferences(p);Start(p);bootPending=false;Log("Inicializacao: escolha aplicada.");}
            catch(Exception e){bootAttempts++;nextAttempt=bootWait.Elapsed.TotalSeconds+5;if(lighting!=null){lighting.Dispose();lighting=null;}if(bootAttempts>=6){bootPending=false;Error(e,true);}else Log("Aguardando driver: "+e.Message);}return;
        }
        if(active==null || active.Effect==LightEffect.Estatica)return;
        if(active.Effect==LightEffect.SignalRGB)
        {
            try
            {
                ColorFrame f;long sequence;double age;
                if(bridge!=null && bridge.TryLatest(out f,out sequence,out age))
                {
                    if(age>3){if(!bridgeTimedOut){bridgeTimedOut=true;SetStatus("bridge.timeout");}return;}
                    if(sequence!=bridgeSequence){f.Brightness=(byte)Math.Round(f.Brightness*active.Brightness/100.0);Push(f);bridgeSequence=sequence;bridgeTimedOut=false;SetStatus("bridge.connected");}
                }
            }
            catch(Exception e){StopActive();Error(e,true);}return;
        }
        try{Push(EffectMath.Frame(active,elapsed.Elapsed.TotalSeconds));}
        catch(Exception e){StopActive();Error(e,true);}
    }
    void ShowWindow(){Show();WindowState=FormWindowState.Normal;Activate();}
    void ApplySelected(){Run(delegate{bootPending=false;Connect();ColorPreferences p=ReadSelection();Start(p);try{p.Save();}catch{StopActive();throw;}SetStatus(p.Effect==LightEffect.SignalRGB?"bridge.wait":p.Effect==LightEffect.Estatica?"applied":"effect.active");});}
    void StopActive(){active=null;if(bridge!=null){bridge.Dispose();bridge=null;}bridgeSequence=-1;bridgeTimedOut=false;}
    void Start(ColorPreferences p){if(p.Brightness==0 || (p.Color.R==0 && p.Color.G==0 && p.Color.B==0 && p.Effect!=LightEffect.SignalRGB))throw new Exception(UiLocale.Get("black.unsupported"));previous=lighting.ReadControl();restore.Enabled=true;StopActive();lastFrame=null;if(p.Effect==LightEffect.SignalRGB){bridge=new SignalBridge();SetStatus("bridge.wait");}else Push(EffectMath.Frame(p,0));active=p;elapsed.Restart();}
    void Push(ColorFrame frame){if(frame.Brightness==0 || (frame.Color.R==0 && frame.Color.G==0 && frame.Color.B==0))return;if(frame.Key==lastFrame)return;lighting.Apply(frame.Color,frame.White,frame.Brightness);lastFrame=frame.Key;preview.BackColor=frame.Color;UpdateColorLabels(frame.Color);}
    ColorPreferences ReadSelection(){return new ColorPreferences {Color=ColorWindow.Parse(hex.Text),Second=ColorWindow.Parse(secondHex.Text),White=(byte)white.Value,Brightness=(byte)brightness.Value,Speed=(int)speed.Value,Effect=(LightEffect)effects.SelectedIndex};}
    void ShowPreferences(ColorPreferences p){SetColor(p.Color);secondHex.Text=ColorWindow.ToHex(p.Second);white.Value=p.White;brightness.Value=Math.Max(1,(int)p.Brightness);speed.Value=p.Speed;effects.SelectedIndex=(int)p.Effect;}
    void ShowState(){Connect();byte[] s=lighting.ReadControl();SetColor(PnyLighting.ColorOf(s));white.Value=PnyLighting.WhiteOf(s);brightness.Value=Math.Max(1,PnyLighting.BrightnessOf(s));SetStatus("read.done");}
    void SetColor(Color c){syncing=true;try{hex.Text=ColorWindow.ToHex(c);red.Value=c.R;green.Value=c.G;blue.Value=c.B;preview.BackColor=c;UpdateColorLabels(c);}finally{syncing=false;}}
    void ChannelChanged(object sender,EventArgs e){if(!syncing)SetColor(Color.FromArgb((int)red.Value,(int)green.Value,(int)blue.Value));}
    void Run(Action action){try{action();}catch(Exception e){Error(e,false);}}
    void Error(Exception e,bool background)
    {
        status.Text=UiLocale.Get("error.title")+". "+e.Message;hints.SetToolTip(status,status.Text);Log(e.Message);
        string message=UiLocale.Get("error.title")+".\n\n"+UiLocale.Get("error.detail")+"\n"+e.Message;
        if(background&&tray!=null)tray.ShowBalloonTip(6000,"PNY Color",message,ToolTipIcon.Warning);
        else MessageBox.Show(this,message,"PNY Color",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    }
    static void Log(string message){try{Directory.CreateDirectory(AppPaths.Root);string path=Path.Combine(AppPaths.Root,"PnyColor.log");if(File.Exists(path)&&new FileInfo(path).Length>131072)File.Move(path,path+"."+DateTime.Now.ToString("yyyyMMddHHmmss")+".bak");File.AppendAllText(path,DateTime.Now.ToString("s")+" "+message+Environment.NewLine);}catch{}}
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);try{int enabled=1;DwmSetWindowAttribute(Handle,20,ref enabled,4);}catch{}}
}
static class FeatureTests
{
    static void Assert(bool test,string label){if(!test)throw new Exception("Teste falhou: "+label);}
    public static void Run()
    {
        ColorPreferences legacy=ColorPreferences.Parse("#FFFFFF|255|100");Assert(legacy.White==255 && legacy.Effect==LightEffect.Estatica,"migracao v1");
        ColorPreferences p=ColorPreferences.Parse("v2|#FF8000|0|75|3|4|#0080FF");Assert(ColorPreferences.Parse(p.Serialize()).Serialize()==p.Serialize(),"persistencia v2");
        Assert(EffectMath.Frame(p,0).Color.ToArgb()==p.Color.ToArgb(),"transicao inicio");Assert(EffectMath.Frame(p,7).Color.ToArgb()==p.Second.ToArgb(),"transicao destino");
        foreach(LightEffect effect in Enum.GetValues(typeof(LightEffect)))if(effect!=LightEffect.SignalRGB)for(int speed=1;speed<=10;speed++){p.Effect=effect;p.Speed=speed;for(int i=0;i<=200;i++){ColorFrame f=EffectMath.Frame(p,i/10.0);Assert(f.Brightness<=75,"limite brilho");if(effect!=LightEffect.Estatica && effect!=LightEffect.Respiracao)Assert(f.White==0,"branco efeitos");}}
        string[] bad={"v2|#FF0000|0|101|0|4|#000000","v2|#FF0000|0|100|8|4|#000000","v2|#FF0000|0|100|0|0|#000000","#FFF|0|100"};
        foreach(string value in bad){bool rejected=false;try{ColorPreferences.Parse(value);}catch{rejected=true;}Assert(rejected,"config invalida");}
        UiLocale.SelfTest();
        EffectsWindow.UiSelfTest();
        PersistenceTests.Run();
        SignalBridge.SelfTest();
        BridgePackage.SelfTest();
        ScreenColorPicker.SelfTest();
        Console.WriteLine("PASS: migracao/configuracao, sete modos locais, velocidades, limites e transicao.");
    }
}
