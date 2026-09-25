using System;
using System.Collections.Generic;

namespace серьёзный.Модели
{
    public class CardLayout
    {
        public int PcId { get; set; }

        public List<Guid> Order { get; set; } =
            new();
    }
}