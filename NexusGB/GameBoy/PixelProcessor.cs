namespace NexusGB.GameBoy;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;
using System.Runtime.CompilerServices;

public sealed class PixelProcessor
{
    private const char Pixel = '█';

    private const int VBLANK_INTERRUPT = 0;
    private const int LCD_INTERRUPT = 1;

    private readonly NexusConsoleGraphic _graphics;
    private readonly MemoryManagement _mmu;
    private readonly int _offsetX;
    private readonly int _offsetY;

    private int scanlineCounter;
    private int windowCounter;
    private bool mode0Requested;
    private bool mode1Requested;
    private bool mode2Requested;
    private bool vBlankRequested;
    private bool coincidenceRequested;
    private bool lastFrameEnabled;

    private bool IsLcdEnabled => Bits.Is(_mmu.LCDControl, 7);
    private bool IsSignedAddress => !Bits.Is(_mmu.LCDControl, 4);
    private ushort TileDataAddress => (ushort)(Bits.Is(_mmu.LCDControl, 4) ? 0x8000 : 0x8800);
    private int SpriteSize => Bits.Is(_mmu.LCDControl, 2) ? 16 : 8;

    public PixelProcessor(NexusConsoleGraphic graphics, MemoryManagement mmu, in int pixelCountX, in int pixelCountY)
    {
        _graphics = graphics;
        _mmu = mmu;

        _offsetX = (pixelCountX - GameBoySystem.ScreenWidth) / 4;
        _offsetY = (pixelCountY - GameBoySystem.ScreenHeight) / 4;
    }

    public void Update(in int cycles)
    {
        if (!IsLcdEnabled)
        {
            _mmu.LCDControlY = 0;
            scanlineCounter = 0;
            windowCounter = 0;

            mode0Requested = false;
            mode1Requested = false;
            mode2Requested = false;
            coincidenceRequested = false;
            vBlankRequested = false;

            SetMode(0);

            return;
        }

        scanlineCounter += cycles;

        if (scanlineCounter >= 456)
        {
            if (_mmu.LCDControlY < GameBoySystem.ScreenHeight) DrawScanLine();

            coincidenceRequested = false;
            Bits.Clear(ref _mmu.LCDControlStatus, 2);

            _mmu.LCDControlY++;
            scanlineCounter -= 456;
        }

        SetSTAT();

        if (_mmu.LCDControlY != _mmu.LYCompare) return;

        Bits.Set(ref _mmu.LCDControlStatus, 2);

        if (!Bits.Is(_mmu.LCDControlStatus, 6) || coincidenceRequested) return;

        _mmu.RequestInterrupt(LCD_INTERRUPT);
        coincidenceRequested = true;
    }

    private void DrawScanLine()
    {
        if (!lastFrameEnabled) return;

        var lcdControl = _mmu.LCDControl;
        if (Bits.Is(lcdControl, 0)) RenderBackground();
        if (Bits.Is(lcdControl, 5)) RenderWindow();
        if (Bits.Is(lcdControl, 1)) RenderSprites();
    }

    private void RenderBackground()
    {
        var lcdControl = _mmu.LCDControl;
        var lcdControlY = _mmu.LCDControlY;

        var backgroundTilemapY = (lcdControlY + _mmu.ScrollY) / 8 * 32;
        backgroundTilemapY %= 1024;

        for (int i = 0; i < GameBoySystem.ScreenWidth; i++)
        {
            var backgroundTilemapX = (_mmu.ScrollX + i) / 8;
            backgroundTilemapX %= 32;

            var backgroundTilemapIndex = (ushort)((Bits.Is(lcdControl, 3) ? 0x9C00 : 0x9800) + backgroundTilemapX + backgroundTilemapY);
            var backgroundTileDataIndex = TileDataAddress;

            backgroundTileDataIndex += (ushort)(IsSignedAddress
                ? ((sbyte)_mmu.ReadByte(backgroundTilemapIndex) + 128) * 16
                : _mmu.ReadByte(backgroundTilemapIndex) * 16);

            var currentTileLine = (lcdControlY + _mmu.ScrollY) % 8 * 2;
            var currentTileColumn = -((_mmu.ScrollX + i) % 8 - 7);

            var low = _mmu.ReadByte((ushort)(backgroundTileDataIndex + currentTileLine));
            var high = _mmu.ReadByte((ushort)(backgroundTileDataIndex + currentTileLine + 1));

            var paletteIndex = GetColorIdBits(currentTileColumn, low, high);
            var index = new NexusColorIndex(GetColorFromPalette(_mmu.BackgroundPalette, paletteIndex));
            _graphics.DrawPixel(new NexusCoord(i + _offsetX, lcdControlY + _offsetY), new NexusChar(Pixel, index, index));
        }
    }

