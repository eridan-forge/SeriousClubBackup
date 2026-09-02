using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace серьёзный.ЭкранКлуба.Уведомления;

public partial class AchievementToast : Window
{
    public AchievementToast(string название, string описание)
    {
        InitializeComponent();

        Название.Text = название;
        Описание.Text = описание;

        Loaded += ПриЗагрузке;
    }

    private async void ПриЗагрузке(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Left =
                SystemParameters.WorkArea.Right - Width - 20;

            Top =
                SystemParameters.WorkArea.Top - Height;

            BeginAnimation(
                TopProperty,
                new DoubleAnimation(
                    Top,
                    20,
                    TimeSpan.FromMilliseconds(280)));

            await Task.Delay(6000);

            if (!IsVisible)
                return;

            var anim = new DoubleAnimation(
                Top,
                -Height,
                TimeSpan.FromMilliseconds(220));

            anim.Completed += (_, _) => Close();

            BeginAnimation(TopProperty, anim);
        }
        catch
        {
            // Анимация не должна ронять окно игрока.
        }
    }
}