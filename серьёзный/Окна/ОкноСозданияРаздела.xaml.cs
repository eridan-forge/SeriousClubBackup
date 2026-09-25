using System.Windows;
using серьёзный.Core.CoreShop;
using System.IO;


namespace серьёзный.Окна;

public partial class ОкноСозданияРаздела : Window
{
    private readonly ShopService shop = new();

    public ОкноСозданияРаздела()
    {
        InitializeComponent();

        Создать.Click += (_, _) =>
        {
            shop.AddCategory(
                new ShopCategory
                {
                    Name = Имя.Text
                });

            

            DialogResult = true;
        };
    }
}