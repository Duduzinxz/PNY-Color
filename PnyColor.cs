// PNY Color: narrow NVAPI RGBW controller for PCI 220810DE / 1384196E.
// Does not expose I2C, firmware, fan, clock, voltage or power APIs.
// NVAPI interface and layouts: https://github.com/NVIDIA/nvapi (MIT).
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

sealed class PnyLighting : IDisposable
{
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll", CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr module, string name);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Query(uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Simple();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Enumerate([Out] IntPtr[] handles, out uint count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Pci(IntPtr gpu, out uint device, out uint subsystem, out uint revision, out uint extended);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int Info(IntPtr gpu, IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate int ErrorText(int code, StringBuilder text);
    const int ControlSize = 6476, ZoneStart = 76, ZoneStride = 200;
    IntPtr library, gpu;
    Query query;
    ErrorText errorText;
    bool initialized;
    public PnyLighting()
    {
        try
        {
            if (!Environment.Is64BitProcess) throw new Exception(UiLocale.D("Use a versao de 64 bits."));
            library = LoadLibraryEx("nvapi64.dll", IntPtr.Zero, 0x800); // System32 only
            if (library == IntPtr.Zero) throw new Exception(UiLocale.Get("driver.missing"));
            IntPtr entry = GetProcAddress(library, "nvapi_QueryInterface");
            if (entry == IntPtr.Zero) throw new Exception(UiLocale.D("NVAPI QueryInterface indisponivel."));
            query = (Query)Marshal.GetDelegateForFunctionPointer(entry, typeof(Query));
            errorText = Get<ErrorText>(0x6c2d048c);
            Check(Get<Simple>(0x0150e828)(), UiLocale.D("Inicializar NVAPI")); initialized = true;
            IntPtr[] handles = new IntPtr[64]; uint count;
            Check(Get<Enumerate>(0xe5ac921f)(handles, out count), UiLocale.D("Enumerar GPUs"));
            if (count > 64) throw new Exception(UiLocale.D("Quantidade de GPUs invalida."));
            for (int i=0; i<count; i++)
            {
                uint device, subsystem, revision, extended;
                Check(Get<Pci>(0x2ddfb66e)(handles[i], out device, out subsystem, out revision, out extended), UiLocale.D("Identificar GPU"));
                if (device == 0x220810de && subsystem == 0x1384196e)
                {
                    if (gpu != IntPtr.Zero) throw new Exception(UiLocale.D("Mais de uma placa identica; selecao ambigua."));
                    gpu = handles[i];
                }
            }
            if (gpu == IntPtr.Zero) throw new Exception(UiLocale.D("PNY RTX 3080 Ti UPRISING 1384196E nao encontrada. Nenhuma escrita realizada."));
            byte[] zones = Read(0x4b81241b, 4552, UiLocale.D("Ler zonas"));
            byte[] devices = Read(0xd4100e58, 4424, UiLocale.D("Ler controladores"));
            if (Int(zones,4)!=2 || Int(zones,72)!=3 || zones[76]!=0 || Int(zones,212)!=4 || Int(devices,4)!=2 || Int(devices,72)!=2 || (Int(devices,76)&1)==0)
                throw new Exception(UiLocale.D("Layout de iluminacao diferente do validado. Escrita bloqueada."));
            Validate(ReadControl());
        }
        catch { Dispose(); throw; }
    }
    T Get<T>(uint id) where T:class
    {
        IntPtr address = query(id);
        if(address==IntPtr.Zero) throw new Exception(UiLocale.D("NVAPI nao oferece a funcao ") + id.ToString("X8"));
        return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
    }
    void Check(int code,string action)
    {
        if(code==0) return;
        StringBuilder text=new StringBuilder(64);
        if(errorText!=null) errorText(code,text);
        throw new Exception(action+": "+code+" "+text);
    }
    static int Int(byte[] data,int offset) { return BitConverter.ToInt32(data,offset); }
    static void PutInt(byte[] data,int offset,int value) { Buffer.BlockCopy(BitConverter.GetBytes(value),0,data,offset,4); }
    byte[] Read(uint id,int size,string action)
    {
        byte[] data=new byte[size]; PutInt(data,0,size|(1<<16));
        IntPtr memory=Marshal.AllocHGlobal(size);
        try { Marshal.Copy(data,0,memory,size); Check(Get<Info>(id)(gpu,memory),action); Marshal.Copy(memory,data,0,size); return data; }
        finally { Marshal.FreeHGlobal(memory); }
    }
    public byte[] ReadControl() { byte[] data=Read(0x3dbf5764,ControlSize,UiLocale.D("Ler cor")); Validate(data); return data; }
    static void Validate(byte[] data)
    {
        if(data==null || data.Length!=ControlSize || Int(data,0)!=(ControlSize|(1<<16)) || Int(data,4)!=0 || Int(data,8)!=2 || Int(data,ZoneStart)!=3 || Int(data,ZoneStart+4)!=0 || Int(data,ZoneStart+ZoneStride)!=4)
            throw new Exception(UiLocale.D("Estado RGBW inesperado; nenhuma alteracao sera aplicada."));
    }
    public static Color ColorOf(byte[] data) { Validate(data); return Color.FromArgb(data[84],data[85],data[86]); }
    public static int WhiteOf(byte[] data) { Validate(data); return data[87]; }
    public static int BrightnessOf(byte[] data) { Validate(data); return data[88]; }
    public static byte[] WithColor(byte[] current,Color color,byte white,byte brightness)
    {
        Validate(current);
        if(brightness>100) throw new ArgumentException("Brilho deve estar entre 0 e 100.");
        byte[] next=(byte[])current.Clone();
        next[84]=color.R; next[85]=color.G; next[86]=color.B; next[87]=white; next[88]=brightness;
        return next;
    }
    void Write(byte[] data)
    {
        Validate(data);
        IntPtr memory=Marshal.AllocHGlobal(data.Length);
        try { Marshal.Copy(data,0,memory,data.Length); Check(Get<Info>(0x197d065e)(gpu,memory),UiLocale.D("Aplicar iluminacao")); }
        finally { Marshal.FreeHGlobal(memory); }
    }
    public byte[] Apply(Color color,byte white,byte brightness)
    {
        byte[] previous=ReadControl();
        Write(WithColor(previous,color,white,brightness));
        byte[] actual=ReadControl();
        if(ColorOf(actual).ToArgb()!=color.ToArgb() || WhiteOf(actual)!=white || BrightnessOf(actual)!=brightness)
            throw new Exception(UiLocale.D("O driver aceitou a chamada, mas a leitura retornou outros valores. Clique em Restaurar se necessario."));
        return previous;
    }
    public void RestoreColor(byte[] saved)
    {
        // Read-modify-write fresh state: never revert the other lighting zone.
        Apply(ColorOf(saved),(byte)WhiteOf(saved),(byte)BrightnessOf(saved));
    }
    public void Dispose()
    {
        if(initialized) { initialized=false; try { Get<Simple>(0xd22bdd7e)(); } catch {} }
        if(library!=IntPtr.Zero) { FreeLibrary(library); library=IntPtr.Zero; }
    }
    public static void SelfTest()
    {
        byte[] sample=new byte[ControlSize]; PutInt(sample,0,ControlSize|(1<<16)); PutInt(sample,8,2); PutInt(sample,76,3); PutInt(sample,276,4);
        sample[284]=63;
        byte[] updated=WithColor(sample,Color.FromArgb(18,79,255),0,75);
        for(int i=0;i<sample.Length;i++) if((i<84 || i>88) && sample[i]!=updated[i]) throw new Exception("Mudanca fora dos canais RGBW/brilho.");
        if(sample[84]!=0 || updated[84]!=18 || updated[85]!=79 || updated[86]!=255 || updated[87]!=0 || updated[88]!=75 || updated[284]!=63) throw new Exception("Teste dos canais falhou.");
        bool rejected=false; try { WithColor(new byte[ControlSize],Color.Red,0,100); } catch { rejected=true; }
        if(!rejected) throw new Exception("Layout invalido nao foi rejeitado.");
        Console.WriteLine("PASS: identidade/layout restritos; somente bytes RGBW/brilho sao alterados; estado original preservado.");
    }
}

