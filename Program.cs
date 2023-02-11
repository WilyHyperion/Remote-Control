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
            if (ServerRegulatedTick)
            {
                s.Start();
            }
            HttpListenerContext context = listener.GetContext();
            Task task1 = Task.Factory.StartNew(() => Handle(context));

            if (ServerRegulatedTick)
            {
                s.Stop();
                TimeSpan spent = s.Elapsed;
                if (!(spent - GOAL_TICK_TIME >= new TimeSpan(0)))
                {
                    Thread.Sleep(spent - GOAL_TICK_TIME);
                }
            }
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
        catch (Exception ex)
        {
            return;

        }
    }
    public static string ReadBody(HttpListenerRequest request)
    {
        using (System.IO.Stream body = request.InputStream)
        {
            Console.WriteLine(body.Length);
            using (var reader = new System.IO.StreamReader(body, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }
    }

    static Size imageSize = new Size(200, 150);
    static int downKey;
    public static byte[] Process(HttpListenerContext l, ref HttpListenerResponse r)
    {
        if (l.Request.HttpMethod == "POST")
        {
            int key;
            try
            {
                key = Int32.Parse(l.Request.Headers["key"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Key error {0}", ex);
                key = -1;
            }
            PointD MouseChange = new PointD(-65535, -65535);
            try
            {
                MouseChange = new PointD(double.Parse(l.Request.Headers["mousex"]), double.Parse(l.Request.Headers["mousey"]));
                Console.WriteLine("Mouse change {0} {1}", 65535 * MouseChange.X, 65535 * MouseChange.Y);
            }
            catch (Exception ex)
            { }
            bool md = false;

            try
            {
                md = l.Request.Headers["md"] == "true";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            bool rd = false;
            try
            {
                rd = l.Request.Headers["rd"] == "true";
            }
            catch (Exception ex)
            {

            }

            try
            {
                int longside = Int32.Parse(l.Request.Headers["res"]);
                imageSize = new Size(longside, ((int)(longside * 0.75f)));
            }catch (Exception ex) { }
            {
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }
                    var body = new StreamReader(l.Request.InputStream).ReadToEnd();
                    r.AddHeader("height", bounds.Height.ToString());
                    r.AddHeader("width", bounds.Width.ToString());
                    Bitmap resized = (Bitmap)resizeImage(bitmap, imageSize);
                    MemoryStream s = new MemoryStream();
                    resized.SaveJPG100(s);
                    if (key != -1)
                    {
                        if (key != downKey)
                        {
                            sim.Keyboard.KeyDown((VirtualKeyCode)key);
                            downKey = key;
                        }
                        else
                        {
                            sim.Keyboard.KeyUp((VirtualKeyCode)key);
                            downKey = -1;
                        }

                    }
                    if (MouseChange.X != -65535 && MouseChange.Y != -65535)
                    {
                        sim.Mouse.MoveMouseTo(65535 * MouseChange.X, 65535 * MouseChange.Y);
                    }
                    //mouse on client is just got held down
                    if (md)
                    {
                        sim.Mouse.LeftButtonClick();
                    }
                    //mouse on client just got moved up

                    if (rd)
                    {
                        sim.Mouse.RightButtonClick();
                    }
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
    private static string GetThisFilePath([CallerFilePath] string path = null)
    {
        return path;
    }
    public static Image resizeImage(Image imgToResize, Size size)
    {
        return (Image)(new Bitmap(imgToResize, size));
    }
}