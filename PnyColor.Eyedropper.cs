using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Captures only on an explicit user click. Snapshot stays in memory and is disposed.
sealed class ScreenColorPicker : Form
{
    [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    readonly Bitmap snapshot;
    Point pixel;
    public Color SelectedColor { get; private set; }

    ScreenColorPicker(Bitmap image, Rectangle desktop)
    {
        snapshot=image;
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;
        StartPosition=FormStartPosition.Manual;Bounds=desktop;
        ShowInTaskbar=false;TopMost=true;KeyPreview=true;DoubleBuffered=true;
        Cursor=Cursors.Cross;Text="PNY Color — "+UiLocale.Get("pick");
        AccessibleName=UiLocale.Get("pick.access");
        pixel=new Point(Cursor.Position.X-desktop.Left,Cursor.Position.Y-desktop.Top);
        DialogResult=DialogResult.Cancel;
    }

    public static bool Pick(IWin32Window owner, out Color color)
    {
        color=Color.Empty;IntPtr prior=IntPtr.Zero;
        try
        {
            // Physical pixel coordinates across displays; does not change process-wide DPI.
            try{prior=SetThreadDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){}
            Rectangle desktop=SystemInformation.VirtualScreen;
            using(Bitmap shot=new Bitmap(desktop.Width,desktop.Height))
            {
                using(Graphics capture=Graphics.FromImage(shot))
                    capture.CopyFromScreen(desktop.Location,Point.Empty,desktop.Size,CopyPixelOperation.SourceCopy);
                using(ScreenColorPicker picker=new ScreenColorPicker(shot,desktop))
                {
                    if(picker.ShowDialog(owner)!=DialogResult.OK)return false;
                    color=picker.SelectedColor;return true;
                }
            }
        }
        finally{if(prior!=IntPtr.Zero)SetThreadDpiAwarenessContext(prior);}
    }

    internal static Color Sample(Bitmap image,Point location)
    {
        if(location.X<0||location.Y<0||location.X>=image.Width||location.Y>=image.Height)
            throw new ArgumentOutOfRangeException("location");
        return image.GetPixel(location.X,location.Y);
    }

    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);pixel=e.Location;Invalidate();}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if(e.Button==MouseButtons.Right){DialogResult=DialogResult.Cancel;Close();return;}
        if(e.Button!=MouseButtons.Left)return;
        if(e.X<0||e.Y<0||e.X>=snapshot.Width||e.Y>=snapshot.Height)return;
        SelectedColor=Sample(snapshot,e.Location);DialogResult=DialogResult.OK;Close();
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {
        if(keyData==Keys.Escape){DialogResult=DialogResult.Cancel;Close();return true;}
        return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.DrawImageUnscaled(snapshot,0,0);
        if(pixel.X<0||pixel.Y<0||pixel.X>=snapshot.Width||pixel.Y>=snapshot.Height)return;
        Color selected=Sample(snapshot,pixel);
        Rectangle monitor=Screen.FromPoint(new Point(Left+pixel.X,Top+pixel.Y)).Bounds;
        monitor.Offset(-Left,-Top);
        int x=Math.Max(monitor.Left,Math.Min(pixel.X+24,monitor.Right-370));
        int y=Math.Max(monitor.Top,Math.Min(pixel.Y+24,monitor.Bottom-102));
        using(Brush dark=new SolidBrush(Color.FromArgb(22,25,34)))e.Graphics.FillRectangle(dark,x,y,370,102);
        using(Pen border=new Pen(Color.FromArgb(110,95,215)))e.Graphics.DrawRectangle(border,x,y,369,101);
        using(Brush swatch=new SolidBrush(selected))e.Graphics.FillRectangle(swatch,x+12,y+12,36,36);
        using(Font text=new Font("Segoe UI",10))
        using(Font title=new Font("Segoe UI",12,FontStyle.Bold))
        {
            e.Graphics.DrawString(ColorWindow.ToHex(selected),title,Brushes.White,x+60,y+10);
            e.Graphics.DrawString("RGB "+selected.R+", "+selected.G+", "+selected.B,text,Brushes.LightGray,x+60,y+35);
            e.Graphics.DrawString(UiLocale.Get("pick.instructions"),text,Brushes.White,x+12,y+72);
        }
        Rectangle source=Rectangle.Intersect(new Rectangle(pixel.X-4,pixel.Y-4,9,9),new Rectangle(Point.Empty,snapshot.Size));
        e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;
        e.Graphics.DrawImage(snapshot,new Rectangle(x+286,y+10,72,54),source,GraphicsUnit.Pixel);
    }

    public static void SelfTest()
    {
        using(Bitmap sample=new Bitmap(3,2))
        {
            sample.SetPixel(0,0,Color.FromArgb(18,79,255));sample.SetPixel(2,1,Color.FromArgb(255,128,0));
            if(ColorWindow.ToHex(Sample(sample,new Point(0,0)))!="#124FFF"||ColorWindow.ToHex(Sample(sample,new Point(2,1)))!="#FF8000")throw new Exception("Conta-gotas: cor incorreta");
            Point[] invalid={new Point(-1,0),new Point(0,-1),new Point(3,0),new Point(0,2)};
            foreach(Point point in invalid){bool rejected=false;try{Sample(sample,point);}catch(ArgumentOutOfRangeException){rejected=true;}if(!rejected)throw new Exception("Conta-gotas: limite invalido aceito");}
            using(ScreenColorPicker picker=new ScreenColorPicker(sample,new Rectangle(-100,0,3,2)))
            {
                picker.OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,2,1,0));
                if(picker.DialogResult!=DialogResult.OK||ColorWindow.ToHex(picker.SelectedColor)!="#FF8000")throw new Exception("Conta-gotas: selecao incorreta");
            }
            using(ScreenColorPicker picker=new ScreenColorPicker(sample,new Rectangle(0,0,3,2)))
            {
                picker.OnMouseDown(new MouseEventArgs(MouseButtons.Right,1,0,0,0));
                if(picker.DialogResult!=DialogResult.Cancel||!picker.SelectedColor.IsEmpty)throw new Exception("Conta-gotas: cancelar alterou a cor");
            }
            using(ScreenColorPicker picker=new ScreenColorPicker(sample,new Rectangle(0,0,3,2)))
            {
                Message message=new Message();
                if(!picker.ProcessCmdKey(ref message,Keys.Escape)||picker.DialogResult!=DialogResult.Cancel||!picker.SelectedColor.IsEmpty)throw new Exception("Conta-gotas: Escape falhou");
            }
        }
        Console.WriteLine("PASS: conta-gotas, pixels/HEX e limites; imagem sintetica, sem capturar a tela.");
    }
}
