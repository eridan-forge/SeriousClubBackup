using серьёзный.Core.CoreBattleNet;
using серьёзный.Core.CoreEA;
using серьёзный.Core.CoreEpic;
using серьёзный.Core.CoreModels;
using серьёзный.Core.CoreRockstar;
using серьёзный.Core.CoreRiot;
using серьёзный.Core.CoreSteam;
using серьёзный.Core.CoreUbisoft;
using серьёзный.Core.CoreXbox;

namespace серьёзный.Core.CoreLauncher;

public static class LauncherUniversalScanner
{
    public static List<GameInfo> ScanAll()
    {
        var list = new List<GameInfo>();

        list.AddRange(SteamGameImporter.ImportAll());
        list.AddRange(EpicLibraryScanner.Scan());
        list.AddRange(RockstarLibraryScanner.Scan());
        list.AddRange(UbisoftLibraryScanner.Scan());
        list.AddRange(EALibraryScanner.Scan());
        list.AddRange(BattleNetScanner.Scan());
        list.AddRange(RiotScanner.Scan());
        list.AddRange(XboxScanner.Scan());

        return list
            .GroupBy(x => x.Name)
            .Select(x => x.First())
            .ToList();
    }
}