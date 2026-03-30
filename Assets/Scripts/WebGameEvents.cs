using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGameEvents
{
    public const int WinCoinThreshold = 200;

    private static bool winSent;
    private static bool loseSent;

    public static void ResetRun()
    {
        winSent = false;
        loseSent = false;
    }

    public static void TrySendWinForCoins(int previousCoins, int currentCoins)
    {
        if (winSent)
        {
            return;
        }

        if (previousCoins >= WinCoinThreshold)
        {
            return;
        }

        if (currentCoins < WinCoinThreshold)
        {
            return;
        }

        SendGameWin();
    }

    public static void SendGameWin()
    {
        if (winSent)
        {
            return;
        }

        winSent = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        gameWin();
#else
        Debug.Log("WebGameEvents: gameWin()");
#endif
    }

    public static void SendGameLose()
    {
        if (loseSent)
        {
            return;
        }

        loseSent = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        gameLose();
#else
        Debug.Log("WebGameEvents: gameLose()");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void gameWin();

    [DllImport("__Internal")]
    private static extern void gameLose();
#endif
}
