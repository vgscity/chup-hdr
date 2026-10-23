using System.Windows.Forms;

namespace VgsChupHdr;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "VgsChupHdr-mot-ban", out bool moi);
        if (!moi)
        {
            MessageBox.Show(Chu.DangChayRoi, Chu.TenApp, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => NhatKy.Ghi("ThreadException", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => NhatKy.Ghi("UnhandledException", e.ExceptionObject as Exception);
        Application.Run(new UngDungKhay());
    }
}