sealed class ColorWindow : Form
{
    PnyLighting lighting;
    byte[] previous;
    TextBox hex;
    NumericUpDown red,green,blue,white,brightness;
    Panel preview;
    Label status;
    Button apply,restore;
    bool syncing;
    readonly string preference = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"cor-salva.txt");
    public ColorWindow()
    {
        Text="PNY Color — RTX 3080 Ti UPRISING"; ClientSize=new Size(580,425); FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false; StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(246,247,249);
        AddLabel("PNY Color",22,16,350,30).Font=new Font("Segoe UI",19,FontStyle.Bold);
        AddLabel("RGBW pela API NVIDIA · sem alterar desempenho da GPU",24,53,535,24);
        AddLabel("Cor HEX",24,98,90,25); hex=new TextBox {Left=24,Top=126,Width=145,Text="#124FFF",MaxLength=7}; Controls.Add(hex);
        preview=new Panel {Left=185,Top=124,Width=58,Height=33,BackColor=Color.FromArgb(18,79,255),BorderStyle=BorderStyle.FixedSingle}; Controls.Add(preview);
        red=Number("R",268,126,255,18); green=Number("G",365,126,255,79); blue=Number("B",462,126,255,255);
        white=Number("Canal branco",24,205,255,0); brightness=Number("Brilho %",185,205,100,100);
        AddLabel("Branco = 0 para usar apenas RGB.",300,208,265,30);
        string[] colors={"#FF0000","#00FF00","#0000FF","#FFFF00","#00FFFF","#FF00FF","#FFFFFF","#000000"};
        for(int i=0;i<colors.Length;i++) { string value=colors[i]; Button swatch=new Button {Left=24+i*65,Top=253,Width=55,Height=30,BackColor=Parse(value),FlatStyle=FlatStyle.Flat,AccessibleName=value}; swatch.Click+=delegate {SetColor(Parse(value));}; Controls.Add(swatch); new ToolTip().SetToolTip(swatch,value); }
        apply=ButtonAt("Aplicar",24,305,110,delegate {ApplyColor();});
        restore=ButtonAt("Restaurar anterior",144,305,165,delegate {Run(delegate {if(previous==null) return; lighting.RestoreColor(previous); LoadState(); status.Text="Cor anterior restaurada e conferida no driver.";});}); restore.Enabled=false;
        ButtonAt("Salvar escolha",319,305,130,delegate {Run(delegate {Color c=Parse(hex.Text); File.WriteAllText(preference,ToHex(c)+"|"+white.Value+"|"+brightness.Value); status.Text="Escolha salva. Nao e aplicada automaticamente.";});});
        ButtonAt("Ler GPU",459,305,96,delegate {Run(delegate {LoadState();});});
        status=AddLabel("Iniciando consulta...",24,352,530,62); status.ForeColor=Color.FromArgb(50,65,80);
        hex.TextChanged+=delegate { if(syncing) return; Color c; if(TryParse(hex.Text,out c)) SetColor(c); };
        red.ValueChanged+=ChannelsChanged; green.ValueChanged+=ChannelsChanged; blue.ValueChanged+=ChannelsChanged;
        Shown+=delegate {Run(delegate {lighting=new PnyLighting(); LoadState(); if(File.Exists(preference)) {string[] fields=File.ReadAllText(preference).Trim().Split('|'); int w,b; Color c; if(fields.Length==3 && TryParse(fields[0],out c) && int.TryParse(fields[1],out w) && int.TryParse(fields[2],out b) && w>=0 && w<=255 && b>=0 && b<=100) {SetColor(c); white.Value=w; brightness.Value=b; status.Text="Escolha salva carregada. Clique em Aplicar para mudar os LEDs.";}} });};
        FormClosed+=delegate {if(lighting!=null) lighting.Dispose();};
    }
    Label AddLabel(string text,int x,int y,int w,int h) {Label label=new Label {Text=text,Left=x,Top=y,Width=w,Height=h}; Controls.Add(label); return label;}
    NumericUpDown Number(string label,int x,int y,int max,int value) {AddLabel(label,x,y-27,120,25); NumericUpDown n=new NumericUpDown {Left=x,Top=y,Width=80,Minimum=0,Maximum=max,Value=value}; Controls.Add(n);return n;}
    Button ButtonAt(string text,int x,int y,int width,Action action) {Button b=new Button {Text=text,Left=x,Top=y,Width=width,Height=34};b.Click+=delegate {action();};Controls.Add(b);return b;}
    void ChannelsChanged(object sender,EventArgs args) {if(!syncing) SetColor(Color.FromArgb((int)red.Value,(int)green.Value,(int)blue.Value));}
    void SetColor(Color c) {syncing=true;try {hex.Text=ToHex(c);red.Value=c.R;green.Value=c.G;blue.Value=c.B;preview.BackColor=c;}finally{syncing=false;}}
    void LoadState() {if(lighting==null) throw new Exception("GPU indisponivel. Feche e abra o aplicativo para tentar novamente.");byte[] state=lighting.ReadControl();SetColor(PnyLighting.ColorOf(state));white.Value=PnyLighting.WhiteOf(state);brightness.Value=PnyLighting.BrightnessOf(state);status.Text="Leitura da GPU concluida. Nenhuma cor foi alterada.";}
    void ApplyColor() {Run(delegate {if(lighting==null) throw new Exception("GPU indisponivel.");Color c=Parse(hex.Text);previous=lighting.ReadControl();restore.Enabled=true;lighting.Apply(c,(byte)white.Value,(byte)brightness.Value);status.Text="Aplicado e conferido no driver. Confirme a cor nos LEDs da placa.";});}
    void Run(Action action) {try {action();}catch(Exception e){status.Text="Falha: "+e.Message;MessageBox.Show(this,e.Message,"PNY Color",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
    internal static string ToHex(Color c) {return "#"+c.R.ToString("X2")+c.G.ToString("X2")+c.B.ToString("X2");}
    internal static bool TryParse(string value,out Color c) {c=Color.Black;string s=value.Trim().TrimStart('#');int rgb;if(s.Length!=6 || !int.TryParse(s,NumberStyles.HexNumber,CultureInfo.InvariantCulture,out rgb))return false;c=Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255);return true;}
    internal static Color Parse(string value) {Color c;if(!TryParse(value,out c))throw new Exception(UiLocale.Get("hex.invalid"));return c;}
}

static class Program
{
    [STAThread] static int Main(string[] args)
    {
        try
        {
            UiLocale.Initialize();
            if(args.Length==1 && args[0]=="--install-bridge") {BridgePackage.Prepare();Console.WriteLine("PASS: embedded SignalRGB plugin installed; preferences preserved; no GPU access.");return 0;}
            if(args.Length==1 && args[0]=="--packaging-self-test") {BridgePackage.SelfTest();return 0;}
            if(args.Length==4 && args[0]=="--preview-ui")
            {
                if(!UiLocale.Valid(args[1]))throw new Exception("Invalid preview language.");
                UiLocale.Code=args[1];int page=int.Parse(args[2]);if(page<0||page>3)throw new Exception("Invalid preview page.");
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(EffectsWindow form=new EffectsWindow(false,true))
                {
                    form.PreviewPage(page);form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);
                    form.Show();form.PerformLayout();
                    using(Bitmap preview=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(preview,new Rectangle(0,0,preview.Width,preview.Height));preview.Save(args[3],System.Drawing.Imaging.ImageFormat.Png);}
                    form.Close();
                }
                return 0;
            }
            if(args.Length==2 && args[0]=="--preview-cover")
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(EffectsWindow form=new EffectsWindow(false,true))
                using(Bitmap preview=new Bitmap(form.Width,form.Height))
                {form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();form.PerformLayout();form.DrawToBitmap(preview,new Rectangle(0,0,preview.Width,preview.Height));preview.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);form.Close();}
                return 0;
            }
            if(args.Length==1 && args[0]=="--self-test") {PnyLighting.SelfTest(); Color c=ColorWindow.Parse("#124FFF");if(ColorWindow.ToHex(c)!="#124FFF")throw new Exception("HEX round-trip failed"); FeatureTests.Run(); Console.WriteLine("PASS: HEX/RGB round-trip.");return 0;}
            if(args.Length==1 && args[0]=="--read") {using(PnyLighting p=new PnyLighting()) {byte[] s=p.ReadControl();Console.WriteLine("RGB="+ColorWindow.ToHex(PnyLighting.ColorOf(s))+" W="+PnyLighting.WhiteOf(s)+" Brightness="+PnyLighting.BrightnessOf(s));}return 0;}
            if(args.Length==1 && args[0]=="--observe-bridge")
            {
                using(SignalBridge receiver=new SignalBridge())
                {
                    System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();long last=0;int printed=0;
                    while(clock.ElapsedMilliseconds<8000)
                    {
                        ColorFrame frame;long seq;double age;
                        if(receiver.TryLatest(out frame,out seq,out age) && seq!=last){last=seq;if(printed++<8)Console.WriteLine("SignalRGB frame "+seq+": "+ColorWindow.ToHex(frame.Color)+" brilho="+frame.Brightness);}
                        System.Threading.Thread.Sleep(100);
                    }
                    Console.WriteLine("Quadros recebidos="+last+". Apenas observacao local; nenhuma escrita na GPU.");
                    return last>0?0:2;
                }
            }
            if(args.Length==1 && args[0]=="--diagnose-brightness")
            {
                using(PnyLighting p=new PnyLighting())
                {
                    byte[] saved=p.ReadControl();
                    Color current=PnyLighting.ColorOf(saved);
                    if(current.R==0 && current.G==0 && current.B==0)throw new Exception("Teste requer uma cor aplicada, nao o padrao RGB zero.");
                    try
                    {
                        p.Apply(current,(byte)PnyLighting.WhiteOf(saved),0);
                        Console.WriteLine("BRILHO ZERO aplicado e conferido. Observar LEDs por 12 segundos.");
                        System.Threading.Thread.Sleep(12000);
                    }
                    finally {p.RestoreColor(saved);Console.WriteLine("Cor e brilho anteriores restaurados e conferidos.");}
                }
                return 0;
            }
            if(args.Length==1 && args[0]=="--verify-write-same") {using(PnyLighting p=new PnyLighting()) {byte[] s=p.ReadControl();p.RestoreColor(s);Console.WriteLine("PASS: mesma cor reaplicada e conferida no driver. Sem teste visual de mudanca.");}return 0;}
            if(args.Length==1 && args[0]=="--apply-saved") {ColorPreferences prefs=ColorPreferences.Load();using(PnyLighting p=new PnyLighting()) {ColorFrame frame=EffectMath.Frame(prefs,0);p.Apply(frame.Color,frame.White,frame.Brightness);Console.WriteLine("PASS: primeiro quadro da escolha salva aplicado e conferido.");}return 0;}
            bool startup=args.Length==1 && args[0]=="--startup";
            if(args.Length!=0 && !startup) throw new Exception("Argumento desconhecido.");
            bool owns;
            using(System.Threading.Mutex mutex=new System.Threading.Mutex(true,"Local\\PNYColor.Uprising.1384196E.v2",out owns))
            {
                if(!owns) {try {using(System.Threading.EventWaitHandle e=System.Threading.EventWaitHandle.OpenExisting("Local\\PNYColor.Show.v2"))e.Set();}catch{}return 0;}
                try {Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new EffectsWindow(startup));}
                finally {mutex.ReleaseMutex();}
            }
            return 0;
        }
        catch(Exception e) {Console.Error.WriteLine(e.Message);if(args.Length==0)MessageBox.Show(e.Message,"PNY Color");return 1;}
    }
}
