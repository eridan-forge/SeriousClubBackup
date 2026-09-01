using System;
using System.Linq;
using серьёзный.Core.CoreAudit;
using серьёзный.Core.CoreEconomy;

namespace серьёзный.Сервисы
{
    public class СервисСгоранияВремени
    {
        private readonly СервисАккаунтов аккаунты = new();

        private readonly PremiumService premium = new();

        private readonly AdminActionLogService лог = new();

        public int СжечьВсем(string? имяАдмина = null)
        {
            int сожжено = 0;

            foreach (var акк in аккаунты.ПолучитьВсе())
            {
                if (акк.ОсталосьВремени <= TimeSpan.Zero)
                    continue;

                // Премиум-аккаунтам время не сгорает.
                if (premium.IsPremium(акк.Id))
                    continue;

                аккаунты.УстановитьОстаток(акк.Id, TimeSpan.Zero);

                сожжено++;
            }

            лог.Log(
                "Сгорание времени",
                $"Обнулено аккаунтов: {сожжено} (премиум-аккаунты не тронуты)",
                имяАдмина);

            return сожжено;
        }
    }
}