    private void RenderWindow()
    {
        var lcdControl = _mmu.LCDControl;
        var lcdControlY = _mmu.LCDControlY;

        var windowX = (byte)(_mmu.WindowX - 7);
        if (!Bits.Is(lcdControl, 5) || _mmu.WindowY > lcdControlY || windowX > GameBoySystem.ScreenWidth) return;

        var windowTilemapY = windowCounter++ / 8 * 32;

        for (int i = windowX; i < GameBoySystem.ScreenWidth; i++)
        {
            var windowTilemapX = (i - windowX) / 8;

            var windowTilemapIndex = (ushort)((Bits.Is(lcdControl, 6) ? 0x9C00 : 0x9800) + windowTilemapX + windowTilemapY);
            var windowTileDataIndex = TileDataAddress;

            windowTileDataIndex += (ushort)(IsSignedAddress
                ? ((sbyte)_mmu.ReadByte(windowTilemapIndex) + 128) * 16
                : _mmu.ReadByte(windowTilemapIndex) * 16);

            var currentTileLine = (lcdControlY - _mmu.WindowY) % 8 * 2;
            var currentTileColumn = -((i - windowX) % 8 - 7);

            var low = _mmu.ReadByte((ushort)(windowTileDataIndex + currentTileLine));
            var high = _mmu.ReadByte((ushort)(windowTileDataIndex + currentTileLine + 1));

            var paletteIndex = GetColorIdBits(currentTileColumn, low, high);
            var index = new NexusColorIndex(GetColorFromPalette(_mmu.BackgroundPalette, paletteIndex));
            _graphics.DrawPixel(new NexusCoord(i + _offsetX, lcdControlY + _offsetY), new NexusChar(Pixel, index, index));
        }
    }

    private unsafe void RenderSprites()
    {
        const int MaxSprites = 10;

        var lcdControlY = _mmu.LCDControlY;

        var sprites = stackalloc Sprite[MaxSprites];

        var spriteCount = 0;
        for (ushort i = 0; i < 40 && spriteCount < MaxSprites; i++)
        {
            var oamSpriteAddress = (ushort)(0xFE00 + i * 4);
            var yPosition = (short)(_mmu.ReadByte(oamSpriteAddress) - 16);

            if (lcdControlY < yPosition || lcdControlY >= yPosition + SpriteSize) continue;

            var spriteAddress = oamSpriteAddress++;
            var xPosition = (short)(_mmu.ReadByte(oamSpriteAddress++) - 8);
            var tileNumber = _mmu.ReadByte(oamSpriteAddress++);
            var attributes = _mmu.ReadByte(oamSpriteAddress);

            if (SpriteSize == 16) tileNumber &= 0xFE;

            sprites[spriteCount++] = new Sprite(spriteAddress, xPosition, yPosition, tileNumber, attributes);
        }

        for (int i = 1; i < spriteCount; i++)
        {
            var key = sprites[i];
            var j = i - 1;

            while (j >= 0 && (sprites[j].PosX < key.PosX || sprites[j].PosX == key.PosX && sprites[j].OamAddress < key.OamAddress))
            {
                sprites[j + 1] = sprites[j];
                j--;
            }

            sprites[j + 1] = key;
        }

        for (int i = 0; i < spriteCount; i++)
        {
            ref var sprite = ref sprites[i];

            var attributes = sprite.Attributes;
            var spriteLine = lcdControlY - sprite.PosY;

            if (Bits.Is(attributes, 6))
            {
                spriteLine -= SpriteSize - 1;
                spriteLine = -spriteLine;
            }

            spriteLine *= 2;

            var spriteDataAddress = (ushort)(0x8000 + spriteLine + sprite.TileNumber * 16);
            var spriteDataLow = _mmu.ReadByte(spriteDataAddress++);
            var spriteDataHigh = _mmu.ReadByte(spriteDataAddress);

            for (int spritePixelIndex = 7; spritePixelIndex >= 0; spritePixelIndex--)
            {
                var paletteIndex = GetColorIdBits(spritePixelIndex, spriteDataLow, spriteDataHigh);

                if (paletteIndex == 0) continue;

                var colorId = GetColorFromPalette(Bits.Is(attributes, 4) ? _mmu.ObjectPalette1 : _mmu.ObjectPalette0, paletteIndex);

                var spriteDataIndex = spritePixelIndex;

                if (!Bits.Is(attributes, 5))
                {
                    spriteDataIndex -= 7;
                    spriteDataIndex = -spriteDataIndex;
                }

                var bufferXIndex = sprite.PosX + spriteDataIndex;

                if (bufferXIndex is < GameBoySystem.ScreenWidth and >= 0 && lcdControlY < GameBoySystem.ScreenHeight)
                {
                    var coord = new NexusCoord(bufferXIndex + _offsetX, lcdControlY + _offsetY);

                    if (Bits.Is(attributes, 7))
                    {
                        if (_graphics.GetPixel(coord).Foreground != (_mmu.BackgroundPalette & 0x03)) continue;
                    }

                    var index = new NexusColorIndex(colorId);
                    _graphics.DrawPixel(coord, new NexusChar(Pixel, index, index));
                }                
            }
        }
    }

