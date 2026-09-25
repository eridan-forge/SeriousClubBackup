using System;
using System.Threading;

namespace серьёзный.Сервисы
{
    public class СервисФона008 : IDisposable
    {
        private readonly Timer таймер;

        private readonly Action действие;

        public СервисФона008(Action действие)
        {
            this.действие = действие;

            таймер = new Timer(
                _ => this.действие(),
                null,
                Timeout.Infinite,
                Timeout.Infinite);
        }

        public void Запустить()
        {
            таймер.Change(1000, 1000);
        }

        public void Остановить()
        {
            таймер.Change(
                Timeout.Infinite,
                Timeout.Infinite);
        }

        public void Dispose()
        {
            таймер.Dispose();
        }
    }
}