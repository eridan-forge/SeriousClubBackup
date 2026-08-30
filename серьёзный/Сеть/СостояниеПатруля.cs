using System;

namespace серьёзный.Сеть
{
    public class СостояниеПатруля
    {
        public int КомпьютерId { get; set; }

        public string ИмяКомпьютера { get; set; } = string.Empty;

        public string ИмяWindows { get; set; } = string.Empty;

        public string IPАдрес { get; set; } = string.Empty;

        public string MACАдрес { get; set; } = string.Empty;

        public bool WindowsЗапущен { get; set; }

        public bool ПользовательЗаблокирован { get; set; }

        public bool СеансАктивен { get; set; }

        public int? СеансId { get; set; }

        public DateTime ВремяСостояния { get; set; }

        public string ВерсияПатруля { get; set; } = string.Empty;
    }
}