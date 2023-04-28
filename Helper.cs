using System;
using System.Drawing.Imaging;

public static class  Helper
{
    public static void SaveJPG100(this Bitmap bmp, Stream stream)
    {
        EncoderParameters encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
        bmp.Save(stream, GetEncoder(ImageFormat.Jpeg), encoderParameters);
    }
    public static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }

        return null;
    }

    const int SCREEN_SIZE= 65535;
    internal static int FracToScreen(double v)
    {
        return (int)(v * SCREEN_SIZE);
    }
}
