using System;
using System.Windows;

namespace серьёзный.Патруль
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var приложение = new App();

            приложение.Run();
        }
    }
}