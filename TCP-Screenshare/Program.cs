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

            string input = "A";

            while (!input.Equals("C") && !input.Equals("S"))
            {
                Console.WriteLine("(C)lient or (S)erver?");
                input = Console.ReadLine();
            }

            IPAddress[] ipaddr = new IPAddress[0];
            int port = 1234;
            while (ipaddr.Length == 0)
            {
                Console.WriteLine("Enter IP:");
                try
                {
                    string[] temp = Console.ReadLine().Split(':');

                    ipaddr = Dns.GetHostAddresses(temp[0]);
                    if (temp.Length > 0)
                        port = int.Parse(temp[1]);
                }
                catch
                {

                }
            }

            Console.Clear();
            Console.WriteLine("Starting...");

            if (input.Equals("S"))
                Server(ipaddr[0], port);
            else
                Client(ipaddr[0], port);

            Console.WriteLine("\nEnd.");
            Console.ReadLine();
        }


        static void Server(IPAddress address, int port)
        {

            //Rectangle captureRectangle = new Rectangle(0, 0, 1920, 1080);
            Rectangle captureRectangle = new Rectangle(0, 0, 1920, 1080);
            Bitmap captureBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);
            Graphics captureGraphics = Graphics.FromImage(captureBitmap);

            //captureBitmap.Save(@"Capture.jpg", ImageFormat.Jpeg);


            captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
            BitmapData tempbitmap = captureBitmap.LockBits(captureRectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            byte[] data = new byte[Math.Abs(tempbitmap.Stride * tempbitmap.Height)];
            Marshal.Copy(tempbitmap.Scan0, data, 0, data.Length);
            

            while(true)
            {
                Socket ogsocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                ogsocket.Bind(new IPEndPoint(address, port));
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
                        //Console.WriteLine("> -------------------------------------");
                        //Console.WriteLine("> Unlocking");
                        captureBitmap.UnlockBits(tempbitmap);
                        //Console.WriteLine("> Copying");
                        captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);

                        // Console.WriteLine("> Locking");
                        tempbitmap = captureBitmap.LockBits(captureRectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
                        data = new byte[Math.Abs(tempbitmap.Stride * tempbitmap.Height)];
                        Console.WriteLine("> Copying");
                        Marshal.Copy(tempbitmap.Scan0, data, 0, data.Length);

                        {
                            //Console.WriteLine("> Sending Size");
                            int number = data.Length;
                            byte[] bytes = BitConverter.GetBytes(number);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(bytes);
                            socket.Send(bytes);
                        }

                        //Console.WriteLine("> Sending Image");
                        socket.Send(data);
                        //Console.WriteLine("> Sending Done");
                        //System.Threading.Thread.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                    //Console.WriteLine($"Error: {ex}");
                }
            }
        }
        static void Client(IPAddress address, int port)
        {
            IntPtr window = new IntPtr();
            IntPtr renderer = new IntPtr();


            try
            {
                Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(address, port));

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
                window = SDL.SDL_CreateWindow("Screenshare Window", 20, 20, width, height, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
                renderer = SDL.SDL_CreateRenderer(window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
                //SDL.SDL_CreateWindowAndRenderer(width, height, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, out window, out renderer);
                SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
                SDL.SDL_RenderClear(renderer);
                SDL.SDL_RenderPresent(renderer);

                SDL.SDL_Rect temprec = new SDL.SDL_Rect() { x = 0, y = 0, h = height, w = width };
                SDL.SDL_Rect temprec2 = new SDL.SDL_Rect() { x = 0, y = 0, h = height, w = width };

                float ratio = -1;
                {
                    SDL.SDL_GetWindowSize(window, out int w, out int h);

                    float newratio = (float)w / width;
                    if ((float)h / height < newratio)
                        newratio = (float)h / height;

                    //temprec = new SDL.SDL_Rect() { x = (w-width - w) / 2, y = (height - h) / 2, h = height, w = width };

                   //if ((w - width) / 2 >= 0)
                   //     temprec.x = (w - width) / 2;
                   //if ((h - height) / 2 >= 0)
                   //     temprec.y = (h - height) / 2;

                    ratio = newratio;
                    SDL.SDL_RenderClear(renderer);
                    SDL.SDL_RenderSetScale(renderer, newratio, newratio);
                }

                System.Threading.Thread.Sleep(100);

                IntPtr framebuffer = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_ARGB8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);

                int[] pixels = new int[width * height];

                GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    IntPtr pointer = handle.AddrOfPinnedObject();

                   
                    SDL.SDL_UpdateTexture(framebuffer, ref temprec, pointer, width * 4);

                    bool exit = false;
                    while (!exit)
                    {
                        //Console.WriteLine("> -------------------------");
                        SDL.SDL_Event @event;
                        //Console.WriteLine("> Poll Events");
                        while (SDL.SDL_PollEvent(out @event) > 0)
                        {
                            if (@event.type == SDL.SDL_EventType.SDL_QUIT)
                            {
                                exit = true;
                                Console.WriteLine("Exiting Screenshare");
                                socket.Close();
                                break;
                            }
                        }

                        {
                            SDL.SDL_GetWindowSize(window, out int w, out int h);

                            float newratio = (float)w / width;

                            if ((float)h / height < newratio)
                                newratio = (float)h / height;


                            int xoff = (int)(((w - width * newratio) / 2) / newratio);
                            int yoff = (int)(((h - height * newratio) / 2) / newratio);
                            if (xoff < 0)
                                xoff = 0;
                            if (yoff < 0)
                                yoff = 0;

                            temprec2.x = xoff;
                            temprec2.y = yoff;

                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine();
                            //Console.WriteLine($"OG Window:   x: {width}, y: {height}");
                            //Console.WriteLine($"NEW Window:  x: {w}, y: {h}");
                            //Console.WriteLine($"ratio: {newratio}");
                            //Console.WriteLine($"Scaled Window:  x: {(width * newratio)}, y: {(height * newratio)}");
                            //Console.WriteLine($"Window + space: x: {(int)(width * newratio) + 2 * xoff}, y: {(int)(height * newratio) + 2 * yoff}");
                            //Console.WriteLine($"xoff: {xoff}, yoff: {yoff}");
                            //Console.WriteLine($"recx: {temprec2.x}, recy: {temprec2.y}");


                            if (newratio != ratio)
                            {
                                ratio = newratio;
                                SDL.SDL_RenderClear(renderer);
                                SDL.SDL_RenderSetScale(renderer, newratio, newratio);
                            }
                        }

                        int size;
                        {
                            //Console.WriteLine("> Get Size");
                            byte[] aaa = new byte[4];
                            int counter = 0;
                            while (counter < 4)
                                counter += socket.Receive(aaa, counter, 4 - counter, SocketFlags.None);
                            size = BinaryPrimitives.ReadInt32BigEndian(aaa);
                        }
                        byte[] image_arr = new byte[size];
                        {
                            //MessageBox.Show($"Getting Image Data...");
                            //Console.WriteLine("> Getting Image Data");
                            int counter = 0;
                            while (counter < size)
                                counter += socket.Receive(image_arr, counter, size - counter, SocketFlags.None);

                            //Console.WriteLine("> Done");
                            // Decompress it
                            //MessageBox.Show($"Getting Image Data 2...");
                            //image_arr = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(image_arr);

                            //MessageBox.Show($"Getting Image Data 3...");
                        }

                        {
                            //Console.WriteLine("> Writing Image");
                            //MessageBox.Show($"Setting Image...");
                            int index = 0;

                            //Console.WriteLine("> Done");

                            {
                                var Asize = image_arr.Length / 4;
                                
                                for (var Aindex = 0; Aindex < Asize; Aindex++)
                                {
                                    pixels[Aindex] = BitConverter.ToInt32(image_arr, Aindex * 4);
                                }
                            }

                            SDL.SDL_UpdateTexture(framebuffer, ref temprec, pointer, width * 4);


                            SDL.SDL_RenderCopy(renderer, framebuffer, ref temprec, ref temprec2);
                            SDL.SDL_RenderPresent(renderer);


                            

                            //MessageBox.Show($"Setting Image 2...");


                        }

                        //Console.WriteLine("> Drew Image");

                        //System.Threading.Thread.Sleep(100);
                    }
                }
                finally
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
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
