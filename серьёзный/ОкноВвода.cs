using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace серьёзный
{
    public class ОкноВвода : Window
    {
        private readonly TextBox поле;
        private readonly PasswordBox пароль;
        private readonly bool режимПароля;

        public string Текст
        {
            get
            {
                return режимПароля
                    ? пароль.Password
                    : поле.Text;
            }
        }

        // Старый конструктор полностью сохранён
        public ОкноВвода(
            string заголовок,
            string? начальныйТекст = null)
            : this(
                заголовок,
                начальныйТекст,
                false)
        {
        }

        // Новый режим для пароля
        public ОкноВвода(
            string заголовок,
            string? начальныйТекст,
            bool парольВместоТекста)
        {
            режимПароля =
                парольВместоТекста;

            Title = заголовок;

            Width = 460;
            Height = 210;

            ResizeMode =
                ResizeMode.NoResize;

            WindowStartupLocation =
                WindowStartupLocation.CenterOwner;

            Background =
                new SolidColorBrush(
                    Color.FromRgb(
                        15,
                        23,
                        42));

            поле =
                new TextBox();

            пароль =
                new PasswordBox();

            var панель =
                new StackPanel
                {
                    Margin = new Thickness(22)
                };

            панель.Children.Add(
                new TextBlock
                {
                    Text = заголовок,
                    FontSize = 17,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(
                        0,
                        0,
                        0,
                        14)
                });

            if (режимПароля)
            {
                пароль.Height = 38;
                пароль.Password = начальныйТекст ?? "";
                пароль.Foreground = Brushes.White;
                пароль.Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            30,
                            41,
                            59));
                пароль.BorderBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            71,
                            85,
                            105));

                панель.Children.Add(
                    пароль);
            }
            else
            {
                поле.Height = 38;
                поле.Text = начальныйТекст ?? "";
                поле.Foreground = Brushes.White;
                поле.CaretBrush = Brushes.White;
                поле.Background =
                    new SolidColorBrush(
                        Color.FromRgb(
                            30,
                            41,
                            59));
                поле.BorderBrush =
                    new SolidColorBrush(
                        Color.FromRgb(
                            71,
                            85,
                            105));

                панель.Children.Add(
                    поле);
            }

            var кнопки =
                new StackPanel
                {
                    Orientation =
                        Orientation.Horizontal,

                    HorizontalAlignment =
                        HorizontalAlignment.Right,

                    Margin = new Thickness(
                        0,
                        20,
                        0,
                        0)
                };

            var отмена =
                new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 34,
                    Margin = new Thickness(
                        0,
                        0,
                        10,
                        0)
                };

            отмена.Click += (_, _) =>
            {
                DialogResult = false;
            };

            var ок =
                new Button
                {
                    Content = "OK",
                    Width = 100,
                    Height = 34
                };

            ок.Click += (_, _) =>
            {
                DialogResult = true;
            };

            кнопки.Children.Add(отмена);
            кнопки.Children.Add(ок);

            панель.Children.Add(кнопки);

            Content = панель;

            Loaded += (_, _) =>
            {
                if (режимПароля)
                {
                    пароль.Focus();
                }
                else
                {
                    поле.Focus();
                    поле.SelectAll();
                }
            };
        }
    }
}