using System;
using System.Collections.Generic;
using System.Linq;

namespace серьёзный.Сервисы
{
    public class СервисИстории
    {
        private readonly List<ЗаписьИстории> записи = new();

        private long следующийId = 1;

        public IReadOnlyList<ЗаписьИстории> ПолучитьВсе()
        {
            return записи
                .OrderByDescending(x => x.Дата)
                .ToList();
        }

        public void Добавить(
            string событие,
            string описание,
            int? компьютерId = null,
            int? сеансId = null)
        {
            записи.Add(new ЗаписьИстории
            {
                Id = следующийId++,
                Дата = DateTime.Now,
                Событие = событие,
                Описание = описание,
                КомпьютерId = компьютерId,
                СеансId = сеансId
            });
        }
    }

    public class ЗаписьИстории
    {
        public long Id { get; set; }

        public DateTime Дата { get; set; }

        public string Событие { get; set; } = string.Empty;

        public string Описание { get; set; } = string.Empty;

        public int? КомпьютерId { get; set; }

        public int? СеансId { get; set; }
    }
}