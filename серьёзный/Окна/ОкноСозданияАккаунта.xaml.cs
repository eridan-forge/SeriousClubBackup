using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Модели;
using серьёзный.Сервисы;

namespace серьёзный.Окна;

public partial class ОкноСозданияАккаунта : Window
{
    private readonly СервисАккаунтов сервис = new();

    private bool идётФорматирование;

    public АккаунтИгрока? СозданныйАккаунт { get; private set; }

    public ОкноСозданияАккаунта()
    {
        InitializeComponent();

        Loaded += (_, _) => ПолеТелефон.Focus();
    }

    private void ПолеТелефон_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (идётФорматирование)
            return;

        идётФорматирование = true;

        try
        {
            var курсорВКонце = ПолеТелефон.CaretIndex == ПолеТелефон.Text.Length;

            var цифры = new string(ПолеТелефон.Text.Where(char.IsDigit).ToArray());

            if (цифры.Length > 0 && цифры[0] == '8')
                цифры = "7" + цифры.Substring(1);
            else if (цифры.Length > 0 && цифры[0] != '7')
                цифры = "7" + цифры;

            if (цифры.Length > 11)
                цифры = цифры.Substring(0, 11);

            var отформатировано = ФорматТелефона(цифры);

            ПолеТелефон.Text = отформатировано;

            ПолеТелефон.CaretIndex = курсорВКонце
                ? отформатировано.Length
                : Math.Min(ПолеТелефон.CaretIndex, отформатировано.Length);
        }
        finally
        {
            идётФорматирование = false;
        }
    }

    private static string ФорматТелефона(string цифры)
    {
        if (цифры.Length == 0)
            return string.Empty;

        var sb = new StringBuilder("+");

        sb.Append(цифры[0]);

        if (цифры.Length > 1)
            sb.Append(" (").Append(цифры.Substring(1, Math.Min(3, цифры.Length - 1)));

        if (цифры.Length > 4)
            sb.Append(") ").Append(цифры.Substring(4, Math.Min(3, цифры.Length - 4)));

        if (цифры.Length > 7)
            sb.Append("-").Append(цифры.Substring(7, Math.Min(2, цифры.Length - 7)));

        if (цифры.Length > 9)
            sb.Append("-").Append(цифры.Substring(9, Math.Min(2, цифры.Length - 9)));

        return sb.ToString();
    }

    private void Создать_Click(object sender, RoutedEventArgs e)
    {
        ТекстОшибка.Visibility = Visibility.Collapsed;

        var телефон = ПолеТелефон.Text.Trim();
        var фио = ПолеФИО.Text.Trim();
        var пароль = ПолеПароль.Password.Trim();

        if (string.IsNullOrWhiteSpace(пароль))
        {
            ПоказатьОшибку("Введите пароль.");
            ПолеПароль.Focus();
            return;
        }

        if (!сервис.Создать(телефон, пароль, фио, out var ошибка))
        {
            ПоказатьОшибку(ошибка);
            return;
        }

        СозданныйАккаунт = сервис.НайтиПоТелефону(телефон);

        DialogResult = true;
    }

    private void ПоказатьОшибку(string текст)
    {
        ТекстОшибка.Text = текст;
        ТекстОшибка.Visibility = Visibility.Visible;
    }
}