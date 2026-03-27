using System.Runtime.InteropServices;

public static class WebSessionStorage
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SS_SetInt(string key, int value);

    [DllImport("__Internal")]
    private static extern int SS_GetInt(string key, int defaultValue);

    [DllImport("__Internal")]
    private static extern int SS_HasItem(string key);

    [DllImport("__Internal")]
    private static extern void SS_RemoveItem(string key);
#endif

    public static void SetInt(string key, int value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SS_SetInt(key, value);
#endif
    }

    public static bool TryGetInt(string key, out int value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (SS_HasItem(key) == 0)
        {
            value = 0;
            return false;
        }

        value = SS_GetInt(key, 0);
        return true;
#else
        value = 0;
        return false;
#endif
    }

    public static void Remove(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SS_RemoveItem(key);
#endif
    }
}
