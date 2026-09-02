using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreEconomy;
using серьёзный.Core.CoreProfiles;

namespace серьёзный.Окна;

public partial class ОкноРазвлеченияАдмин : Window
{
    private readonly EconomyConfigService economy = new();
    private readonly InventoryService inventory = new();
    private readonly CasinoService casino = new();
    private readonly CaseService cases = new();
    private readonly PointsService points = new();
    private readonly AdminActionLogService audit = new();
    private readonly AchievementThresholdsService пороги = new();

    private readonly string имяАдмина;

    public ОкноРазвлеченияАдмин(string имяАдмина)
    {
        InitializeComponent();

        this.имяАдмина = имяАдмина;

        Loaded += (_, _) => ОбновитьВсё();
    }

    private void ОбновитьВсё()
    {
        ГридУровни.ItemsSource = null;
        ГридУровни.ItemsSource = economy.GetTiers();

        var e = economy.GetEconomy();
        ПолеБаллыЗаМинуту.Text = e.PointsPerMinutePurchased.ToString();
        ПолеБаллыЗаДостижение.Text = e.PointsPerAchievement.ToString();
        ПолеБаллыЗаНапиток.Text = e.PointsPerDrinkPurchase.ToString();
        ПолеПремиумБонус.Text = e.PremiumMultiplierBonusPercent.ToString();

        ГридПредметы.ItemsSource = null;
        ГридПредметы.ItemsSource = inventory.GetAll();

        var c = casino.GetConfig();
        ПолеCasinoMin.Text = c.MinBet.ToString();
        ПолеCasinoMax.Text = c.MaxBet.ToString();
        ПолеCasinoChance.Text = c.WinChancePercent.ToString();
        ПолеCasinoMultiplier.Text = c.WinMultiplier.ToString();
        ФлагCasinoEnabled.IsChecked = c.Enabled;

        ГридКейсы.ItemsSource = null;
        ГридКейсы.ItemsSource = cases.GetAll();

        ГридИстория.ItemsSource = null;
        ГридИстория.ItemsSource = audit.GetRecent();

        var текущиеПороги = пороги.Get();

        ПолеПорогЧасов.Text =
               (текущиеПороги.TenHoursSeconds / 3600.0).ToString("0.##");

        ПолеПорогДрузей.Text = текущиеПороги.FiveFriendsCount.ToString();
        ПолеПорогСеансов.Text = текущиеПороги.VeteranSessionsCount.ToString();
    }

    // ============== ЭКОНОМИКА ==============

    private void СохранитьЭкономику_Click(object sender, RoutedEventArgs e)
    {
        var config = new EconomyConfig
        {
            PointsPerMinutePurchased = double.Parse(ПолеБаллыЗаМинуту.Text),
            PointsPerAchievement = int.Parse(ПолеБаллыЗаДостижение.Text),
            PointsPerDrinkPurchase = int.Parse(ПолеБаллыЗаНапиток.Text),
            PremiumMultiplierBonusPercent = double.Parse(ПолеПремиумБонус.Text)
        };

        economy.SaveEconomy(config, имяАдмина);

        MessageBox.Show("Сохранено.");

        ОбновитьВсё();
    }

    private void ДобавитьУровень_Click(object sender, RoutedEventArgs e)
    {
        var tiers = economy.GetTiers();
        var nextLevel = tiers.Count == 0 ? 1 : tiers.Max(x => x.Level) + 1;

        economy.SaveTier(new LevelTier
        {
            Level = nextLevel,
            Name = $"Уровень {nextLevel}",
            MinPlayedSeconds = 0,
            MultiplierPercent = 100
        }, имяАдмина);

        ОбновитьВсё();
    }

    private void СохранитьУровни_Click(object sender, RoutedEventArgs e)
    {
        foreach (var tier in ГридУровни.ItemsSource.Cast<LevelTier>())
        {
            economy.SaveTier(tier, имяАдмина);
        }

        MessageBox.Show("Уровни сохранены.");
    }

