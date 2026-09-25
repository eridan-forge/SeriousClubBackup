using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace серьёзный.Патруль.Окна;

public partial class ВсплывающееСообщение : Window
{
    public event Func<string, Task>? ОтветОтправлен;

    private bool диалогОткрыт;
    private bool закрывается;

    public ВсплывающееСообщение(
        string имя,
        string текст)
    {
        InitializeComponent();

        Имя.Text = имя;
        Текст.Text = текст;

        Loaded += ПриЗагрузке;
    }

    private async void ПриЗагрузке(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Left =
                SystemParameters.WorkArea.Right -
                Width -
                20;

            Top =
                SystemParameters.WorkArea.Top -
                Height;

            BeginAnimation(
                TopProperty,
                new DoubleAnimation(
                    Top,
                    20,
                    TimeSpan.FromMilliseconds(280)));

            await Task.Delay(9000);

            if (IsVisible && !диалогОткрыт)
            {
                ЗакрытьКрасиво();
            }
        }
        catch (Exception ex)
        {
            серьёзный.патруль.Сервисы.Лог.Записать(
                "КРАШ ПриЗагрузке: " + ex);
        }
    }

    private async void Ответить_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            диалогОткрыт = true;

            var окно = new ОкноОтвета
            {
                Owner = this
            };

            var ok = окно.ShowDialog();

            диалогОткрыт = false;

            if (ok != true)
            {
                return;
            }

            if (ОтветОтправлен != null)
            {
                await ОтветОтправлен(окно.Текст);
            }

            ЗакрытьКрасиво();
        }
        catch (Exception ex)
        {
            диалогОткрыт = false;

            серьёзный.патруль.Сервисы.Лог.Записать(
                "КРАШ Ответить_Click: " + ex);
        }
    }

    private void Закрыть_Click(
        object sender,
        RoutedEventArgs e)
    {
        ЗакрытьКрасиво();
    }

    private void ЗакрытьКрасиво()
    {
        if (закрывается)
        {
            return;
        }

        закрывается = true;

        var anim = new DoubleAnimation(
            Top,
            -Height,
            TimeSpan.FromMilliseconds(220));

        anim.Completed += (_, _) => Close();

        BeginAnimation(
            TopProperty,
            anim);
    }
}