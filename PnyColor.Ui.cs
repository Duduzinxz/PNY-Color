using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

sealed class Surface : Panel
{
    public Surface(){DoubleBuffered=true;BackColor=Color.FromArgb(23,26,36);Padding=new Padding(20);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using(Pen pen=new Pen(Color.FromArgb(43,48,64)))using(GraphicsPath path=Round(new Rectangle(0,0,Width-1,Height-1),14))e.Graphics.DrawPath(pen,path);
    }
    internal static GraphicsPath Round(Rectangle r,int radius)
    {
        GraphicsPath p=new GraphicsPath();int d=radius*2;
        p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
}
sealed class ColorPreview : Panel
{
    public ColorPreview(){DoubleBuffered=true;BackColor=Color.FromArgb(255,128,0);}
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        Color c=BackColor;
        using(LinearGradientBrush b=new LinearGradientBrush(ClientRectangle,Color.FromArgb(Math.Max(8,c.R/9),Math.Max(10,c.G/9),Math.Max(16,c.B/9)),Color.FromArgb(Math.Max(12,c.R/3),Math.Max(14,c.G/3),Math.Max(22,c.B/3)),35f))g.FillRectangle(b,ClientRectangle);
        int diameter=Math.Min(Height-26,100);Rectangle circle=new Rectangle((Width-diameter)/2,(Height-diameter)/2,diameter,diameter);
        using(Brush fill=new SolidBrush(c))g.FillEllipse(fill,circle);
        using(Pen edge=new Pen(Color.FromArgb(90,255,255,255),2))g.DrawEllipse(edge,circle);
    }
    protected override void OnBackColorChanged(EventArgs e){base.OnBackColorChanged(e);Invalidate();}
}

sealed class UiButton : Button
{
    bool hovering;
    public UiButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hovering=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hovering=false;Invalidate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.Clear(Parent==null?BackColor:Parent.BackColor);
        Color fill=Enabled?(hovering?FlatAppearance.MouseOverBackColor:BackColor):Color.FromArgb(26,30,40);
        using(GraphicsPath path=Surface.Round(new Rectangle(0,0,Width-1,Height-1),8))
        {
            using(Brush brush=new SolidBrush(fill))e.Graphics.FillPath(brush,path);
            if(FlatAppearance.BorderSize>0||Focused)using(Pen pen=new Pen(Focused?Color.FromArgb(160,135,255):FlatAppearance.BorderColor))e.Graphics.DrawPath(pen,path);
        }
        Rectangle textBounds=new Rectangle(Padding.Left+6,3,Width-Padding.Horizontal-12,Height-6);
        TextFormatFlags flags=TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine;
        flags|=TextAlign==ContentAlignment.MiddleLeft?TextFormatFlags.Left:TextFormatFlags.HorizontalCenter;
        TextRenderer.DrawText(e.Graphics,Text,Font,textBounds,Enabled?ForeColor:Color.FromArgb(125,136,158),flags);
    }
}

sealed partial class EffectsWindow
{
    readonly Dictionary<Control,string> texts=new Dictionary<Control,string>();
    readonly List<Button> navigation=new List<Button>();
    readonly List<Panel> pages=new List<Panel>();
    Label pageTitle,pageSubtitle,colorCode,colorChannels;
    ComboBox languages;
    TrackBar brightnessSlider;
    ToolTip hints;
    string statusKey="ready";
    int pageIndex;
    bool localizing;
    ToolStripItem trayOpen,trayPause,trayExit;
    static readonly Color Muted=Color.FromArgb(148,159,185),Ink=Color.FromArgb(239,242,251),Accent=Color.FromArgb(128,100,242);

