using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using серьёзный.Core.CoreModels;

namespace серьёзный.Core.CoreWeather;

// Единственный во всём проекте класс, который ходит в интернет за
// погодой. Основной пользователь — СЕРВЕР (админский ПК): клиентские
// экраны спрашивают погоду у него по локальной сети. Экран клуба
// вызывает этот класс напрямую ТОЛЬКО когда сервер недоступен, и
// тогда просит кэш на час вместо получаса.
public static class СервисПогодыСервера
{
    // Один статический HttpClient на весь процесс: новый клиент на
    // каждый запрос со временем исчерпывает сокеты, а тут он не нужен.
    private static readonly HttpClient http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    // Пять ПК могут спросить погоду в одну и ту же секунду — замок
    // гарантирует, что наружу уйдёт РОВНО один запрос, а остальные
    // четыре дождутся его результата.
    private static readonly SemaphoreSlim замок = new(1, 1);

    // с. Вилино, Бахчисарайский район, Крым.
    private const double Широта = 44.8449;
    private const double Долгота = 33.6562;

    private static readonly TimeSpan ЖизньКэшаПоУмолчанию =
        TimeSpan.FromMinutes(30);

    // Если метеослужба недоступна — не пытаемся чаще, чем раз в
    // 5 минут, даже если экраны продолжают спрашивать.
    private static readonly TimeSpan ПаузаПослеОшибки =
        TimeSpan.FromMinutes(5);

    private static WeatherDto? кэш;
    private static DateTime времяКэша = DateTime.MinValue;
    private static DateTime времяПопытки = DateTime.MinValue;

    // Чтобы лог не разрастался при суточном отсутствии интернета,
    // пишем только момент перехода «было ок → стало не ок» и обратно.
    private static bool предыдущаяПопыткаУспешна = true;

    public static async Task<WeatherDto> ПолучитьAsync(TimeSpan? жизньКэша = null)
    {
        var срок = жизньКэша ?? ЖизньКэшаПоУмолчанию;

        var текущий = кэш;

        if (текущий != null && DateTime.Now - времяКэша < срок)
            return текущий;

        await замок.WaitAsync();

        try
        {
            // Пока ждали замок, соседний запрос мог уже всё обновить.
            if (кэш != null && DateTime.Now - времяКэша < срок)
                return кэш;

            if (кэш != null && DateTime.Now - времяПопытки < ПаузаПослеОшибки)
                return кэш;

            времяПопытки = DateTime.Now;

            var свежий = await ЗапроситьAsync();

            if (свежий != null)
            {
                кэш = свежий;
                времяКэша = DateTime.Now;

                if (!предыдущаяПопыткаУспешна)
                {
                    ЗаписатьЛог("связь с метеослужбой восстановлена");
                    предыдущаяПопыткаУспешна = true;
                }

                return свежий;
            }

            // Сеть моргнула — отдаём последнее известное значение,
            // чтобы экран не мигал в «нет данных» и обратно.
            return кэш ?? new WeatherDto { Успешно = false };
        }
        finally
        {
            замок.Release();
        }
    }

    private static async Task<WeatherDto?> ЗапроситьAsync()
    {
        try
        {
            var url =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={Широта.ToString(CultureInfo.InvariantCulture)}" +
                $"&longitude={Долгота.ToString(CultureInfo.InvariantCulture)}" +
                "&current=temperature_2m,weather_code,is_day" +
                "&timezone=auto";

            var json = await http.GetStringAsync(url);

            using var doc = JsonDocument.Parse(json);

            var текущее = doc.RootElement.GetProperty("current");

            return new WeatherDto
            {
                Успешно = true,
                Температура = текущее.GetProperty("temperature_2m").GetDouble(),
                Код = текущее.GetProperty("weather_code").GetInt32(),
                День = текущее.GetProperty("is_day").GetInt32() == 1,
                Обновлено = DateTime.Now
            };
        }
        catch (Exception ошибка)
        {
            if (предыдущаяПопыткаУспешна)
            {
                ЗаписатьЛог("недоступна: " + ошибка.Message);
                предыдущаяПопыткаУспешна = false;
            }

            return null;
        }
    }

    private static void ЗаписатьЛог(string текст)
    {
        try
        {
            var папка = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SeriousClub", "logs");

            Directory.CreateDirectory(папка);

            File.AppendAllText(
                Path.Combine(папка, "weather.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Погода {текст}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}