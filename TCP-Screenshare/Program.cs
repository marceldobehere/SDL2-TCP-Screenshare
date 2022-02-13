using System;
using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using SDL2;

namespace TCP_Screenshare
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            string input = "A";

            while (!input.Equals("C") && !input.Equals("S"))
            {
                Console.WriteLine("(C)lient or (S)erver?");
                input = Console.ReadLine();
            }

            if (input.Equals("S"))
                Server();
            else
                Client();

            Console.WriteLine("\nEnd.");
            Console.ReadLine();
        }


        static void Server()
        {

            Rectangle captureRectangle = new Rectangle(0, 0, 1920, 1080);
            Bitmap captureBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);
            Graphics captureGraphics = Graphics.FromImage(captureBitmap);

            //captureBitmap.Save(@"Capture.jpg", ImageFormat.Jpeg);


            captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
            BitmapData tempbitmap = captureBitmap.LockBits(captureRectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] data = new byte[Math.Abs(tempbitmap.Stride * tempbitmap.Height)];
            Marshal.Copy(tempbitmap.Scan0, data, 0, data.Length);
            

            Socket ogsocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            ogsocket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1234));
            ogsocket.Listen(100);
            Socket socket = ogsocket.Accept();


            {
                int number = captureBitmap.Width;
                byte[] bytes = BitConverter.GetBytes(number);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                socket.Send(bytes);
            }

            {
                int number = captureBitmap.Height;
                byte[] bytes = BitConverter.GetBytes(number);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                socket.Send(bytes);
            }




            try
            {
                System.Threading.Thread.Sleep(100);

                while (true)
                {
                    Console.WriteLine("> -------------------------------------");
                    Console.WriteLine("> Unlocking");
                    captureBitmap.UnlockBits(tempbitmap);
                    Console.WriteLine("> Copying");
                    captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);

                    Console.WriteLine("> Locking");
                    tempbitmap = captureBitmap.LockBits(captureRectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    data = new byte[Math.Abs(tempbitmap.Stride * tempbitmap.Height)];
                    Console.WriteLine("> Copying");
                    Marshal.Copy(tempbitmap.Scan0, data, 0, data.Length);

                    {
                        Console.WriteLine("> Sending Size");
                        int number = data.Length;
                        byte[] bytes = BitConverter.GetBytes(number);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);
                        socket.Send(bytes);
                    }

                    Console.WriteLine("> Sending Image");
                    socket.Send(data);
                    Console.WriteLine("> Sending Done");
                    System.Threading.Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Console.WriteLine($"Error: {ex}");
            }
        }
        static void Client()
        {
            IntPtr window = new IntPtr();
            IntPtr renderer = new IntPtr();


            try
            {
                Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1").Address, 1234));

                //MessageBox.Show($"Getting Data...");

                int width = 0, height;
                {
                    byte[] aaa = new byte[4];
                    int counter = 0;
                    while (counter < 4)
                        counter += socket.Receive(aaa, counter, 4, SocketFlags.None);
                    width = BinaryPrimitives.ReadInt32BigEndian(aaa);
                }
                {
                    byte[] aaa = new byte[4];
                    int counter = 0;
                    while (counter < 4)
                        counter += socket.Receive(aaa, counter, 4, SocketFlags.None);
                    height = BinaryPrimitives.ReadInt32BigEndian(aaa);
                }






                SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
                window = SDL.SDL_CreateWindow("Screenshare", 20, 20, width, height, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
                renderer = SDL.SDL_CreateRenderer(window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
                //SDL.SDL_CreateWindowAndRenderer(width, height, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, out window, out renderer);
                SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
                SDL.SDL_RenderClear(renderer);
                SDL.SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);
                SDL.SDL_RenderPresent(renderer);

                System.Threading.Thread.Sleep(100);

                while (true)
                {
                    Console.WriteLine("> -------------------------");
                    SDL.SDL_Event @event;
                    Console.WriteLine("> Poll Events");
                    while (SDL.SDL_PollEvent(out @event) > 0) 
                    {
                            /* handle your event here */
                    }
                    int size;
                    {
                        Console.WriteLine("> Get Size");
                        byte[] aaa = new byte[4];
                        int counter = 0;
                        while (counter < 4)
                            counter += socket.Receive(aaa, counter, 4 - counter, SocketFlags.None);
                        size = BinaryPrimitives.ReadInt32BigEndian(aaa);
                    }
                    byte[] image_arr = new byte[size];
                    {
                        //MessageBox.Show($"Getting Image Data...");
                        Console.WriteLine("> Getting Image Data");
                        int counter = 0;
                        while (counter < size)
                            counter += socket.Receive(image_arr, counter, size - counter, SocketFlags.None);

                        Console.WriteLine("> Done");
                        // Decompress it
                        //MessageBox.Show($"Getting Image Data 2...");
                        //image_arr = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(image_arr);

                        //MessageBox.Show($"Getting Image Data 3...");
                    }

                    {
                        Console.WriteLine("> Writing Image");
                        //MessageBox.Show($"Setting Image...");
                        int index = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                byte[] rgb = new byte[3];
                                rgb[2] = image_arr[index]; index++;
                                rgb[1] = image_arr[index]; index++;
                                rgb[0] = image_arr[index]; index++;

                                SDL.SDL_SetRenderDrawColor(renderer, rgb[0], rgb[1], rgb[2], 0);
                                SDL.SDL_RenderDrawPoint(renderer, x, y);
                            }
                        }
                        Console.WriteLine("> Done");
                        //SDL.SDL_RenderPresent(renderer);

                        //MessageBox.Show($"Setting Image 2...");
                    }

                    Console.WriteLine("> Drew Image");

                }


            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                Console.WriteLine($"Error: {ex}");
            }

            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
        }
    }
}
