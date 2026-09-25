namespace серьёзный.Патруль.Модели
{
    public class КонфигурацияПатруля
    {
        public int КомпьютерId { get; set; }

        public string ИмяКомпьютера { get; set; } =
            string.Empty;

        public string СерверIP { get; set; } =
            string.Empty;

        public int СерверПорт { get; set; }

        public int ТаймаутПодключенияСекунд { get; set; } = 5;

        public int ИнтервалHeartbeatСекунд { get; set; } = 5;
    }
}