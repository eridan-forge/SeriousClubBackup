using System.Windows;
using серьёзный.Патруль.Окна;

namespace серьёзный.Патруль.Сервисы
{
    public class СервисБлокировки
    {
        private ЭкранБлокировки? окно;

        public bool Заблокирован =>
            окно != null;

        public void Заблокировать(
    string названиеПК)
        {
            if (окно != null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                окно =
                    new ЭкранБлокировки(
                        названиеПК);

                окно.Show();
            });

            СервисСостоянияБлокировки.Сохранить(
                true,
                названиеПК);
        }

        public void Разблокировать()
        {
            if (окно == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                окно.ЗакрытьПринудительно();
                окно = null;
            });

            СервисСостоянияБлокировки.Сохранить(
                false,
                string.Empty);
        }
    }
}