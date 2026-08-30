namespace серьёзный.Сервисы.ОпределениеИгр
{
    public static class EpicResolver
    {
        public static string BuildLaunchUri(string appId)
        {
            return $"com.epicgames.launcher://apps/{appId}?action=launch";
        }
    }
}