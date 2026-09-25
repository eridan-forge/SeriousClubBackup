using System;

namespace серьёзный.Модели
{
    public enum РежимВремени
    {
        ТолькоПокупное,
        ТолькоБаланс,
        БалансПлюсПокупное
    }

    public enum СтатусСеанса
    {
        Ожидает,
        Активен,
        НаПаузе,
        Завершён,
        Отменён
    }

    public class Сеанс
    {
        public int Id { get; set; }

        public РежимВремени Режим { get; set; } =
            РежимВремени.ТолькоПокупное;

        public int КомпьютерId { get; set; }

        public int? КлиентId { get; set; }

        public string ИмяКлиента { get; set; } =
            string.Empty;

        public DateTime Начало { get; set; }

        public DateTime ЗапланированноеОкончание { get; set; }

        public DateTime? ФактическоеОкончание { get; set; }

        public decimal Стоимость { get; set; }

        public DateTime? НачалоПаузы { get; set; }

        public TimeSpan ВремяПаузы { get; set; } =
            TimeSpan.Zero;

        public TimeSpan ИсходноеВремяАккаунта { get; set; } =
            TimeSpan.Zero;

        public СтатусСеанса Статус { get; set; }

        public bool Предупреждение15МинутОтправлено { get; set; }

        public bool Предупреждение10МинутОтправлено { get; set; }

        public bool Предупреждение5МинутОтправлено { get; set; }

        public bool КомандаЗавершенияОтправлена { get; set; }

        public Guid? АккаунтGuid { get; set; }

        public TimeSpan ВремяАккаунта { get; set; } =
            TimeSpan.Zero;

        public TimeSpan КупленноеВремя { get; set; } =
            TimeSpan.Zero;

        public bool ИспользуетсяОстатокАккаунта { get; set; }

        internal DateTime ПоследнееОбновление { get; set; } =
            DateTime.Now;

        public Сеанс()
        {
            Статус =
                СтатусСеанса.Ожидает;
        }

        public TimeSpan ОсталосьВремени
        {
            get
            {
                if (Статус == СтатусСеанса.НаПаузе &&
                    НачалоПаузы.HasValue)
                {
                    var осталосьВоВремяПаузы =
                        ЗапланированноеОкончание -
                        НачалоПаузы.Value;

                    return осталосьВоВремяПаузы > TimeSpan.Zero
                        ? осталосьВоВремяПаузы
                        : TimeSpan.Zero;
                }

                if (Статус != СтатусСеанса.Активен &&
                    Статус != СтатусСеанса.НаПаузе)
                {
                    return TimeSpan.Zero;
                }

                var осталось =
                    ЗапланированноеОкончание -
                    DateTime.Now;

                return осталось > TimeSpan.Zero
                    ? осталось
                    : TimeSpan.Zero;
            }
        }

        public bool ВремяИстекло =>
            Статус == СтатусСеанса.Активен &&
            DateTime.Now >= ЗапланированноеОкончание;

        public void Пауза()
        {
            if (Статус != СтатусСеанса.Активен)
                return;

            НачалоПаузы =
                DateTime.Now;

            ПоследнееОбновление =
                DateTime.Now;

            Статус =
                СтатусСеанса.НаПаузе;
        }

        public void Продолжить()
        {
            if (Статус != СтатусСеанса.НаПаузе ||
                !НачалоПаузы.HasValue)
            {
                return;
            }

            var прошло =
                DateTime.Now -
                НачалоПаузы.Value;

            if (прошло > TimeSpan.Zero)
            {
                ВремяПаузы +=
                    прошло;

                ЗапланированноеОкончание +=
                    прошло;
            }

            НачалоПаузы = null;

            ПоследнееОбновление =
                DateTime.Now;

            Статус =
                СтатусСеанса.Активен;
        }

        public void ДобавитьВремя(
            TimeSpan время)
        {
            if (время <= TimeSpan.Zero)
                return;

            ЗапланированноеОкончание +=
                время;
        }

        public void УбавитьВремя(
            TimeSpan время)
        {
            if (время <= TimeSpan.Zero)
                return;

            ЗапланированноеОкончание -=
                время;
        }

        public void Завершить()
        {
            ФактическоеОкончание =
                DateTime.Now;

            НачалоПаузы = null;

            Статус =
                СтатусСеанса.Завершён;
        }

        public void Отменить()
        {
            ФактическоеОкончание =
                DateTime.Now;

            НачалоПаузы = null;

            Статус =
                СтатусСеанса.Отменён;
        }

        public void УстановитьТолькоПокупное()
        {
            Режим =
                РежимВремени.ТолькоПокупное;

            ИспользуетсяОстатокАккаунта =
                false;

            ВремяАккаунта =
                TimeSpan.Zero;

            ИсходноеВремяАккаунта =
                TimeSpan.Zero;
        }

        public void УстановитьТолькоБаланс(
            TimeSpan остаток)
        {
            Режим =
                РежимВремени.ТолькоБаланс;

            ИспользуетсяОстатокАккаунта =
                true;

            ИсходноеВремяАккаунта =
                остаток;

            ВремяАккаунта =
                остаток;

            КупленноеВремя =
                TimeSpan.Zero;
        }

        public void УстановитьБалансПлюсПокупное(
            TimeSpan остаток)
        {
            Режим =
                РежимВремени.БалансПлюсПокупное;

            ИспользуетсяОстатокАккаунта =
                true;

            ИсходноеВремяАккаунта =
                остаток;

            ВремяАккаунта =
                остаток;
        }

        public void ПересчитатьОкончание()
        {
            var сейчас =
                DateTime.Now;

            ЗапланированноеОкончание =
                сейчас +
                ВремяАккаунта +
                КупленноеВремя;

            ПоследнееОбновление =
                сейчас;
        }
    }
}
