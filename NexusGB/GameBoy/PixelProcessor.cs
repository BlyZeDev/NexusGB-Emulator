namespace NexusGB.GameBoy;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;
using System.Runtime.CompilerServices;

public sealed class PixelProcessor
{
    private const char Pixel = '█';

    private const int SCREEN_VBLANK_HEIGHT = 153;
    private const int OAM_CYCLES = 80;
    private const int VRAM_CYCLES = 172;
    private const int HBLANK_CYCLES = 204;
    private const int SCANLINE_CYCLES = 456;

    private const int VBLANK_INTERRUPT = 0;
    private const int LCD_INTERRUPT = 1;

    private readonly NexusConsoleGraphic _graphics;
    private readonly MemoryManagement _mmu;
    private readonly int _offsetX;
    private readonly int _offsetY;

    private int scanlineCounter;

    public PixelProcessor(NexusConsoleGraphic graphics, MemoryManagement mmu, in int pixelCountX, in int pixelCountY)
    {
        _graphics = graphics;
        _mmu = mmu;

        _offsetX = (pixelCountX - GameBoySystem.ScreenWidth) / 4;
        _offsetY = (pixelCountY - GameBoySystem.ScreenHeight) / 4;
    }

    public void Update(in int cycles)
    {
        if (!IsLCDEnabled(_mmu.LCDControl))
        {
            scanlineCounter = 0;
            _mmu.LCDControlY = 0;
            _mmu.LCDControlStatus = (byte)(_mmu.LCDControlStatus & ~0x03);
            return;
        }

        scanlineCounter += cycles;

        switch (_mmu.LCDControlStatus & 0x03)
        {
            case 2:
                if (scanlineCounter >= OAM_CYCLES)
                {
                    ChangeSTATMode(3);
                    scanlineCounter -= OAM_CYCLES;
                }
                break;
            case 3:
                if (scanlineCounter >= VRAM_CYCLES)
                {
                    ChangeSTATMode(0);
                    DrawScanLine();
                    scanlineCounter -= VRAM_CYCLES;
                }
                break;
            case 0:
                if (scanlineCounter >= HBLANK_CYCLES)
                {
                    _mmu.LCDControlY++;
                    scanlineCounter -= HBLANK_CYCLES;

                    if (_mmu.LCDControlY == GameBoySystem.ScreenHeight)
                    {
                        ChangeSTATMode(1);
                        _mmu.RequestInterrupt(VBLANK_INTERRUPT);
                        _graphics.Render();
                    }
                    else ChangeSTATMode(2);
                }
                break;
            case 1:
                if (scanlineCounter >= SCANLINE_CYCLES)
                {
                    _mmu.LCDControlY++;
                    scanlineCounter -= SCANLINE_CYCLES;

                    if (_mmu.LCDControlY > SCREEN_VBLANK_HEIGHT)
                    {
                        ChangeSTATMode(2);
                        _mmu.LCDControlY = 0;
                    }
                }
                break;
        }

        if (_mmu.LCDControlY == _mmu.LYCompare)
        {
            Bits.Set(ref _mmu.LCDControlStatus, 2);
            if (Bits.Is(_mmu.LCDControlStatus, 6)) _mmu.RequestInterrupt(LCD_INTERRUPT);
        }
        else Bits.Clear(ref _mmu.LCDControlStatus, 2);
    }

    private void DrawScanLine()
    {
        var lcdControl = _mmu.LCDControl;
        if (Bits.Is(lcdControl, 0)) RenderBackground();
        if (Bits.Is(lcdControl, 1)) RenderSprites();
    }

    private void RenderBackground()
    {
        var windowX = (byte)(_mmu.WindowX - 7);
        var windowY = _mmu.WindowY;
        var lcdControlY = _mmu.LCDControlY;

        if (lcdControlY > GameBoySystem.ScreenHeight) return;

        var lcdControl = _mmu.LCDControl;
        var scrollY = _mmu.ScrollY;
        var scrollX = _mmu.ScrollX;
        var backgroundPalette = _mmu.BackgroundPalette;
        var isWindow = IsWindow(lcdControl, windowY, lcdControlY);

        var y = (byte)(isWindow ? lcdControlY - windowY : lcdControlY + scrollY);
        var tileLine = (byte)((y & 7) * 2);

        var tileRow = (ushort)(y / 8 * 32);
        var tileMap = isWindow ? GetWindowTilemapAddress(lcdControl) : GetBackgroundTilemapAddress(lcdControl);

        byte high = 0;
        byte low = 0;
        for (int p = 0; p < GameBoySystem.ScreenWidth; p++)
        {
            var x = (byte)(isWindow && p >= windowX ? p - windowX : p + scrollX);
            if ((p & 0x07) == 0 || ((p + scrollX) & 0x07) == 0)
            {
                var tileColumn = (ushort)(x / 8);
                var tileAddress = (ushort)(tileMap + tileRow + tileColumn);

                var tileLocation = IsSignedAddress(lcdControl)
                    ? (ushort)(GetTileDataAddress(lcdControl) + _mmu.ReadVRAM(tileAddress) * 16)
                    : (ushort)(GetTileDataAddress(lcdControl) + ((sbyte)_mmu.ReadVRAM(tileAddress) + 128) * 16);

                low = _mmu.ReadVRAM((ushort)(tileLocation + tileLine));
                high = _mmu.ReadVRAM((ushort)(tileLocation + tileLine + 1));
            }

            var colorIdThroughtPalette = GetColorIdThroughtPalette(backgroundPalette, GetColorIdBits(7 - (x & 7), low, high));
            _graphics.DrawPixel(new NexusCoord(p + _offsetX, lcdControlY + _offsetY), new NexusChar(Pixel, new NexusColorIndex(colorIdThroughtPalette), new NexusColorIndex(colorIdThroughtPalette)));
        }
    }

