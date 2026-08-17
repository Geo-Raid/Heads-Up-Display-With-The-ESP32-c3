using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using Encoder = System.Drawing.Imaging.Encoder;



class Program
{
    // Dir of the files
    public static string exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    // Display size of the HUD display not the Desktop
    const int DisplayWidth = 320; 
    const int DisplayHeight = 172;

    static void Main() {
        var handle = GetConsoleWindow();
        ShowWindow(handle, 5);

        string EndPointIP = "192.168.1.237"; // Sets the IP of the ESP32 (Set this to the IP of the ESP32)

        UdpClient Client = new UdpClient(); // Starts the UDP Server

        List<byte> ImageBytes = new List<byte>();
        ImageBytes = GetDisplayBytes();
        ImageBytes = RemoveHeaderAndAlphaBytes(ImageBytes);
        ImageBytes = ConvertTo8BitColor(ImageBytes);
        ImageBytes = RLE(ImageBytes);

        Console.WriteLine(ImageBytes.Count);

        File.WriteAllBytes(exeDir + "/test.bin", ImageBytes.ToArray());

        SendFrame(ImageBytes, Client, EndPointIP, 11000); // Sends the message over UDP to the ESP32

        Client.Close(); // Closes the UDP socket
    }

    static List<byte> GetDisplayBytes()
    {
        var Bmp = ScreenCapture.CaptureDesktop(); // Captures the Desktop
        Bmp = new Bitmap(Bmp, new System.Drawing.Size(DisplayWidth, DisplayHeight)); // Converts the size of the image to the size of the HUD display

        using (var ms = new MemoryStream())
        {
            Bmp.Save(ms, ImageFormat.Bmp); // Converts the image to Jpeg
            Bmp.Save("Image.Bmp", ImageFormat.Bmp);
            return ms.ToArray().ToList(); // Converts the Memory Stream to an Array and then to a List
        }
    }

    static void SendFrame(List<byte> FrameData, UdpClient Client, string IP, int Port)
    {
        const int chunkSize = 1024; // Number of bytes in a package
        int Total = FrameData.Count;
        int Offset = 0; // Offset of the pointer in the image data

        // Sends the Length of the entire Image before sending the image data
        ushort LengthOfImage = (ushort)FrameData.Count();
        byte[] LengthHeader = new byte[3];
        LengthHeader[0] = (byte)(LengthOfImage >> 8); // High Byte of the Length
        LengthHeader[1] = (byte)(LengthOfImage & 0xFF); // Low Byte of the Length
        LengthHeader[2] = (byte)(Math.Min(chunkSize, Total - Offset)); // How many packets are going to be sent
        Client.Send(LengthHeader, 3, IP, Port);

        Console.WriteLine(LengthHeader[0].ToString("X2") + LengthHeader[1].ToString("X2"));


        while (Offset < Total)
        {
            int Size = Math.Min(chunkSize, Total - Offset);
            byte[] Chunk = new byte[Size]; // Gets 1024 bytes or less if not enough bytes and seperates them into chunks (becasuse UDP doesn't allow sending the enitre image at once)

            Buffer.BlockCopy(FrameData.ToArray(), Offset, Chunk, 0, Size); // Coppies all the bytes for the packet from the frameData variable into the chunk variable

            Client.Send(Chunk, Chunk.Length, IP, Port); // Sends the Packet to the ESP32

            Offset += Size;
        }
    }

    static List<byte> RemoveHeaderAndAlphaBytes(List<byte> ImageBytes)
    {
        for (int i = 0; i < 54; i += 1) {
            ImageBytes.RemoveAt(0); // Removes the header of the BMP file (the first 53 bytes up till 0x35 in the file)
        }

        for (int i = 3; i < ImageBytes.Count; i += 3){
            ImageBytes.RemoveAt(i); // Remove all the Alpha bytes from the picture every 4 bytes (because we don't need transparancy data and Alpha bytes are always 0xFF)
        }
        return ImageBytes;
    }

    static List<byte> ConvertTo8BitColor(List<byte> ImageBytes)
    {
        List<byte> ConvertedImage = [];
        byte[] InitialColor = [0x00, 0x00, 0x00];
        byte[] FinalColor = [0x00, 0x00, 0x00];
        decimal[] ColorPercentage = [0, 0, 0];

        for (int i = 0; i < ImageBytes.Count;i += 3)
        {
            InitialColor[0] = ImageBytes[i]; // Sets the Blue Byte
            InitialColor[1] = ImageBytes[i+1]; // Sets the Green Byte
            InitialColor[2] = ImageBytes[i+2]; // Stets the Red Byte

            // Gets the Percentage (0 - 1) of each color channel
            ColorPercentage[0] = Math.Round((decimal)InitialColor[0] / 255, 5); // Gets the Blue channel Percentage
            ColorPercentage[1] = Math.Round((decimal)InitialColor[1] / 255, 5); // Gets the Green channel Percentage
            ColorPercentage[2] = Math.Round((decimal)InitialColor[2] / 255, 5); // Gets the Red channel Percentage

            // Converts the 24-Bit Color to the 8-Bit Color
            // Uses the Percentage to find the matching value
            FinalColor[0] = (byte)Math.Round((decimal)(4 * ColorPercentage[0])); // 8-Bit Blue
            FinalColor[1] = (byte)Math.Round((decimal)(8 * ColorPercentage[1])); // 8-Bit Green
            FinalColor[2] = (byte)Math.Round((decimal)(8 * ColorPercentage[2])); // 8-Bit Red

            // Stitches all the Colors together into 1 Byte (R3 G3 B2)
            ConvertedImage.Add((byte)(FinalColor[2] << 5 | FinalColor[1] << 2 | FinalColor[0]));
        }
        return ConvertedImage;
    }

    static List<byte> RLE(List<byte> ImageBytes)
    {
        ushort Offset = 0x0000;
        ushort Count = 0x0001;
        byte Pointer = 0x00;
        List<byte> CompressedImage = [];

        Pointer = ImageBytes[Offset];

        while (Offset < ImageBytes.Count - 1)
        {
            if (Pointer == ImageBytes[Offset + 1] && Offset != ImageBytes.Count() - 1) // Runs if the Next Byte is the same as the last
            {
                Count++;
            } else // Runs if the Next Byte is Not the same as the last
            {
                CompressedImage.Add((byte)((Count & 0xFF00) >> 8)); // Adds the low Byte of the Offset to the Image 
                CompressedImage.Add((byte)(Count & 0x00FF)); // Adds the high Byte of the Offset to the Image
                CompressedImage.Add(Pointer); // Adds the Value to the Image
                Count = 0x0001;

                Pointer = ImageBytes[Offset]; // Sets the Pointer to the Next Byte in the Sequence
            }
            Offset++;
        }

        if  (CompressedImage.Count > ImageBytes.Count){ // If the Compressed Image is bigger than the Original just send the Original Image
            CompressedImage = ImageBytes; // Sets the Return value to the Orininal Image
        }

        return CompressedImage;
    }
}
