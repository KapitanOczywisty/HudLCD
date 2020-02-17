using System;
using System.Text;
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

//
// Original mod by: Jawastew
// https://steamcommunity.com/sharedfiles/filedetails/?id=911144486
//

using KapitanOczywisty;

namespace KapitanOczywisty.HudLcd
{
  [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), false)]
  sealed class HudLcd : MyGameLogicComponent
  {
    const long WORKSHOP_ID = 1999560983;
    const string MOD = "HudLcd";

    //Static
    public static bool IsInit => hudAPI != null;
    public static bool IsAPIAlive => hudAPI != null && hudAPI.Heartbeat;
    static bool isServer = false;

    static HudAPIv2 hudAPI = null;

    //config
    const int ttl = 10;
    const string matchstring = "hudlcd";
    readonly char[] delim = new char[1] { ':' };
    const double textPosX = -0.98;
    const double textPosY = -0.2;
    const double textScale = 0.8;
    const string textFont = "white";
    const string textFontMonospace = "monospace";
    const bool textBackground = false;
    readonly Color textBackgroundColor = new Color(0f, 0f, 0f, 0.5f);

    readonly Regex regexBackground = new Regex(@"background=(?:([a-z]+)|(\d{1,3},\d{1,3},\d{1,3}))(?:,(\d+))?", RegexOptions.IgnoreCase);

    // Textpanel
    IMyTextPanel thisLcd = null;
    StringBuilder m_msg;
    HudAPIv2.HUDMessage Message;
    HudAPIv2.BillBoardHUDMessage Background;
    Vector2D thistextPosition = new Vector2D(textPosX, textPosY);
    double thistextScale = textScale;
    string thisconfigcolour = string.Empty;
    string thistextFont = textFont;
    bool thistextBackground = textBackground;
    Color thistextBackgroundColor;

    IMyTerminalBlock ControlledEntity => MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity as IMyTerminalBlock;
    bool isControlled => ControlledEntity != null && ControlledEntity.CubeGrid == thisLcd.CubeGrid;
    bool hasHudLcd = false;

    const string stringNil = "\0";
    bool dataDirty = true;
    string titleCache = "";
    string textCache = stringNil;

    // Initializes textAPI once for clients, checks if is Server
    static void Initialize()
    {
      if (IsInit) return;
      if ((MyAPIGateway.Multiplayer.IsServer && MyAPIGateway.Utilities.IsDedicated))
      {
        isServer = true;
        return;
      }
      hudAPI = new HudAPIv2();
    }


    public override void Init(MyObjectBuilder_EntityBase objectBuilder)
    {
      base.Init(objectBuilder);
      thisLcd = Entity as IMyTextPanel;
      thisLcd.CustomDataChanged += (x) => { dataDirty = true; };
      NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
    }

    public override void UpdateOnceBeforeFrame()
    {
      if (!IsInit) Initialize();
      NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
    }

    //for Performance every 10th frame
    public override void UpdateBeforeSimulation10()
    {
      if (isServer) return;
      if (!IsAPIAlive) return;
      if (thisLcd.GetPublicTitle() != titleCache)
      {
        dataDirty = true;
        titleCache = thisLcd.GetPublicTitle();
      }
      // only check if something has changed
      if (dataDirty)
      {
        UpdateValues();
      }
      if (isControlled && hasHudLcd)
      {
        if (textCache != thisLcd.GetText())
          UpdateLCD();
      }
      else
      {
        if (Message != null)
          CleanupHUD();
      }
    }

    private void CleanupHUD()
    {
      if (Background != null)
      {
        Background.TimeToLive = 0;
        Background = null;
      }
      if (Message != null)
      {
        m_msg.Clear();
        Message.TimeToLive = 0;
        Message = null;
      }
      textCache = stringNil;
    }

    private void UpdateValues()
    {
      hasHudLcd = false;
      if (thisLcd.GetPublicTitle().ToLower().Contains(matchstring))
      {
        hasHudLcd = true;
        ParseAndUpdateConfig(thisLcd.GetPublicTitle());
      }
      else if (thisLcd.CustomData.ToLower().Contains(matchstring))
      {
        hasHudLcd = true;
        ParseAndUpdateConfig(thisLcd.CustomData);
      }
      else
      {
        if (Message != null)
          CleanupHUD();
      }
    }

    private void ParseAndUpdateConfig(string data)
    {
      String[] lines = data.Split('\n');
      foreach (String line in lines)
      {
        if (line.ToLower().Contains(matchstring))
        {
          int maxParams = 5;
          String[] rawconf = line.Split(delim, maxParams + 1);
          for (int i = 0; i < maxParams; i++)
          {
            if (rawconf.Length > i && rawconf[i].Length > 0) // Set values from Config Line
            {
              switch (i)
              {
                case 0:
                  break;
                case 1:
                  thistextPosition.X = TryGetDouble(rawconf[i], textPosX);
                  break;
                case 2:
                  thistextPosition.Y = TryGetDouble(rawconf[i], textPosY);
                  break;
                case 3:
                  thistextScale = TryGetDouble(rawconf[i], textScale);
                  break;
                case 4:
                  thisconfigcolour = "<color=" + rawconf[i].Trim() + ">";
                  break;
                default:
                  break;
              }
            }
            else // Set Default Values
            {
              switch (i)
              {
                case 0:
                  break;
                case 1:
                  thistextPosition.X = textPosX;
                  break;
                case 2:
                  thistextPosition.Y = textPosY;
                  break;
                case 3:
                  thistextScale = textScale;
                  break;
                case 4:
                  var fontColor = thisLcd.GetValueColor("FontColor");
                  thisconfigcolour = $"<color={fontColor.R},{fontColor.G},{fontColor.B}>";
                  break;
                default:
                  break;
              }
            }

          }

          // defaults
          thistextFont = textFont;
          thistextBackground = false;
          thistextBackgroundColor = textBackgroundColor;
          if (rawconf.Length > maxParams)
          { // new parameters
            string extra = rawconf[maxParams].ToLower();
            if (extra.Contains("monospace"))
            {
              thistextFont = textFontMonospace;
            }
            if (extra.Contains("background"))
            {
              thistextBackground = true;

              // look for custom color
              var match = regexBackground.Match(extra);
              if (match.Success)
              {
                if (match.Groups[1].Length > 0)
                {
                  thistextBackgroundColor = Colors.GetColor(match.Groups[1].Value);
                }
                else if (match.Groups[2].Length > 0)
                {
                  var cp = match.Groups[2].Value.Split(',');
                  thistextBackgroundColor = new Color(TryGetInt(cp[0], 0), TryGetInt(cp[1], 0), TryGetInt(cp[2], 0));
                }

                if (match.Groups[3].Length > 0)
                {
                  thistextBackgroundColor = thistextBackgroundColor * (TryGetInt(match.Groups[3].Value, 127) / 255f);
                }
                else
                {
                  thistextBackgroundColor = thistextBackgroundColor * 0.5f;
                }
              }
            }
          }

          if (Message != null)
          {
            Message.Origin = thistextPosition;
            Message.Scale = thistextScale;
            Message.Font = thistextFont;
          }
          if (Background != null)
          {
            Background.Origin = thistextPosition;
            Background.Visible = thistextBackground;
            Background.BillBoardColor = thistextBackgroundColor;
          }
          break;
        }

      }

      textCache = stringNil;
    }

    private double TryGetDouble(string v, double defaultval)
    {
      try
      {
        return double.Parse(v);
      }
      catch (Exception)
      {
        return defaultval;
      }
    }

    private int TryGetInt(string v, int defaultval)
    {
      try
      {
        return int.Parse(v);
      }
      catch (Exception)
      {
        return defaultval;
      }
    }

    private void UpdateLCD()
    {
      if (m_msg == null)
      {
        m_msg = new StringBuilder();
      }
      if (Background == null)
      {
        Background = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("SquareIgnoreDepth"),
                                                      thistextPosition,
                                                      thistextBackgroundColor,
                                                      Scale: 1.05, // .05 is for padding
                                                      Blend: BlendTypeEnum.PostPP);
      }
      if (Message == null)
      {
        Message = new HudAPIv2.HUDMessage(m_msg, thistextPosition, Scale: thistextScale, Blend: BlendTypeEnum.PostPP, Font: thistextFont);
      }
      textCache = thisLcd.GetText();
      m_msg.Clear();
      m_msg.Append(thisconfigcolour);
      m_msg.Append(textCache);
      Background.Visible = thistextBackground;
      if (thistextBackground)
      {
        var ln = Message.GetTextLength();
        Background.Offset = ln / 2d;
        Background.Width = (float)ln.X;
        Background.Height = (float)ln.Y;
      }
    }

    public override void Close()
    {
      CleanupHUD();
    }
  }
}
