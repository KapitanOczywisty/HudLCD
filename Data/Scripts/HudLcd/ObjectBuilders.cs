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

namespace KapitanOczywisty.HudLcd
{
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false)]
  sealed class TextPanelHud : BlockBase
  { }
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MyProgrammableBlock), false)]
  sealed class ProgramableBlockHud : BlockBase
  { }
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Cockpit), false)]
  sealed class CockpitHud : BlockBase
  { }
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CryoChamber), false)]
  sealed class CryoChamberHud : BlockBase
  { }
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_MedicalRoom), false)]
  sealed class MedicalRoomHud : BlockBase
  { }
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SurvivalKit), false)]
  sealed class SurvivalKit : BlockBase
  { }
}
