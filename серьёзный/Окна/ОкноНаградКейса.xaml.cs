using System.Linq;
using System.Windows;
using серьёзный.Core.CoreEconomy;

namespace серьёзный.Окна;

public partial class ОкноНаградКейса : Window
{
    private readonly CaseInfo caseInfo;
    private readonly string имяАдмина;
    private readonly CaseService cases = new();

    public ОкноНаградКейса(CaseInfo caseInfo, string имяАдмина)
    {
        InitializeComponent();

        this.caseInfo = caseInfo;
        this.имяАдмина = имяАдмина;

        Title = $"Награды кейса «{caseInfo.Name}»";

        Loaded += (_, _) => Обновить();
    }

    private void Обновить()
    {
        ГридНаград.ItemsSource = null;
        ГридНаград.ItemsSource = cases.GetRewards(caseInfo.Id);
    }

    private void Добавить_Click(object sender, RoutedEventArgs e)
    {
        cases.SaveReward(new CaseReward
        {
            CaseId = caseInfo.Id,
            Type = CaseRewardType.Points,
            Value = "50",
            Label = "50 баллов",
            Weight = 10
        }, имяАдмина);

        Обновить();
    }

    private void Сохранить_Click(object sender, RoutedEventArgs e)
    {
        foreach (var r in ГридНаград.ItemsSource.Cast<CaseReward>())
        {
            cases.SaveReward(r, имяАдмина);
        }

        MessageBox.Show("Сохранено.");
    }

    private void Удалить_Click(object sender, RoutedEventArgs e)
    {
        if (ГридНаград.SelectedItem is not CaseReward r)
            return;

        cases.DeleteReward(r.Id, имяАдмина);

        Обновить();
    }
}