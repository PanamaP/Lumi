using SDL;
using static SDL.SDL3;

namespace Lumi.Platform;

/// <summary>
/// Manages an SDL3 streaming texture that can be updated with pixel data from SkiaSharp.
/// </summary>
public unsafe class Sdl3RenderTarget : IDisposable
{
    private readonly SDL_Renderer* _renderer;
    private SDL_Texture* _texture;
    private int _width;
    private int _height;
    private bool _disposed;

    public Sdl3RenderTarget(SDL_Renderer* renderer)
    {
        if (renderer == null) throw new ArgumentNullException(nameof(renderer));
        _renderer = renderer;
    }

    public Sdl3RenderTarget(IntPtr rendererPtr)
        : this((SDL_Renderer*)rendererPtr)
    {
    }

    /// <summary>
    /// Ensures the internal texture matches the given dimensions. Recreates it if the size changed.
    /// </summary>
    public void EnsureSize(int width, int height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");

        if (_texture != null && _width == width && _height == height)
            return;

        if (_texture != null)
        {
            SDL_DestroyTexture(_texture);
            _texture = null;
        }

        _texture = SDL_CreateTexture(
            _renderer,
            SDL_PixelFormat.SDL_PIXELFORMAT_BGRA8888,
            SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            width,
            height);

        if (_texture == null)
            throw new InvalidOperationException($"SDL_CreateTexture failed: {SDL_GetError()}");

        _width = width;
        _height = height;
    }

    /// <summary>
    /// Copies pixel data into the streaming texture.
    /// </summary>
    /// <param name="pixelData">Pointer to the pixel buffer (BGRA format).</param>
    /// <param name="pitch">Number of bytes per row.</param>
    public void UpdatePixels(IntPtr pixelData, int pitch)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_texture == null)
            throw new InvalidOperationException("Texture has not been created. Call EnsureSize() first.");

        if (!SDL_UpdateTexture(_texture, (SDL_Rect*)null, pixelData, pitch))
            throw new InvalidOperationException($"SDL_UpdateTexture failed: {SDL_GetError()}");
    }

    /// <summary>
    /// Renders the texture to the screen and presents.
    /// </summary>
    public void Present()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_texture == null)
            throw new InvalidOperationException("Texture has not been created. Call EnsureSize() first.");

        SDL_RenderTexture(_renderer, _texture, (SDL_FRect*)null, (SDL_FRect*)null);
        SDL_RenderPresent(_renderer);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_texture != null)
        {
            SDL_DestroyTexture(_texture);
            _texture = null;
        }

        GC.SuppressFinalize(this);
    }
}
