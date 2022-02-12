using System;
using SDL2;

namespace TCP_Screenshare
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //IntPtr window = SDL.SDL_CreateWindow("TEST", 100, 100, 720, 480, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            //IntPtr renderer = SDL.SDL_CreateRenderer(window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            //SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);

            //SDL.SDL_RenderDrawLine(renderer, 10, 10, 30, 20);

            IntPtr window;
            IntPtr renderer;


            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            SDL.SDL_CreateWindowAndRenderer(720, 480, 0, out window, out renderer);
            SDL.SDL_SetRenderDrawColor(renderer, 0, 0, 0, 0);
            SDL.SDL_RenderClear(renderer);
            SDL.SDL_SetRenderDrawColor(renderer, 255, 0, 0, 255);
            for (int i = 0; i < 720; ++i)
                SDL.SDL_RenderDrawPoint(renderer, i, i);
            SDL.SDL_RenderPresent(renderer);


        





        Console.ReadLine();
        }
    }
}
