using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    static void Main() {
        var Prm = new Program();

        string EndPointIP = "192.168.1.255"; // Sets the IP of the ESP32 (Set this to the IP of the ESP32)

        UdpClient Client = new UdpClient(); // Starts the UDP Server

        byte[] ImageBytes = GetDisplayBytes();

        Prm.SendFrame(ImageBytes, Client, EndPointIP, 11000); // Sends the message over UDP to the ESP32

        Client.Close(); // Closes the UDP socket
    }

    static byte[] GetDisplayBytes()
    {
        var Bmp = ScreenCapture.CaptureDesktop(); // Captures the Desktop
        using (var ms = new MemoryStream())
        {
            Bmp.Save(ms, ImageFormat.Jpeg); // Converts the image to Jpeg
            return ms.ToArray();
        }
    }

    void SendFrame(byte[] frameData, UdpClient client, string ip, int port)
    {
        const int chunkSize = 1024;
        int total = frameData.Length;
        int offset = 0;

        while (offset < total)
        {
            int size = Math.Min(chunkSize, total - offset);
            byte[] chunk = new byte[size];

            Buffer.BlockCopy(frameData, offset, chunk, 0, size);

            client.Send(chunk, chunk.Length, ip, port);

            offset += size;
        }
    }
}