    private void SetSTAT()
    {
        if (_mmu.LCDControlY >= GameBoySystem.ScreenHeight)
        {
            SetMode(1);

            mode0Requested = false;
            mode2Requested = false;

            if (Bits.Is(_mmu.LCDControlStatus, 4) && !mode1Requested)
            {
                _mmu.RequestInterrupt(LCD_INTERRUPT);
                mode1Requested = true;
            }

            if (!vBlankRequested)
            {
                _mmu.RequestInterrupt(VBLANK_INTERRUPT);
                vBlankRequested = true;
            }

            if (_mmu.LCDControlY <= 153) return;

            vBlankRequested = false;

            _mmu.LCDControlY = 0;
            scanlineCounter = 0;
            windowCounter = 0;

            lastFrameEnabled = IsLcdEnabled;
            _graphics.Render();
        }
        else
        {
            switch (scanlineCounter)
            {
                case < 80:
                    SetMode(2);

                    mode0Requested = false;
                    mode1Requested = false;

                    if (!Bits.Is(_mmu.LCDControlStatus, 5) || mode2Requested) return;

                    _mmu.RequestInterrupt(LCD_INTERRUPT);
                    mode2Requested = true;
                    break;
                case < 390:
                    SetMode(3);

                    mode0Requested = false;
                    mode1Requested = false;
                    mode2Requested = false;
                    break;
                default:
                    SetMode(0);

                    mode1Requested = false;
                    mode2Requested = false;

                    if (!Bits.Is(_mmu.LCDControlStatus, 3) || mode0Requested) return;

                    _mmu.RequestInterrupt(LCD_INTERRUPT);
                    mode0Requested = true;
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetMode(in byte value)
    {
        _mmu.LCDControlStatus &= 0b1111_1100;
        _mmu.LCDControlStatus |= value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetColorIdBits(in int currentTileColumn, in byte low, in byte high)
        => (byte)(((Bits.Is(high, currentTileColumn) ? 1 : 0) << 1) | (Bits.Is(low, currentTileColumn) ? 1 : 0));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetColorFromPalette(in int palette, in int colorId) => (palette >> colorId * 2) & 0x03;

    private readonly ref struct Sprite
    {
        public readonly ushort OamAddress;
        public readonly short PosX;
        public readonly short PosY;
        public readonly byte TileNumber;
        public readonly byte Attributes;

        public Sprite(scoped in ushort oamAddress, scoped in short x, scoped in short y, scoped in byte tileNumber, scoped in byte attributes)
        {
            OamAddress = oamAddress;
            PosX = x;
            PosY = y;
            TileNumber = tileNumber;
            Attributes = attributes;
        }
    }
}