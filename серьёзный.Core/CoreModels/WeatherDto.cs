namespace серьёзный.Core.CoreModels;

public class WeatherDto
{
    public bool Успешно { get; set; }

    public double Температура { get; set; }

    // WMO weather code (0 — ясно, 3 — пасмурно, 61 — дождь и т.д.)
    public int Код { get; set; }

    public bool День { get; set; }

    // Когда сервер реально получил эти данные от метеослужбы.
    public DateTime Обновлено { get; set; }
}