using System;
using System.Collections.Generic;

namespace серьёзный.ЭкранКлуба.Модели
{
    public class НастройкиИгрока
    {
        public Guid АккаунтId { get; set; }

        public List<Guid> Избранное { get; set; } = new();

        public List<Guid> ПоследниеИгры { get; set; } = new();
    }
}