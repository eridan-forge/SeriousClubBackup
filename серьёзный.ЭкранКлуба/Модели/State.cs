using System;

namespace серьёзный.ЭкранКлуба.Модели
{
    public class State
    {
        public bool Locked { get; set; } = true;

        public int PcId { get; set; } = 1;

        public Guid? AccountId { get; set; }
    }
}