    void BuildUi()
    {
        Text="PNY Color";ClientSize=new Size(1080,720);MinimumSize=new Size(1080,720);
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.Sizable;MaximizeBox=true;StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(13,16,23);ForeColor=Ink;
        DoubleBuffered=true;hints=new ToolTip {AutoPopDelay=12000,InitialDelay=500,ReshowDelay=200};
        Panel sidebar=new Panel {Dock=DockStyle.Left,Width=216,BackColor=Color.FromArgb(18,21,30)};Controls.Add(sidebar);
        PictureBox brand=new PictureBox {Left=28,Top=26,Width=94,Height=94,SizeMode=PictureBoxSizeMode.Zoom,AccessibleName="PNY Color"};
        using(Stream s=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("PNYColor.Brand.png"))
        {if(s==null)throw new InvalidOperationException("Brand image is missing.");using(Image image=Image.FromStream(s))brand.Image=new Bitmap(image);}
        brand.Disposed+=delegate{if(brand.Image!=null)brand.Image.Dispose();};sidebar.Controls.Add(brand);
        Plain(sidebar,"PNY Color",28,127,188,42,20,true,Ink);
        LabelFor(sidebar,"tagline",28,175,180,28,10,false,Muted);
        string[] keys={"nav.color","nav.effects","nav.bridge","nav.settings"};string[] symbols={"◉","◈","↔","⚙"};
        for(int i=0;i<keys.Length;i++)
        {
            int index=i;Button button=Action(sidebar,keys[i],20,246+i*54,176,44,delegate{Navigate(index);});
            button.TextAlign=ContentAlignment.MiddleLeft;button.Padding=new Padding(16,0,0,0);button.FlatAppearance.BorderSize=0;
            navigation.Add(button);hints.SetToolTip(button,symbols[i]+"  "+UiLocale.Get(keys[i]));
        }
        Surface device=Card(sidebar,18,503,180,142);device.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
        LabelFor(device,"device",14,14,154,20,8,true,Muted);
        Plain(device,"RTX 3080 Ti",14,42,154,28,14,true,Ink);
        LabelFor(device,"device.detail",14,78,152,44,8,false,Muted);
        Label independent=LabelFor(sidebar,"independent",22,675,190,24,8,true,Muted);independent.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;

        pageTitle=Plain(this,"",244,25,808,44,26,true,Ink);pageTitle.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        pageSubtitle=Plain(this,"",246,80,802,32,10,false,Muted);pageSubtitle.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        for(int i=0;i<4;i++){Panel p=new Panel {Left=244,Top=128,Width=808,Height=418,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=BackColor,Visible=false};Controls.Add(p);pages.Add(p);}
        BuildColorPage(pages[0]);BuildEffectsPage(pages[1]);BuildBridgePage(pages[2]);BuildSettingsPage(pages[3]);
        Label warning=LabelFor(this,"limitation",248,556,800,43,9,false,Color.FromArgb(209,178,117));warning.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
        Button apply=Action(this,"apply",244,611,236,48,ApplySelected);apply.BackColor=Accent;apply.Font=new Font("Segoe UI",11,FontStyle.Bold);apply.FlatAppearance.BorderSize=0;apply.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
        Action(this,"pause",492,611,130,48,PauseControl).Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
        restore=Action(this,"restore",634,611,130,48,delegate{Run(delegate{if(previous==null)return;StopActive();lighting.RestoreColor(previous);ShowState();SetStatus("restored");});});restore.Enabled=false;restore.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
        Action(this,"read",776,611,130,48,delegate{Run(ShowState);}).Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
        status=Plain(this,UiLocale.Get("ready"),246,680,802,26,9,false,Color.FromArgb(160,196,248));status.Anchor=AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;status.AutoEllipsis=true;
        AcceptButton=apply;
        hex.TextChanged+=delegate{if(syncing)return;Color c;if(ColorWindow.TryParse(hex.Text,out c))SetColor(c);};
        red.ValueChanged+=ChannelChanged;green.ValueChanged+=ChannelChanged;blue.ValueChanged+=ChannelChanged;
        brightness.ValueChanged+=delegate{if(brightnessSlider.Value!=(int)brightness.Value)brightnessSlider.Value=(int)brightness.Value;};
        brightnessSlider.ValueChanged+=delegate{if(brightness.Value!=brightnessSlider.Value)brightness.Value=brightnessSlider.Value;};
        effects.SelectedIndexChanged+=delegate{secondHex.Enabled=effects.SelectedIndex==3;speed.Enabled=effects.SelectedIndex>0&&effects.SelectedIndex<7;};
        secondHex.Enabled=false;speed.Enabled=false;
        syncing=true;try{startup.Checked=StartupSetting.Enabled;}catch{}finally{syncing=false;}
        ApplyLocale();Navigate(0);SetColor(Color.FromArgb(132,86,255));
        FormClosed+=delegate{hints.Dispose();};
    }
    void BuildColorPage(Panel page)
    {
        Surface visual=Card(page,0,0,378,244);LabelFor(visual,"preview",22,18,328,23,8,true,Muted);
        preview=new ColorPreview {Left=22,Top=53,Width=334,Height=126,AccessibleName=UiLocale.Get("preview")};visual.Controls.Add(preview);
        colorCode=Plain(visual,"#8456FF",22,185,220,34,19,true,Ink);
        colorChannels=Plain(visual,"132, 86, 255",236,194,118,22,9,false,Muted);colorChannels.TextAlign=ContentAlignment.MiddleRight;
        Surface input=Card(page,396,0,412,244);input.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        LabelFor(input,"hex",20,18,184,25,10,true,Ink);hex=Input(input,"#8456FF",20,53,164);
        Action(input,"pick",202,49,188,38,PickScreenColor);
        LabelFor(input,"channels",20,103,348,24,9,false,Muted);
        red=Numeric(input,"R",20,154,0,255,132);green=Numeric(input,"G",150,154,0,255,86);blue=Numeric(input,"B",280,154,0,255,255);
        LabelFor(input,"preview.hint",20,196,368,36,8,false,Muted);
        Surface palette=Card(page,0,262,378,150);LabelFor(palette,"palette",22,17,328,23,8,true,Muted);
        string[] colors={"#FF455C","#FF963B","#FFE16A","#48DF99","#37CBEF","#4779FF","#A76BFF","#FFFFFF"};
        for(int i=0;i<colors.Length;i++)
        {
            string v=colors[i];Button b=new Button {Left=22+i*40,Top=55,Width=32,Height=36,BackColor=ColorWindow.Parse(v),FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,AccessibleName=v};
            b.FlatAppearance.BorderSize=0;b.Click+=delegate{SetColor(ColorWindow.Parse(v));};palette.Controls.Add(b);hints.SetToolTip(b,v);
        }
        LabelFor(palette,"white.hint",22,109,328,28,8,false,Muted);
        Surface intensity=Card(page,396,262,412,150);intensity.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        LabelFor(intensity,"brightness",20,14,164,22,9,false,Muted);
        brightness=new NumericUpDown {Left=288,Top=42,Width=102,Minimum=1,Maximum=100,Value=100};StyleInput(brightness);intensity.Controls.Add(brightness);
        brightnessSlider=new TrackBar {Left=16,Top=43,Width=263,Height=38,Minimum=1,Maximum=100,Value=100,TickStyle=TickStyle.None,BackColor=intensity.BackColor};intensity.Controls.Add(brightnessSlider);
        LabelFor(intensity,"white",20,99,190,26,9,false,Muted);white=new NumericUpDown {Left=288,Top=96,Width=102,Minimum=0,Maximum=255,Value=0};StyleInput(white);intensity.Controls.Add(white);
    }
    void BuildEffectsPage(Panel page)
    {
        Surface selection=Card(page,0,0,808,186);selection.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        LabelFor(selection,"effect",24,23,520,28,12,true,Ink);
        effects=new ComboBox {Left=24,Top=65,Width=504,DropDownStyle=ComboBoxStyle.DropDownList,DropDownWidth=520};StyleInput(effects);selection.Controls.Add(effects);
        for(int i=0;i<8;i++)effects.Items.Add(UiLocale.Get("effect."+i));effects.SelectedIndex=0;
        LabelFor(selection,"speed",574,26,190,25,10,false,Muted);speed=new NumericUpDown {Left=574,Top=65,Width=198,Minimum=1,Maximum=10,Value=4};StyleInput(speed);selection.Controls.Add(speed);
        LabelFor(selection,"effects.hint",24,120,748,45,9,false,Muted);
        Surface transition=Card(page,0,204,808,208);transition.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        LabelFor(transition,"second",24,24,520,28,12,true,Ink);secondHex=Input(transition,"#0080FF",24,65,238);
        LabelFor(transition,"second.hint",24,118,748,45,10,false,Muted);
    }
    void BuildBridgePage(Panel page)
    {
        Surface bridgeCard=Card(page,0,0,808,412);bridgeCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        Plain(bridgeCard,"SignalRGB",24,22,650,44,23,true,Ink);
        LabelFor(bridgeCard,"bridge.steps",24,89,744,174,10,false,Muted);
        Action(bridgeCard,"bridge.select",24,279,286,42,delegate{Run(delegate{bool changed=BridgePackage.Prepare();effects.SelectedIndex=7;ApplySelected();if(changed && active!=null && active.Effect==LightEffect.SignalRGB)SetStatus("bridge.restart");});});
        Action(bridgeCard,"bridge.folder",324,279,292,42,delegate{Run(delegate{BridgePackage.InstallTo(BridgePackage.PluginDirectory);System.Diagnostics.Process.Start("explorer.exe",Quote(BridgePackage.PluginDirectory));});});
        LabelFor(bridgeCard,"bridge.detail",24,343,742,54,9,false,Muted);
    }
    void BuildSettingsPage(Panel page)
    {
        Surface language=Card(page,0,0,808,140);language.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        LabelFor(language,"language",24,21,352,28,12,true,Ink);
        languages=new ComboBox {Left=470,Top=24,Width=306,DropDownStyle=ComboBoxStyle.DropDownList};StyleInput(languages);languages.Items.AddRange(UiLocale.Names);languages.SelectedIndex=Array.IndexOf(UiLocale.Codes,UiLocale.Code);language.Controls.Add(languages);
        LabelFor(language,"language.hint",24,76,748,43,9,false,Muted);
        languages.SelectedIndexChanged+=delegate{if(localizing)return;Run(delegate{UiLocale.Save(UiLocale.Codes[languages.SelectedIndex]);ApplyLocale();SetStatus("language.done");});localizing=true;languages.SelectedIndex=Array.IndexOf(UiLocale.Codes,UiLocale.Code);localizing=false;};
        Surface startupCard=Card(page,0,156,808,108);startupCard.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        startup=new CheckBox {Left=24,Top=20,Width=746,Height=31,ForeColor=Ink,BackColor=startupCard.BackColor};Bind(startup,"startup");startupCard.Controls.Add(startup);
        LabelFor(startupCard,"startup.hint",24,64,748,28,9,false,Muted);
        Surface data=Card(page,0,280,808,132);data.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;
        LabelFor(data,"data",24,17,340,26,11,true,Ink);LabelFor(data,"data.hint",24,54,738,30,9,false,Muted);
        Action(data,"data.open",24,89,330,30,delegate{Run(delegate{Directory.CreateDirectory(AppPaths.Root);System.Diagnostics.Process.Start("explorer.exe",Quote(AppPaths.Root));});});
        LabelFor(data,"about",385,89,383,40,8,false,Muted);
    }
    void ApplyLocale()
    {
        localizing=true;
        try
        {
            foreach(KeyValuePair<Control,string> pair in texts)pair.Key.Text=UiLocale.Get(pair.Value);
            int effect=effects.SelectedIndex;effects.BeginUpdate();try{effects.Items.Clear();for(int i=0;i<8;i++)effects.Items.Add(UiLocale.Get("effect."+i));effects.SelectedIndex=Math.Max(0,effect);}finally{effects.EndUpdate();}
            languages.SelectedIndex=Array.IndexOf(UiLocale.Codes,UiLocale.Code);
            preview.AccessibleName=UiLocale.Get("preview");
            if(trayOpen!=null){trayOpen.Text=UiLocale.Get("tray.open");trayPause.Text=UiLocale.Get("tray.pause");trayExit.Text=UiLocale.Get("tray.exit");}
            UpdatePageHeading();status.Text=UiLocale.Get(statusKey);
        }finally{localizing=false;}
    }
    void Navigate(int index)
    {
        pageIndex=index;for(int i=0;i<pages.Count;i++){pages[i].Visible=i==index;navigation[i].BackColor=i==index?Color.FromArgb(52,42,82):Color.FromArgb(18,21,30);navigation[i].ForeColor=i==index?Color.FromArgb(205,185,255):Muted;}
        UpdatePageHeading();
    }
    void UpdatePageHeading(){string[] sections={"color","effects","bridge","settings"};pageTitle.Text=UiLocale.Get(sections[pageIndex]+".title");pageSubtitle.Text=UiLocale.Get(sections[pageIndex]+".subtitle");}
    void PauseControl(){StopActive();SetStatus("paused");}
    void SetStatus(string key){statusKey=key;status.Text=UiLocale.Get(key);hints.SetToolTip(status,status.Text);}
    void UpdateColorLabels(Color color){if(colorCode!=null)colorCode.Text=ColorWindow.ToHex(color);if(colorChannels!=null)colorChannels.Text=color.R+", "+color.G+", "+color.B;}
    static string Quote(string value){return "\""+value+"\"";}
    void Bind(Control control,string key){texts.Add(control,key);control.Text=UiLocale.Get(key);control.AccessibleName=control.Text;}
    Surface Card(Control parent,int x,int y,int width,int height){Surface p=new Surface {Left=x,Top=y,Width=width,Height=height};parent.Controls.Add(p);return p;}
    Label Plain(Control parent,string text,int x,int y,int width,int height,float size,bool bold,Color color)
    {
        Label l=new Label {Text=text,Left=x,Top=y,Width=width,Height=height,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent};parent.Controls.Add(l);return l;
    }
    Label LabelFor(Control parent,string key,int x,int y,int width,int height,float size,bool bold,Color color){Label l=Plain(parent,"",x,y,width,height,size,bold,color);Bind(l,key);return l;}
    Button Action(Control parent,string key,int x,int y,int width,int height,Action action)
    {
        Button b=new UiButton {Left=x,Top=y,Width=width,Height=height,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(32,37,51),ForeColor=Ink,Cursor=Cursors.Hand,UseVisualStyleBackColor=false};b.FlatAppearance.BorderColor=Color.FromArgb(61,69,91);b.FlatAppearance.MouseOverBackColor=Color.FromArgb(67,53,107);Bind(b,key);b.Click+=delegate{action();};parent.Controls.Add(b);return b;
    }
    TextBox Input(Control parent,string text,int x,int y,int width){TextBox t=new TextBox {Text=text,Left=x,Top=y,Width=width,MaxLength=7,Font=new Font("Consolas",15),CharacterCasing=CharacterCasing.Upper};StyleInput(t);parent.Controls.Add(t);return t;}
    NumericUpDown Numeric(Control parent,string channel,int x,int y,int min,int max,int value){Plain(parent,channel,x,y-25,80,22,9,true,Muted);NumericUpDown n=new NumericUpDown {Left=x,Top=y,Width=108,Minimum=min,Maximum=max,Value=value};StyleInput(n);parent.Controls.Add(n);return n;}
    void StyleInput(Control c){c.BackColor=Color.FromArgb(15,19,28);c.ForeColor=Ink;TextBox t=c as TextBox;if(t!=null)t.BorderStyle=BorderStyle.FixedSingle;ComboBox combo=c as ComboBox;if(combo!=null)combo.FlatStyle=FlatStyle.Flat;}
    internal void PreviewPage(int index){Navigate(index);}
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Rectangle area=Screen.FromControl(this).WorkingArea;
        float fit=Math.Min(1f,Math.Min((area.Width-24)/(float)Width,(area.Height-24)/(float)Height));
        if(fit<1f)
        {
            MinimumSize=Size.Empty;Scale(new SizeF(fit,fit));MinimumSize=Size;
        }
    }
    internal static void UiSelfTest()
    {
        string original=UiLocale.Code;
        try
        {
            foreach(string code in UiLocale.Codes)
            {
                UiLocale.Code=code;
                using(EffectsWindow form=new EffectsWindow(false,true))
                {
                    form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-30000,-30000);form.Show();
                    ColorPreferences selected=ColorPreferences.Parse("v2|#124FFF|0|75|3|4|#0080FF");
                    form.ShowPreferences(selected);form.active=selected;
                    UiLocale.Code=code=="en"?"pt-BR":"en";form.ApplyLocale();
                    if(form.active!=selected||form.ReadSelection().Serialize()!=selected.Serialize()||!form.secondHex.Enabled)throw new Exception("Language change altered an active effect or selection.");
                    form.brightnessSlider.Value=30;if(form.brightness.Value!=30)throw new Exception("Brightness slider did not update value.");
                    form.brightness.Value=65;if(form.brightnessSlider.Value!=65)throw new Exception("Brightness value did not update slider.");
                    form.effects.SelectedIndex=7;if(form.speed.Enabled||form.secondHex.Enabled)throw new Exception("SignalRGB exposed unrelated effect controls.");
                    form.Close();
                }
            }
            Console.WriteLine("PASS: language switches preserve active effects and selection; brightness bindings and effect controls verified without GPU access or registry writes.");
        }finally{UiLocale.Code=original;}
    }
}
