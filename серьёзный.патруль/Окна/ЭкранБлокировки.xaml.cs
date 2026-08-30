using System.ComponentModel;
using System.Windows;

namespace серьёзный.Патруль.Окна
{
    public partial class ЭкранБлокировки : Window
    {
        public ЭкранБлокировки(
            string названиеПК)
        {
            InitializeComponent();

            ТекстПК.Text = названиеПК;
        }

        protected override void OnClosing(
            CancelEventArgs e)
        {
            if (!разрешеноЗакрытие)
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        private bool разрешеноЗакрытие;

        public void ЗакрытьПринудительно()
        {
            разрешеноЗакрытие = true;
            Close();
        }
    }
}