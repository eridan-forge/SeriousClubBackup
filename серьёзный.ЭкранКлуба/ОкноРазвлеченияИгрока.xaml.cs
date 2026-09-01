using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreEconomy;
using серьёзный.Сервисы;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноРазвлеченияИгрока : Window
{
    private readonly Guid playerId;

    private readonly PointsService points = new();
    private readonly CasinoService casino = new();
    private readonly CaseService cases = new();
    private readonly InventoryService inventory = new();
    private readonly СервисАккаунтов accounts = new();

    public ОкноРазвлеченияИгрока(Guid playerId)
    {
        InitializeComponent();

        this.playerId = playerId;

        Loaded += (_, _) => Обновить();
    }

    private void Обновить()
    {
        var баланс = points.Get(playerId);

        ТекстБаланса.Text = $"⭐ {баланс.Points} баллов";

        ПостроитьКейсы();
        ПостроитьИнвентарь();
    }

    private void ПостроитьКейсы()
    {
        ПанельКейсов.Children.Clear();

        foreach (var кейс in cases.GetAll().Where(x => x.Enabled))
        {
            var кнопка = new Button
            {
                Content = $"{кейс.Icon} {кейс.Name}\n{кейс.PriceInPoints} баллов",
                Width = 160,
                Height = 70,
                Margin = new Thickness(8)
            };

            кнопка.Click += (_, _) => ОткрытьКейс(кейс.Id);

            ПанельКейсов.Children.Add(кнопка);
        }
    }

    private void ОткрытьКейс(Guid caseId)
    {
        var результат = cases.Open(playerId, caseId, out var ошибка);

        if (результат == null)
        {
            MessageBox.Show(ошибка, "Кейс");
            return;
        }

        if (результат.Type == CaseRewardType.TimeMinutes &&
            int.TryParse(результат.Value, out var минуты))
        {
            var аккаунт = accounts.Получить(playerId);

            if (аккаунт != null)
            {
                accounts.ДобавитьВремя(playerId, TimeSpan.FromMinutes(минуты));
            }
        }

        MessageBox.Show($"🎉 Выпало: {результат.Label}", "Кейс открыт");

        Обновить();
    }

    private void ПостроитьИнвентарь()
    {
        ПанельИнвентаря.Children.Clear();

        foreach (var (item, entry) in inventory.GetOwned(playerId))
        {
            var кнопка = new Button
            {
                Content =
                    $"{item.Icon} {item.Name}\n" +
                    $"+{item.PointsBonusPercent}% баллов, +{item.TimeBonusPercent}% времени\n" +
                    (entry.Equipped ? "✅ Надето" : "Надеть"),
                Width = 180,
                Height = 80,
                Margin = new Thickness(8)
            };

            кнопка.Click += (_, _) =>
            {
                inventory.SetEquipped(playerId, item.Id, !entry.Equipped);
                Обновить();
            };

            ПанельИнвентаря.Children.Add(кнопка);
        }
    }

    private void Крутить_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(ПолеСтавки.Text, out var ставка) || ставка <= 0)
        {
            MessageBox.Show("Введите корректную ставку.");
            return;
        }

        var результат = casino.Play(playerId, ставка, out var ошибка);

        if (!string.IsNullOrEmpty(ошибка))
        {
            MessageBox.Show(ошибка, "Казино");
            return;
        }

        MessageBox.Show(
            результат.Win
                ? $"🎉 Выигрыш! +{результат.Payout} баллов"
                : "😔 Не повезло. Попробуй ещё раз!",
            "Казино");

        Обновить();
    }
}