using System.Windows;
using System.Windows.Media.Imaging;
using серьёзный.Core.CoreShop;
using серьёзный.Core.CoreVideo;
using серьёзный.Сервисы;


namespace серьёзный.Окна;

public partial class ОкноСозданияТовара : Window
{
    private readonly ShopService shop = new();

    private string image = "";

    private readonly Guid categoryId;

    public ОкноСозданияТовара(Guid category)
    {
        InitializeComponent();

        categoryId = category;

        DragDropCoverService.Enable(
            Обложка,
            CoverDropped);

        Создать.Click += (_, _) => Save();
    }

    private void CoverDropped(string file)
    {
        var editor =
            new CoverEditorWindow(file);

        if (editor.ShowDialog() != true)
            return;

        var результат = editor.Result;

        if (результат == null)
            return;

        // Раньше здесь копировался исходный "file" целиком —
        // перетаскивание/масштаб в редакторе ни на что не влияли.
        image = ShopImageService.NewPath();

        ImageCropService.SaveCrop(результат, image);

        Обложка.Source =
            new BitmapImage(
                new Uri(image));
    }

    private void Save()
    {
        decimal.TryParse(
            Цена.Text,
            out var price);

        int.TryParse(
            Остаток.Text,
            out var stock);

        var item =
            new ShopItem
            {
                CategoryId = categoryId,
                Name = Название.Text,
                Price = price,
                Description = Описание.Text,
                Stock = stock,
                Image = image
            };

        shop.AddItem(item);

        DialogResult = true;
    }
}