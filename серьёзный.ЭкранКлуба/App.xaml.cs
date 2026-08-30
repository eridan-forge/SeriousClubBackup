using System.Windows;

namespace серьёзный.ЭкранКлуба
{
    public partial class App : Application
    {
        public bool ЭтоПервыйЗапускShell { get; private set; }

        public App()
        {
            ЭтоПервыйЗапускShell = true;

            DispatcherUnhandledException += (_, e) =>
            {
             
                e.Handled = true;
            };
        }
    }
}