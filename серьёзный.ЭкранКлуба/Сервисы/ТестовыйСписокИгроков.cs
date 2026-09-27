using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using серьёзный.Core.CoreModels;

namespace серьёзный.ЭкранКлуба.Сервисы;

// Тестовые игроки клуба для тестового аккаунта (ТестовыйАккаунт) —
// работает в обход сервера/Патруля, по тому же принципу, что
// ТестовыйКаталогИгр/ТестовыйКаталогМагазина.
public static class ТестовыйСписокИгроков
{
    private static readonly (string Имя, bool Онлайн, int Пк, string? Игра, bool Друг, bool Заявка)[] Заготовки =
    {
        ("Алексей Смирнов", true, 3, "Counter-Strike 2", true, false),
        ("Мария Кузнецова", true, 1, "Dota 2", false, false),
        ("Дмитрий Волков", false, 0, null, true, false),
        ("Анна Соколова", true, 5, "Valorant", false, true),
        ("Иван Попов", false, 0, null, false, false),
        ("Екатерина Лебедева", true, 2, "В клубе", false, false),
        ("Сергей Новиков", true, 4, "Apex Legends", true, false),
        ("Ольга Морозова", false, 0, null, false, true),
        ("Павел Зайцев", true, 3, "Fortnite", false, false),
        ("Наталья Фёдорова", false, 0, null, false, false),
        ("Артём Егоров", true, 1, "League of Legends", false, false),
        ("Виктория Козлова", true, 5, "В клубе", true, false)
    };

    public static SocialStateDto Сгенерировать()
    {
        var players = new List<OnlinePlayerDto>();
        var incoming = new List<IncomingFriendRequestDto>();

        foreach (var (имя, онлайн, пк, игра, друг, заявка) in Заготовки)
        {
            var id = СтабильныйId(имя);

            players.Add(new OnlinePlayerDto
            {
                AccountId = id,
                FullName = имя,
                Online = онлайн,
                PcId = онлайн ? пк : 0,
                CurrentGame = онлайн ? игра : null,
                IsFriend = друг,
                HasPendingOutgoing = false
            });

            if (заявка)
            {
                incoming.Add(new IncomingFriendRequestDto
                {
                    RequestId = Guid.NewGuid(),
                    FromAccountId = id,
                    FromFullName = имя
                });
            }
        }

        return new SocialStateDto
        {
            Players = players,
            Incoming = incoming
        };
    }

    private static Guid СтабильныйId(string значение)
    {
        using var md5 = MD5.Create();
        var хеш = md5.ComputeHash(Encoding.UTF8.GetBytes("test-player-" + значение));
        return new Guid(хеш);
    }
}