    private void RenderSprites()
    {
        var lcdControlY = _mmu.LCDControlY;

        if (lcdControlY > GameBoySystem.ScreenHeight) return;

        var lcdControl = _mmu.LCDControl;
        for (int i = 0x9C; i >= 0; i -= 4)
        {
            var y = _mmu.ReadOAM(i) - 16;
            var x = _mmu.ReadOAM(i + 1) - 8;
            var tile = _mmu.ReadOAM(i + 2);
            var attribute = _mmu.ReadOAM(i + 3);

            if (lcdControlY >= y && lcdControlY < y + SpriteSize(lcdControl))
            {
                var palette = Bits.Is(attribute, 4) ? _mmu.ObjectPalette1 : _mmu.ObjectPalette0;

                var tileRow = IsYFlipped(attribute) ? SpriteSize(lcdControl) - 1 - (lcdControlY - y) : (lcdControlY - y);

                var tileAddress = (ushort)(0x8000 + tile * 16 + tileRow * 2);
                var low = _mmu.ReadVRAM(tileAddress);
                var high = _mmu.ReadVRAM((ushort)(tileAddress + 1));

                for (int p = 0; p < 8; p++)
                {
                    var colorId = GetColorIdBits(IsXFlipped(attribute) ? p : 7 - p, low, high);
                    var colorIdThroughtPalette = GetColorIdThroughtPalette(palette, colorId);

                    if (x + p >= 0 && x + p < GameBoySystem.ScreenWidth)
                    {
                        var coord = new NexusCoord(x + p + _offsetX, lcdControlY + _offsetY);

                        if (!IsTransparent(colorId) && (IsAboveBackground(attribute) || IsBackgroundWhite(_mmu.BackgroundPalette, coord)))
                            _graphics.DrawPixel(coord, new NexusChar(Pixel, new NexusColorIndex(colorIdThroughtPalette), new NexusColorIndex(colorIdThroughtPalette)));
                    }
                }
            }
        }
    }

    private void ChangeSTATMode(in int mode)
    {
        var stat = (byte)(_mmu.LCDControlStatus & ~0x03);
        _mmu.LCDControlStatus = (byte)(stat | mode);

        if (mode == 0 && Bits.Is(stat, 3) || mode == 1 && Bits.Is(stat, 4) || mode == 2 && Bits.Is(stat, 5))
            _mmu.RequestInterrupt(LCD_INTERRUPT);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsBackgroundWhite(in byte backgroundPalette, in NexusCoord coord) => _graphics.GetPixel(coord).Foreground == (backgroundPalette & 0x03);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsLCDEnabled(in byte lcdControl) => Bits.Is(lcdControl, 7);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWindow(in byte lcdControl, in byte windowY, in byte lcdControlY) => Bits.Is(lcdControl, 5) && windowY <= lcdControlY;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSignedAddress(in byte lcdControl) => Bits.Is(lcdControl, 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetBackgroundTilemapAddress(in byte lcdControl) => (ushort)(Bits.Is(lcdControl, 3) ? 0x9C00 : 0x9800);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetWindowTilemapAddress(in byte lcdControl) => (ushort)(Bits.Is(lcdControl, 6) ? 0x9C00 : 0x9800);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetTileDataAddress(in byte lcdControl) => (ushort)(Bits.Is(lcdControl, 4) ? 0x8000 : 0x8800);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetColorIdBits(in int colorBit, in byte low, in byte high) => ((high >> colorBit) & 0x01) << 1 | ((low >> colorBit) & 0x01);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetColorIdThroughtPalette(in int palette, in int colorId) => (palette >> colorId * 2) & 0x03;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SpriteSize(in byte lcdControl) => Bits.Is(lcdControl, 2) ? 16 : 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsXFlipped(in byte attribute) => Bits.Is(attribute, 5);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsYFlipped(in byte attribute) => Bits.Is(attribute, 6);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTransparent(in int bit) => bit == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAboveBackground(in byte attribute) => attribute >> 7 == 0;
}