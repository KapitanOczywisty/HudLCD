using System;
using System.Text;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using System.Text.RegularExpressions;

namespace KapitanOczywisty
{
  static class Utils
  {
    public static int TryGetInt(string v, int defaultval)
    {
      try { return int.Parse(v); }
      catch (Exception) { return defaultval; }
    }
    public static double TryGetDouble(string v, double defaultval)
    {
      try { return double.Parse(v); }
      catch (Exception) { return defaultval; }
    }

    public static void LogWarning(string text)
    {
      MyLog.Default.Warning(text);
    }

    static int WarningCounter = 0;
    public static void ShowWarning(string text, int timeoutMs = 3000)
    {
      MyAPIGateway.Utilities.ShowNotification(++WarningCounter + text, timeoutMs, MyFontEnum.Red);
    }
  }
}