    private void УдалитьУровень_Click(object sender, RoutedEventArgs e)
    {
        if (ГридУровни.SelectedItem is not LevelTier tier)
            return;

        economy.DeleteTier(tier.Level, имяАдмина);

        ОбновитьВсё();
    }

    // ============== ПРЕДМЕТЫ ==============

    private void ДобавитьПредмет_Click(object sender, RoutedEventArgs e)
    {
        inventory.Save(new InventoryItem
        {
            Name = "Новый предмет",
            Icon = "🏆"
        }, имяАдмина);

        ОбновитьВсё();
    }

    private void СохранитьПредметы_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in ГридПредметы.ItemsSource.Cast<InventoryItem>())
        {
            inventory.Save(item, имяАдмина);
        }

        MessageBox.Show("Предметы сохранены.");
    }

    private void УдалитьПредмет_Click(object sender, RoutedEventArgs e)
    {
        if (ГридПредметы.SelectedItem is not InventoryItem item)
            return;

        inventory.Delete(item.Id, имяАдмина);

        ОбновитьВсё();
    }

    // ============== КАЗИНО ==============

    private void СохранитьКазино_Click(object sender, RoutedEventArgs e)
    {
        casino.SaveConfig(new CasinoConfig
        {
            MinBet = int.Parse(ПолеCasinoMin.Text),
            MaxBet = int.Parse(ПолеCasinoMax.Text),
            WinChancePercent = double.Parse(ПолеCasinoChance.Text),
            WinMultiplier = double.Parse(ПолеCasinoMultiplier.Text),
            Enabled = ФлагCasinoEnabled.IsChecked == true
        }, имяАдмина);

        MessageBox.Show("Настройки казино сохранены.");
    }

    // ============== КЕЙСЫ ==============

    private void ДобавитьКейс_Click(object sender, RoutedEventArgs e)
    {
        cases.Save(new CaseInfo
        {
            Name = "Новый кейс",
            PriceInPoints = 100
        }, имяАдмина);

        ОбновитьВсё();
    }

    private void СохранитьКейсы_Click(object sender, RoutedEventArgs e)
    {
        foreach (var c in ГридКейсы.ItemsSource.Cast<CaseInfo>())
        {
            cases.Save(c, имяАдмина);
        }

        MessageBox.Show("Кейсы сохранены.");
    }

    private void УдалитьКейс_Click(object sender, RoutedEventArgs e)
    {
        if (ГридКейсы.SelectedItem is not CaseInfo c)
            return;

        cases.DeleteCase(c.Id, имяАдмина);

        ОбновитьВсё();
    }

    private void НаградыКейса_Click(object sender, RoutedEventArgs e)
    {
        if (ГридКейсы.SelectedItem is not CaseInfo c)
        {
            MessageBox.Show("Сначала выберите кейс.");
            return;
        }

        new ОкноНаградКейса(c, имяАдмина) { Owner = this }.ShowDialog();

        ОбновитьВсё();
    }

    // ============== ИСТОРИЯ ==============

    private void ОбновитьИсторию_Click(object sender, RoutedEventArgs e)
    {
        ГридИстория.ItemsSource = null;
        ГридИстория.ItemsSource = audit.GetRecent();

  
    }

    // ============== ДОСТИЖЕНИЯ ==============

    private void СохранитьПорогиДостижений_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(ПолеПорогЧасов.Text, out var часы) || часы < 0)
        {
            MessageBox.Show("Введите корректное количество часов.");
            return;
        }

        if (!int.TryParse(ПолеПорогДрузей.Text, out var друзья) || друзья < 0)
        {
            MessageBox.Show("Введите корректное количество друзей.");
            return;
        }

        if (!int.TryParse(ПолеПорогСеансов.Text, out var сеансы) || сеансы < 0)
        {
            MessageBox.Show("Введите корректное количество сеансов.");
            return;
        }

        пороги.Save(new AchievementThresholds
        {
            TenHoursSeconds = (long)(часы * 3600),
            FiveFriendsCount = друзья,
            VeteranSessionsCount = сеансы
        }, имяАдмина);

        MessageBox.Show("Пороги достижений сохранены.");
    }
}