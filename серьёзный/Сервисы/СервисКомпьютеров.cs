using System;
using System.Collections.Generic;
using System.Linq;
using серьёзный.Модели;

namespace серьёзный.Сервисы
{
    public class СервисКомпьютеров
    {
        private readonly List<ИгровойКомпьютер> компьютеры;

        public СервисКомпьютеров()
        {
            компьютеры = new List<ИгровойКомпьютер>
            {
                СоздатьКомпьютер(
                    1,
                    "PC-01",
                    "DESKTOP-IN5G5T1",
                    "192.168.31.197",
                    "34:5A:60:F4:E5:29"),

                СоздатьКомпьютер(
                    2,
                    "PC-02",
                    "DESKTOP-E079RMC",
                    "192.168.31.55",
                    "FC:9D:05:66:31:35"),

                СоздатьКомпьютер(
                    3,
                    "PC-03",
                    "DESKTOP-BOAJUJV",
                    "192.168.31.150",
                    "34:5A:60:F4:E5:F4"),

                СоздатьКомпьютер(
                    4,
                    "PC-04",
                    "DESKTOP-5S1UI1G",
                    "192.168.31.204",
                    "34:5A:60:F4:E5:30"),

                СоздатьКомпьютер(
                    5,
                    "PC-05",
                    "DESKTOP-TB208IO",
                    "192.168.31.147",
                    "34:5A:60:F4:E5:F1")
            };
        }

        public IReadOnlyList<ИгровойКомпьютер> ПолучитьВсе()
        {
            return компьютеры;
        }

        public ИгровойКомпьютер? ПолучитьПоId(int id)
        {
            return компьютеры.FirstOrDefault(x => x.Id == id);
        }

        public ИгровойКомпьютер? ПолучитьПоИмени(string имя)
        {
            return компьютеры.FirstOrDefault(
                x => string.Equals(
                    x.Имя,
                    имя,
                    StringComparison.OrdinalIgnoreCase));
        }

        public void ОбновитьСостояние(
            int id,
            СостояниеКомпьютера состояние,
            bool наСвязи,
            bool включен)
        {
            var компьютер = ПолучитьПоId(id);

            if (компьютер == null)
                return;

            компьютер.Состояние = состояние;
            компьютер.НаСвязи = наСвязи;
            компьютер.Включен = включен;
            компьютер.ПоследнийКонтакт = DateTime.Now;
        }

        public void ОбновитьПоследнийКонтакт(int id)
        {
            var компьютер = ПолучитьПоId(id);

            if (компьютер == null)
                return;

            компьютер.НаСвязи = true;
            компьютер.ПоследнийКонтакт = DateTime.Now;
        }

        private static ИгровойКомпьютер СоздатьКомпьютер(
            int id,
            string имя,
            string имяWindows,
            string ip,
            string mac)
        {
            return new ИгровойКомпьютер
            {
                Id = id,
                Имя = имя,
                ИмяWindows = имяWindows,
                IPАдрес = ip,
                MACАдрес = mac,
                Включен = false,
                НаСвязи = false,
                Состояние = СостояниеКомпьютера.Неизвестно
            };
        }
    }
}