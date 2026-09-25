namespace серьёзный.Core.CoreShop;

public static class ShopValidationService
{
    public static bool ValidateItem(
        ShopItem item,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            error = "Введите название товара.";
            return false;
        }

        if (item.Price < 0)
        {
            error = "Цена не может быть отрицательной.";
            return false;
        }

        if (item.Stock < 0)
        {
            error = "Остаток не может быть отрицательным.";
            return false;
        }

        return true;
    }

    public static bool ValidateCategory(
        string name,
        out string error)
    {
        error = "";

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Введите название раздела.";
            return false;
        }

        return true;
    }
}