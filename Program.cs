using System.Linq.Expressions;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using WindowsInput.Native;
using WindowsInput;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

public static class Program
{
    static InputSimulator sim = new InputSimulator();
    public static void Main(string[] args)
    {
        //Console.ReadLine();
        TimeSpan GOAL_TICK_TIME = new TimeSpan(0, 0, 0, 0, 50); 
        const bool ServerRegulatedTick = false;

        if (!HttpListener.IsSupported)
        {
            Console.WriteLine("HttpListner not supported");
            return;
        }
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://*:8000/");
        Console.WriteLine("Starting on port 8000");
        listener.Start();
        Stopwatch s = new Stopwatch();
        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            Task task1 = Task.Factory.StartNew(() => Handle(context));
        }
    }
    public static void Handle(HttpListenerContext context)
    {
        HttpListenerResponse response = context.Response;
        byte[] buffer = Process(context, ref response);
        response.ContentLength64 = buffer.Length;
        Stream output = response.OutputStream;
        try
        {
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
        catch 
        {
            return;

        }
    }
    public static string ReadBody(HttpListenerRequest request)
    {
        using (System.IO.Stream body = request.InputStream)
        {
            using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }
    }
    //Change default image size here, if the image is to blury and your willing to take the hit to performance
    static Size imageSize = new Size(1920, 1080);
    //Try to keep this ratio at 4:3 , so the screen doesn't look funky
    public static byte[] Process(HttpListenerContext l, ref HttpListenerResponse r)
    {
        if (l.Request.HttpMethod == "POST")
        {
            try
            {
                int longside = Int32.Parse(l.Request.Headers["res"]);
                imageSize = new Size(longside, ((int)(longside * 0.75f)));
            }catch { }
            {
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }
                    var body = ReadBody(l.Request);
                    
                    r.AddHeader("height", bounds.Height.ToString());
                    r.AddHeader("width", bounds.Width.ToString());
                    Bitmap resized = (Bitmap)resizeImage(bitmap, imageSize);
                    MemoryStream s = new MemoryStream();
                    resized.SaveJPG100(s);
                    HandleInput(body);
                    //sim.Keyboard.KeyPress((VirtualKeyCode)key);
                    return s.ToArray();
                    //return (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
                }
            }

        }
        else if (l.Request.HttpMethod == "GET")
        {
            var path = GetThisFilePath();
            var directory = Path.GetDirectoryName(path);
            return System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(directory +  "\\Response.html"));
        }
        return new byte[0];

    }

    private static void HandleInput(string body)
    {
        var list = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(body);
        Console.WriteLine(list);
        ProcessEvents(list);
    }

    private static void ProcessEvents(List<Dictionary<string, string>> events)
    {
        foreach(Dictionary<string, string> e in events){
            string type = e["type"];
            switch (type)
            {
                case "keydown":
                    int keydown = Int32.Parse(e["keyCode"]);
                    sim.Keyboard.KeyDown((VirtualKeyCode)keydown);
                    break;
                case "keyup":
                    int keyup = Int32.Parse(e["keyCode"]);
                    sim.Keyboard.KeyUp((VirtualKeyCode)keyup);
                    break;
                case "mouseup":
                    switch (e["button"])
                    {
                        case "0":
                            sim.Mouse.LeftButtonUp();
                            break;
                        case "2":
                            sim.Mouse.RightButtonUp();
                            break;
                    }
                    break;
                case "mousedown":
                    switch (e["button"])
                    {
                        case "0":
                            sim.Mouse.LeftButtonDown();
                            break;
                        case "2":
                            sim.Mouse.RightButtonDown();
                            break;
                    }
                    break;
                case "mousemove":
                    int x = Helper.FracToScreen(Double.Parse(e["xPer"]));
                    int y = Helper.FracToScreen(Double.Parse(e["yPer"]));
                    sim.Mouse.MoveMouseTo(x, y);
                    break;


            }
        }
    }

    private static string GetThisFilePath([CallerFilePath] string path = null)
    {
        return path;
    }
    public static Image resizeImage(Image imgToResize, Size size)
    {
        return (Image)(new Bitmap(imgToResize, size));
    }
}