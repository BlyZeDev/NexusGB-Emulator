namespace NexusGB.Statics;

using System.Runtime.InteropServices;

public static class RomFileHandler
{
    public static string? OpenRomFile(string initialDirectory)
    {
        var ofn = new OpenFileName
        {
            lStructSize = Marshal.SizeOf<OpenFileName>(),
            lpstrFilter = "GameBoy (*.gb)\0*.gb\0",
            lpstrCustomFilter = null!,
            nMaxCustFilter = 0,
            nFilterIndex = 1,
            lpstrFile = new string(stackalloc char[256]),
            nMaxFile = 256,
            lpstrFileTitle = null!,
            nMaxFileTitle = 0,
            lpstrInitialDir = initialDirectory,
            lpstrTitle = "Open a GameBoy Rom",
            Flags = 0x00080000 | 0x00001000,
            nFileOffset = 0,
            nFileExtension = 0,
            lpstrDefExt = null!
        };

        return GetOpenFileName(ref ofn) ? ofn.lpstrFile : null;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OpenFileName ofn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct OpenFileName
    {
        public int lStructSize;
        public nint hwndOwner;
        public nint hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public nint lCustData;
        public nint lpfnHoo;
        public string lpTemplateName;
    }
}