using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreServices;

namespace серьёзный.ЭкранКлуба;

public partial class ОкноРазвлеченияИгрока : Window
{
    private readonly Guid playerId;

    private EconomySummaryDto? текущаяСводка;

    public ОкноРазвлеченияИгрока(Guid playerId)
    {
        InitializeComponent();

        this.playerId = playerId;

        Loaded += (_, _) =>
            _ = ОбновитьAsync(new EconomyRequestDto { Action = EconomyAction.GetSummary });
    }

    private async Task ОбновитьAsync(EconomyRequestDto запрос)
    {
        var requestId = EconomyBridgeService.CreateRequest(playerId, запрос);

        EconomyResultDto? результат = null;

        for (int i = 0; i < 30; i++) // до ~6 сек
        {
            await Task.Delay(200);

            результат = EconomyBridgeService.GetResult(requestId);

            if (результат != null)
                break;
        }

        if (результат == null)
        {
            MessageBox.Show("Сервер не ответил. Попробуйте ещё раз.", "Развлечения");
            return;
        }

        if (!результат.Success)
        {
            MessageBox.Show(результат.Error ?? "Ошибка.", "Развлечения");
        }

        if (результат.RewardLabel != null)
        {
            MessageBox.Show($"🎉 Выпало: {результат.RewardLabel}", "Кейс открыт");
        }

        if (запрос.Action == EconomyAction.PlayCasino && результат.Success)
        {
            MessageBox.Show(
                результат.Win
                    ? $"🎉 Выигрыш! +{результат.Payout} баллов"
                    : "😔 Не повезло. Попробуй ещё раз!",
                "Казино");
        }

        текущаяСводка = результат.Summary;

        if (текущаяСводка != null)
            Обновить();
    }

    private void Обновить()
    {
        if (текущаяСводка == null)
            return;

        ТекстБаланса.Text = $"⭐ {текущаяСводка.Points} баллов" +
            (текущаяСводка.Premium ? " • ПРЕМИУМ" : "");

        ПостроитьКейсы();
        ПостроитьИнвентарь();
    }

    private void ПостроитьКейсы()
    {
        ПанельКейсов.Children.Clear();

        foreach (var кейс in текущаяСводка!.Cases)
        {
            var кнопка = new Button
            {
                Content = $"{кейс.Icon} {кейс.Name}\n{кейс.PriceInPoints} баллов",
                Width = 160,
                Height = 70,
                Margin = new Thickness(8)
            };

            кнопка.Click += (_, _) => _ = ОбновитьAsync(new EconomyRequestDto
            {
                Action = EconomyAction.OpenCase,
                CaseId = кейс.Id
            });

            ПанельКейсов.Children.Add(кнопка);
        }
    }

    private void ПостроитьИнвентарь()
    {
        ПанельИнвентаря.Children.Clear();

        foreach (var item in текущаяСводка!.Inventory)
        {
            var кнопка = new Button
            {
                Content =
                    $"{item.Icon} {item.Name}\n" +
                    $"+{item.PointsBonusPercent}% баллов, +{item.TimeBonusPercent}% времени\n" +
                    (item.Equipped ? "✅ Надето" : "Надеть"),
                Width = 180,
                Height = 80,
                Margin = new Thickness(8)
            };

            кнопка.Click += (_, _) => _ = ОбновитьAsync(new EconomyRequestDto
            {
                Action = EconomyAction.SetEquipped,
                ItemId = item.Id,
                Equipped = !item.Equipped
            });

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

        _ = ОбновитьAsync(new EconomyRequestDto
        {
            Action = EconomyAction.PlayCasino,
            Bet = ставка
        });
    }
}