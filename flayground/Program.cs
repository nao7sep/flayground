using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace flayground
{
    internal class Program
    {
        // Git に ZIP ファイルや大きなバイナリーファイルを入れるのは作法に反する
        // Static レポジトリーに以下の通りファイルを配置した
        // フォントや .exe ファイルのバージョンは ZIP のものと一致する

        // C:\Repositories\Static\フォント\ttfautohint
        //     ttfautohint-1.8.4-win32.7z
        //     ttfautohint.exe

        // C:\Repositories\Static\フォント\UDEVGothicJPDOC
        //     UDEVGothic_v1.2.1.zip
        //     UDEVGothicJPDOC-Bold.ttf
        //     UDEVGothicJPDOC-BoldItalic.ttf
        //     UDEVGothicJPDOC-Italic.ttf
        //     UDEVGothicJPDOC-Regular.ttf
        //     UDEVGothic35JPDOC-Bold.ttf
        //     UDEVGothic35JPDOC-BoldItalic.ttf
        //     UDEVGothic35JPDOC-Italic.ttf
        //     UDEVGothic35JPDOC-Regular.ttf

        // C:\Repositories\Static\フォント\PlemolJP
        //      PlemolJP_v1.5.0.zip
        //      PlemolJP-Bold.ttf
        //      PlemolJP-BoldItalic.ttf
        //      PlemolJP-Italic.ttf
        //      PlemolJP-Regular.ttf
        //      PlemolJP35-Bold.ttf
        //      PlemolJP35-BoldItalic.ttf
        //      PlemolJP35-Italic.ttf
        //      PlemolJP35-Regular.ttf

        private static readonly string
            mTtfautohintExeFilePath = @"C:\Repositories\Static\フォント\ttfautohint\ttfautohint.exe",
            mTtxExeFilePath = @"C:\Program Files\Python311\Scripts\ttx.exe",
            mUdevGothicDirectoryPath = @"C:\Repositories\Static\フォント\UDEVGothicJPDOC",
            mPlemolJpDirectoryPath = @"C:\Repositories\Static\フォント\PlemolJP";

        // .ttx の保存に
        private static readonly UTF8Encoding mEncoding = new UTF8Encoding (encoderShouldEmitUTF8Identifier: false);

        private static readonly List <string> mNewFilePaths = new List <string> ();

        private static void iHandleDirectory (string path)
        {
            foreach (string xFilePath in Directory.GetFiles (path, "*.ttf", SearchOption.TopDirectoryOnly).
                Where (x => Path.GetFileNameWithoutExtension (x).Contains ("-dehinted", StringComparison.OrdinalIgnoreCase) == false))
            {
                string xFileNameWithoutExtension = Path.GetFileNameWithoutExtension (xFilePath),
                    xExtension = Path.GetExtension (xFilePath),
                    xDehintedFilePath = Path.Join (Path.GetDirectoryName (xFilePath), xFileNameWithoutExtension + "-dehinted" + xExtension);

                ProcessStartInfo xStartInfo = new ProcessStartInfo (mTtfautohintExeFilePath);

                xStartInfo.ArgumentList.Add ("--dehint");
                xStartInfo.ArgumentList.Add (xFilePath);
                xStartInfo.ArgumentList.Add (xDehintedFilePath);

                using (Process? xProcess = Process.Start (xStartInfo))
                {
                    if (xProcess != null)
                        xProcess.WaitForExit ();
                }

                // =============================================================================

                xStartInfo = new ProcessStartInfo (mTtxExeFilePath);

                // ttx — fontTools Documentation
                // https://fonttools.readthedocs.io/en/latest/ttx.html

                xStartInfo.ArgumentList.Add ("-f");

                // でかいし不要
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("cmap");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("glyf");

                // なくてよさそう
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("GlyphOrder");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("maxp");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("hmtx");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("loca");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("post");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("gasp");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("GDEF");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("GPOS");
                xStartInfo.ArgumentList.Add ("-x");
                xStartInfo.ArgumentList.Add ("GSUB");

                // head, hhea, OS/2, name が出力される
                // そのうち必要なものだけ指定しての出力でもよいが、今後の派生開発があるなら、ほかにも含まれるテーブルを知りたい

                xStartInfo.ArgumentList.Add (xDehintedFilePath);

                using (Process? xProcess = Process.Start (xStartInfo))
                {
                    if (xProcess != null)
                        xProcess.WaitForExit ();
                }

                // =============================================================================

                string xTtxFilePath = Path.ChangeExtension (xDehintedFilePath, ".ttx"), // BOM なしの UTF-8
                    xFileContents = File.ReadAllText (xTtxFilePath, Encoding.UTF8);

                // 雑だが動く

                static string iReplace (string value)
                {
                    // 長いものから順に置換
                    // 最後に、置換しすぎを直す

                    return value.
                        Replace ("UDEV Gothic 35JPDOC", "Udev39").
                        Replace ("UDEV Gothic JPDOC", "Udev").
                        Replace ("UDEVGothic35JPDOC", "Udev39").
                        Replace ("UDEVGothicJPDOC", "Udev").
                        Replace ("PlemolJP35", "Plemo39").
                        Replace ("PlemolJP", "Plemo").
                        Replace ("[Plemo]", "[PlemolJP]");
                }

                string xNewFileContents = iReplace (xFileContents);

                string xNewTtxFilePath = Path.Join (Path.GetDirectoryName (xTtxFilePath), Path.GetFileNameWithoutExtension (xTtxFilePath) + "-new" + Path.GetExtension (xTtxFilePath));
                File.WriteAllText (xNewTtxFilePath, xNewFileContents, mEncoding);

                // =============================================================================

                string xNewFilePath = Path.Join (Environment.GetFolderPath (Environment.SpecialFolder.DesktopDirectory), iReplace (Path.GetFileName (xFilePath)));

                xStartInfo = new ProcessStartInfo (mTtxExeFilePath);

                xStartInfo.ArgumentList.Add ("-f");

                xStartInfo.ArgumentList.Add ("-o");
                xStartInfo.ArgumentList.Add (xNewFilePath);
                xStartInfo.ArgumentList.Add ("-m");
                xStartInfo.ArgumentList.Add (xDehintedFilePath);

                xStartInfo.ArgumentList.Add (xNewTtxFilePath);

                using (Process? xProcess = Process.Start (xStartInfo))
                {
                    if (xProcess != null)
                        xProcess.WaitForExit ();
                }

                // =============================================================================
#if !DEBUG
                File.Delete (xDehintedFilePath);
                File.Delete (xTtxFilePath);
                File.Delete (xNewTtxFilePath);
#endif
                mNewFilePaths.Add (xNewFilePath);
            }
        }

        private static void iCompressFiles (IEnumerable <string> paths)
        {
            string xZipFilePath = Path.Join (Environment.GetFolderPath (Environment.SpecialFolder.DesktopDirectory),
                $"flayground-v●-fonts.zip"); // これを生成するソースのバージョン番号との一致を忘れない名前に

            using (FileStream xStream = new FileStream (xZipFilePath, FileMode.Create)) // あれば上書き
            using (ZipArchive xArchive = new ZipArchive (xStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                foreach (string xPath in paths)
                {
                    xArchive.CreateEntryFromFile (xPath, Path.GetFileName (xPath));
#if !DEBUG
                    File.Delete (xPath);
#endif
                }

                void iAddLicenseFile (string directoryPath, string fontName)
                {
                    xArchive.CreateEntryFromFile (Path.Join (directoryPath, "LICENSE"), $"LICENSE-{fontName}");
                }

                // それぞれの LICENSE ファイル内でフォント名は "UDEV Gothic", "PlemolJP" とされている
                iAddLicenseFile (mUdevGothicDirectoryPath, "UDEVGothic");
                iAddLicenseFile (mPlemolJpDirectoryPath, "PlemolJP");
            }
        }

        static void Main (/* string [] args */)
        {
            try
            {
                iHandleDirectory (mUdevGothicDirectoryPath);
                iHandleDirectory (mPlemolJpDirectoryPath);
                iCompressFiles (mNewFilePaths);
            }

            catch (Exception xException)
            {
                Console.WriteLine (xException);
                Console.Write ("なんか押しておくんなまし～ ");
                Console.ReadKey (true);
                Console.WriteLine ();
            }
        }
    }
}
