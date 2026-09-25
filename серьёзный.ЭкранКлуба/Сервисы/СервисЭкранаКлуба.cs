using серьёзный.ЭкранКлуба.Модели;
using серьёзный.ЭкранКлуба.Сервисы;

namespace серьёзный.Патруль.Сервисы;

public static class СервисЭкранаКлуба
{
    public static void Заблокировать()
    {
        var state = StateService.Загрузить();
        state.Locked = true;
        StateService.Сохранить(state);
    }

    public static void Разблокировать()
    {
        var state = StateService.Загрузить();
        state.Locked = false;
        StateService.Сохранить(state);
    }

    public static void СменитьПароль(string пароль)
    {
        var config = ConfigService.Загрузить();
        config.Password = пароль;
        ConfigService.Сохранить(config);
    }

    public static void ИзменитьТекст(string текст)
    {
        var config = ConfigService.Загрузить();
        config.Title = текст;
        ConfigService.Сохранить(config);
    }

    // Локальная смена темы прямо с этого ПК (кнопка "Сменить обои" в
    // обслуживании) — пишет в тот же ScreenConfig.ThemeId, что и
    // сетевая команда от админа (СервисЭкрана.УстановитьТему в
    // патруле). MainWindow уже опрашивает ThemeId каждые 3 секунды,
    // поэтому новая тема подхватится сама, без доп. кода.
    public static void УстановитьТему(string themeId)
    {
        var config = ConfigService.Загрузить();
        config.ThemeId = themeId;
        ConfigService.Сохранить(config);
    }
}