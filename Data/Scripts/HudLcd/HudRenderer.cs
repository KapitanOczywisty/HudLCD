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

using Draygo.API;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace KapitanOczywisty.HudLcd
{
  [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
  sealed class HudRenderer : MySessionComponentBase
  {
    public static HudRenderer Instance = null;
    public static bool CanRun => Instance != null && Instance.init && !Instance.isServer;

    HudAPIv2 HudAPI = null;
    bool init = false;
    bool running = false;
    bool isServer = false;

    public bool IsAPIAlive => HudAPI != null && HudAPI.Heartbeat;

    List<HudSurface> RegisteredSurfaces = new List<HudSurface>();



    public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
    {
      if (init) return;
      base.Init(sessionComponent);

      Instance = this;
      HudAPI = new HudAPIv2();

      init = true;
      running = true;
      isServer = (MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated);
      if (!isServer)
        SetUpdateOrder(MyUpdateOrder.AfterSimulation);
    }

    public void Close()
    {
      if (init && !isServer)
        init = false;
      if (HudAPI != null)
        HudAPI.Close();

      running = false;
      isServer = false;

      foreach (var surface in RegisteredSurfaces)
        surface.Close(false);
      RegisteredSurfaces.Clear();
      SetUpdateOrder(MyUpdateOrder.NoUpdate);
    }

    private int _updateCounter = 0;
    const int _updateInterval = 10;

    public override void UpdateAfterSimulation()
    {
      if (!CanRun || !IsAPIAlive) return;
      if (RegisteredSurfaces.Count == 0) return;

      _updateCounter++;
      _updateCounter %= _updateInterval;
      // if (_updateCounter != 0) return;

      for (int i = _updateCounter; i < RegisteredSurfaces.Count; i += _updateInterval)
      {
        RegisteredSurfaces[i].Update();
      }
    }

    public void RegisterSurface(HudSurface surface)
    {
      if (RegisteredSurfaces.Contains(surface))
      {
        MyLog.Default.Warning("HUDLCD: Double registration attempt!");
        return;
      }
      RegisteredSurfaces.Add(surface);
    }

    public void UnregisterSurface(HudSurface surface)
    {
      RegisteredSurfaces.Remove(surface);
    }


  }
}
