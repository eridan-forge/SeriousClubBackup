using System.Windows;
using серьёзный.Патруль.Система;

namespace серьёзный.Окна
{
    public partial class ОкноНастройкиПК : Window
    {
        public ОкноНастройкиПК()
        {
            InitializeComponent();
            Обновить();
        }

        private void Обновить()
        {
            Список.ItemsSource = null;
            Список.ItemsSource = КартаКомпьютеров.Все;
        }

        private void Список_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (Список.SelectedItem is not ЗаписьПК пк)
                return;

            ПолеId.Text = пк.Id.ToString();
            ПолеНазвание.Text = пк.Название;
            ПолеMAC.Text = пк.MAC;
        }

        private void Добавить_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ПолеId.Text, out var id))
            {
                MessageBox.Show("Введите корректный Id."); return;
            }

            try
            {
                КартаКомпьютеров.Добавить(id, ПолеНазвание.Text.Trim(), ПолеMAC.Text.Trim());
                Обновить();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void Сохранить_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ПолеId.Text, out var id))
            {
                MessageBox.Show("Введите корректный Id."); return;
            }

            try
            {
                КартаКомпьютеров.Изменить(id, ПолеНазвание.Text.Trim(), ПолеMAC.Text.Trim());
                Обновить();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void Удалить_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ПолеId.Text, out var id))
                return;

            КартаКомпьютеров.Удалить(id);
            Обновить();
        }
    }
}