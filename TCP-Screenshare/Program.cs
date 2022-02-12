using System;
using Veldrid.Sdl2;

namespace TCP_Screenshare
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Sdl2Window windows = new Sdl2Window("", 10, 10, 200, 100, SDL_WindowFlags.Resizable, true);



            Console.ReadLine();
        }
    }
}
