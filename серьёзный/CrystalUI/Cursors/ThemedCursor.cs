using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32.SafeHandles;

namespace серьёзный.CrystalUI.Cursors;

// Настоящий системный курсор (не WPF-элемент поверх содержимого) —
// рисуется композитором Windows, поэтому не отстаёт от движения мыши.
public static class ThemedCursor
{
    private static readonly Lazy<Cursor> курсор = new(Создать);

    public static Cursor Arrow => курсор.Value;

    private static Cursor Создать()
    {
        const int размер = 32;

        var visual = new DrawingVisual();

        // Без сглаживания краёв: альфа каждого пикселя строго 0 или 255,
        // поэтому не нужно возиться с premultiplied-альфой при переносе
        // пикселей в Win32-битмап.
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);

        using (var dc = visual.RenderOpen())
        {
            var фигура = new StreamGeometry();

            using (var ctx = фигура.Open())
            {
                ctx.BeginFigure(new Point(1, 1), true, true);

                ctx.PolyLineTo(new[]
                {
                    new Point(1, 22),
                    new Point(7, 17),
                    new Point(11, 25),
                    new Point(14, 23.5),
                    new Point(10, 16),
                    new Point(18, 16)
                }, true, true);
            }

            фигура.Freeze();

            var заливка = new LinearGradientBrush(
                Color.FromRgb(0xA9, 0x1E, 0x4D),
               Color.FromRgb(0x72, 0x15, 0x36),
                new Point(0, 0),
                new Point(1, 1));

            заливка.Freeze();

            var обводка = new Pen(
                 new SolidColorBrush(Color.FromRgb(0x2A, 0x08, 0x16)),
                1.6);

            обводка.Freeze();

            dc.DrawGeometry(заливка, обводка, фигура);
        }

        var bmp = new RenderTargetBitmap(
            размер, размер, 96, 96, PixelFormats.Pbgra32);

        bmp.Render(visual);

        var пиксели = new byte[размер * размер * 4];

        bmp.CopyPixels(пиксели, размер * 4, 0);

        return СоздатьИзПикселей(пиксели, размер, размер, 1, 1);
    }

    // =========================================================
    // WIN32: сборка настоящего HCURSOR из готового BGRA-массива
    // =========================================================

    private static Cursor СоздатьИзПикселей(
        byte[] bgra,
        int width,
        int height,
        int hotspotX,
        int hotspotY)
    {
        var bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                // Отрицательная высота = top-down DIB: порядок строк
                // совпадает с тем, что вернул RenderTargetBitmap.CopyPixels.
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 // BI_RGB
            }
        };

        var цветнойBmp = CreateDIBSection(
            IntPtr.Zero, ref bmi, 0, out var указатель, IntPtr.Zero, 0);

        if (цветнойBmp == IntPtr.Zero || указатель == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Не удалось создать DIB-секцию для курсора.");
        }

        Marshal.Copy(bgra, 0, указатель, bgra.Length);

        // 1-битная AND-маска, полностью нулевая: реальная прозрачность
        // берётся из альфа-канала цветного битмапа — стандартный приём
        // для полноцветных курсоров с альфой (начиная с Windows 2000).
        int маскаШагБайт = ((width + 15) / 16) * 2;

        var маскаБиты = new byte[маскаШагБайт * height];

        var маскаBmp = CreateBitmap(width, height, 1, 1, маскаБиты);

        try
        {
            var info = new ICONINFO
            {
                fIcon = false,
                xHotspot = hotspotX,
                yHotspot = hotspotY,
                hbmMask = маскаBmp,
                hbmColor = цветнойBmp
            };

            var hCursor = CreateIconIndirect(ref info);

            if (hCursor == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "CreateIconIndirect вернул пустой хэндл курсора.");
            }

            return CursorInteropHelper.Create(new SafeCursorHandle(hCursor));
        }
        finally
        {
            DeleteObject(цветнойBmp);
            DeleteObject(маскаBmp);
        }
    }

    private sealed class SafeCursorHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeCursorHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle() => DestroyIcon(handle);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(
        IntPtr hdc, ref BITMAPINFO bmi, uint usage,
        out IntPtr bits, IntPtr hSection, uint offset);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateBitmap(
        int nWidth, int nHeight, uint nPlanes, uint nBitCount, byte[] lpBits);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIconIndirect(ref ICONINFO icon);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
}