using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
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

// PB is not implementing Sandbox.ModAPI.IMyTextSurfaceProvider - weird
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
using KapitanOczywisty;

namespace KapitanOczywisty.HudLcd
{
  class BlockBase : MyGameLogicComponent
  {
    protected bool init = false;
    protected bool dataDirty = true;

    protected IMyTerminalBlock block;

    // TextPanel also have IMyTextSurfaceProvider
    protected bool HasSurfaces => block is IMyTextSurfaceProvider;
    protected int SurfaceCount => (block as IMyTextSurfaceProvider).SurfaceCount;
    protected IMyTextSurface GetSurface(int index) => (block as IMyTextSurfaceProvider).GetSurface(index) as IMyTextSurface;

    Dictionary<int, HudSurface> MySurfaces;
    List<int> FoundHudSurfaces;

    const string HudKeyword = "hudlcd";
    // do not allow space "hudlcd 1" with space -  [\t ]*
    // ungreedy modifier only with RightToLeft!
    static readonly Regex regexInstance = new Regex(@"(?xi)
      # keyword and display
      " + HudKeyword + @" (\d+ (?![*.]) )?
      # configuration
      ([\S\t ]*?)", RegexOptions.Compiled | RegexOptions.RightToLeft);
    static readonly Regex regexNoNewLine = new Regex(@"\\\r\n|\\\n|\\\r", RegexOptions.Compiled);

    public override void Init(MyObjectBuilder_EntityBase objectBuilder)
    {
      base.Init(objectBuilder);

      block = Entity as IMyTerminalBlock;

      if (!HudRenderer.CanRun || !HasSurfaces) return;

      init = true;
      // <surfaceId, surface>
      MySurfaces = new Dictionary<int, HudSurface>();
      FoundHudSurfaces = new List<int>();

      dataDirty = true;
      block.CustomDataChanged += MakeDataDirty;
      block.CustomNameChanged += MakeDataDirty;
      NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
      NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
    }

    protected void MakeDataDirty(IMyTerminalBlock _)
    {
      dataDirty = true;
      NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
    }

    public override void UpdateAfterSimulation10()
    {
      if (!init) return;

      NeedsUpdate = MyEntityUpdateEnum.NONE;

      if (!dataDirty) return;

      FoundHudSurfaces.Clear();
      if (block.CustomData.IndexOf(HudKeyword, StringComparison.OrdinalIgnoreCase) != -1)
      {
        ParseConfig(regexNoNewLine.Replace(block.CustomData, ""));
      }
      // name has higher priority
      if (block.CustomName.IndexOf(HudKeyword, StringComparison.OrdinalIgnoreCase) != -1)
      {
        ParseConfig(block.CustomName);
      }

      foreach (var key in MySurfaces.Keys.ToArray().Where(key => !FoundHudSurfaces.Contains(key)))
      {
        MySurfaces[key].Close();
        MySurfaces.Remove(key);
      }
    }

    private void ParseConfig(string data)
    {
      var matches = regexInstance.Matches(data);
      // RightToLeft so reverse order
      for (var i = matches.Count - 1; i > -1; --i)
      // foreach (Match match in matches)
      {
        GroupCollection groups = matches[i].Groups;

        int index = Utils.TryGetInt(groups[1].Value, 0);
        // skip invalid indexes
        if (index < 0 || index >= SurfaceCount) continue;

        FoundHudSurfaces.Add(index);

        if (MySurfaces.ContainsKey(index))
        {
          if (MySurfaces[index].IsActive)
          {
            MySurfaces[index].ApplyConfig(groups[2].Value);
            continue;
          }
          else
            MySurfaces.Remove(index);
        }

        MySurfaces.Add(index, new HudSurface(
          GetSurface(index), block, groups[2].Value, index
        ));
      }
    }

    public override void Close()
    {
      base.Close();

      if (!init) return;
      init = false;
      block.CustomDataChanged -= MakeDataDirty;
      block.CustomNameChanged -= MakeDataDirty;
      NeedsUpdate = MyEntityUpdateEnum.NONE;
      block = null;

      foreach (KeyValuePair<int, HudSurface> surface in MySurfaces)
      {
        surface.Value.Close();
      }
      MySurfaces.Clear();
    }
  }
}
