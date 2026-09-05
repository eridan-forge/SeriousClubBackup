using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using серьёзный.Core.CoreShop;
using серьёзный.Core.CoreVideo;
using серьёзный.Сервисы;
using серьёзный.Окна;

namespace серьёзный.Карточки;

public partial class КарточкаТовара : UserControl
{
    private readonly ShopService shop =
        new();

    public ShopItem Item { get; }

    public event Action<Guid>? DeleteRequested;

    public КарточкаТовара(ShopItem item)
    {
        InitializeComponent();

        Item = item;

        Загрузить();

        DragDropCoverService.Enable(
            Обложка,
            CoverDropped);

        Сохранить.Click += (_, _) => Save();

        Удалить.Click += (_, _) =>
            DeleteRequested?.Invoke(Item.Id);
    }

    private void Загрузить()
    {
        Название.Text = Item.Name;
        Цена.Text = Item.Price.ToString();
        Остаток.Text = Item.Stock.ToString();
        Скрыт.IsChecked = Item.Hidden;

        if (File.Exists(Item.Image))
        {
            Обложка.Source =
                new BitmapImage(
                    new Uri(Item.Image));
        }
    }

    private void CoverDropped(string file)
    {
        var editor = new CoverEditorWindow(file);

        if (editor.ShowDialog() != true)
            return;

        var результат = editor.Result;

        if (результат == null)
            return;

        // Раньше здесь копировался исходный "file" целиком —
        // перетаскивание/масштаб в редакторе ни на что не влияли.
        var путь = ShopImageService.NewPath();

        ImageCropService.SaveCrop(результат, путь);

        Item.Image = путь;

        Обложка.Source =
            new BitmapImage(new Uri(Item.Image));

        shop.UpdateItem(Item);
    }

    private void Save()
    {
        Item.Name = Название.Text;

        decimal.TryParse(
            Цена.Text,
            out var price);

        int.TryParse(
            Остаток.Text,
            out var stock);

        Item.Price = price;
        Item.Stock = stock;
        Item.Hidden = Скрыт.IsChecked == true;

        shop.UpdateItem(Item);
    }
}