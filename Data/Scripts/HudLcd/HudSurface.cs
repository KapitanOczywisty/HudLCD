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

using KapitanOczywisty;

namespace KapitanOczywisty.HudLcd
{
  [Flags]
  public enum RestrictionEnum
  {
    None = 0,
    Camera = 1,
    First = 2,
    Third = 4,
    Any = 7,
  }

  sealed class HudSurface
  {
    bool active = true;
    public bool IsActive => active;

    readonly IMyTextSurface surface;
    // for grid check
    readonly IMyTerminalBlock block;
    readonly int index = 0;

    HudAPIv2.HUDMessage Message = null;
    HudAPIv2.BillBoardHUDMessage Background = null;
    StringBuilder text = new StringBuilder();
    string textCache = defaultTextCache;

    // default values
    const double defaultPosX = -0.98;
    const double defaultPosY = -0.2;
    const double defaultScale = 0.8;
    const string defaultColor = "";
    const string defaultFont = "white";
    const string monospaceFont = "monospace";
    readonly static Color defaultBackgroundColor = new Color(0f, 0f, 0f, 0.5f);
    const int defaultBackgroundOpacity = 127;
    const int defaultRange = 100;
    const RestrictionEnum defaultRestrictions = RestrictionEnum.Any;
    const string defaultTextCache = "\0";

    // properties
    double PosX;
    double PosY;
    Vector2D Position;
    double Scale;
    string Color;
    string ColorObject;
    string Font;
    bool HasBackground;
    string BackgroundColor;
    int BackgroundOpacity;
    Color BackgroundColorObject;
    bool HasRange;
    int Range;
    bool NoHideHud;
    RestrictionEnum Restrictions = RestrictionEnum.None;

    bool needsUpdate = false;

    public HudSurface(IMyTextSurface _surface, IMyTerminalBlock _block, string config, int _index = 0)
    {
      surface = _surface;
      block = _block;
      index = _index;

      // Utils.LogWarning($"sf {_block is Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider} ind {_index} config {config}");

      ApplyConfig(config);

      HudRenderer.Instance.RegisterSurface(this);
    }

    ~HudSurface()
    {
      if (active) Close();
    }

    public void Close(bool deregister = true)
    {
      active = false;
      text.Clear();
      textCache = defaultTextCache;
      if (Message != null)
      {
        Message.TimeToLive = 0;
        Message = null;
      }
      if (Background != null)
      {
        Background.TimeToLive = 0;
        Background = null;
      }

      if (deregister)
        HudRenderer.Instance.UnregisterSurface(this);
    }

    // Render

    IMyTerminalBlock ControlledEntity => MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity as IMyTerminalBlock;
    float PlayerDistance => Vector3.Distance(block.GetPosition(), MyAPIGateway.Session.Player.GetPosition());
    bool IsInFirstPersonView => MyAPIGateway.Session.CameraController.IsInFirstPersonView;
    bool IsInCamera => MyAPIGateway.Session.CameraController.Entity is IMyCameraBlock
      || MyAPIGateway.Session.CameraController.Entity is IMyUserControllableGun;
    bool IsInDesiredRange()
    {
      if (block.OwnerId == 0L) return false;
      if (block.OwnerId == MyAPIGateway.Session.Player.IdentityId)
        return PlayerDistance <= Range;
      return false;
      // for now only owner can see
      // return block.GetPlayerRelationToOwner() == MyRelationsBetweenPlayerAndBlock.FactionShare && PlayerDistance <= Range;
    }
    bool IsOnFoot => ControlledEntity == null;
    bool IsSameGrid => ControlledEntity != null && ControlledEntity.CubeGrid == block.CubeGrid;

    public void Update()
    {
      if (!active) return;

      bool showHud = false;

      if (HasRange && IsOnFoot && IsInDesiredRange())
      {
        showHud = true;
      }
      else if (!HasRange && IsSameGrid)
      {
        if (Restrictions == RestrictionEnum.Any
          || (Restrictions == RestrictionEnum.Camera && IsInCamera)
          || (Restrictions == RestrictionEnum.First && IsInFirstPersonView)
          || (Restrictions == RestrictionEnum.Third && !IsInFirstPersonView))
        {
          showHud = true;
        }
      }

      if (showHud)
      {
        if (textCache != surface.GetText() || needsUpdate)
          UpdateLCD();
      }
      else
      {
        if (Message != null)
          CleanupHUD();
      }
    }

    private void UpdateLCD()
    {
      if (Background == null)
      {
        Background = new HudAPIv2.BillBoardHUDMessage(
          MyStringId.GetOrCompute("SquareIgnoreDepth"),
          Position,
          BackgroundColorObject,
          Scale: 1.05, // .05 is for padding
          HideHud: !NoHideHud,
          Blend: BlendTypeEnum.PostPP);
      }
      if (Message == null)
      {
        Message = new HudAPIv2.HUDMessage(
          text,
          Position,
          Scale: Scale,
          HideHud: !NoHideHud,
          Blend: BlendTypeEnum.PostPP,
          Font: Font);
      }

      needsUpdate = false;
      textCache = surface.GetText();
      text.Clear();
      text.Append(ColorObject);
      text.Append(textCache);
      Background.Visible = HasBackground;
      if (HasBackground)
      {
        var ln = Message.GetTextLength();
        Background.Offset = ln / 2d;
        Background.Width = (float)ln.X;
        Background.Height = (float)ln.Y;
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
        text.Clear();
        Message.TimeToLive = 0;
        Message = null;
      }
      textCache = defaultTextCache;
      needsUpdate = false;
    }

    // Param setters

    public void SetPosition(double _PosX, double _PosY)
    {
      if (PosX == _PosX && PosY == _PosY) return;
      needsUpdate = true;

      PosX = _PosX;
      PosY = _PosY;
      Position = new Vector2D(PosX, PosY);
      if (Message != null) Message.Origin = Position;
      if (Background != null) Background.Origin = Position;
    }

    public void SetScale(double _Scale)
    {
      if (Scale == _Scale) return;
      needsUpdate = true;

      Scale = _Scale;
      if (Message != null) Message.Scale = _Scale;
    }

    public void SetColor(string _Color)
    {
      if (Color == _Color) return;
      needsUpdate = true;

      Color = _Color;
      if (_Color == "")
        ColorObject = $"<color={surface.FontColor.R},{surface.FontColor.G},{surface.FontColor.B}>";
      else
        ColorObject = $"<color={Color}>";
    }

    public void SetFont(string _Font = "")
    {
      if (Font == _Font) return;
      needsUpdate = true;

      Font = _Font;
      if (_Font == "")
        Font = defaultFont;
      else
        Font = _Font;
    }

    public void SetBackground(bool _HasBackground, string _BackgroundColor = "", int _BackgroundOpacity = defaultBackgroundOpacity)
    {
      if (HasBackground == _HasBackground && BackgroundColor == _BackgroundColor && BackgroundOpacity == _BackgroundOpacity) return;
      needsUpdate = true;

      HasBackground = _HasBackground;
      BackgroundColor = _BackgroundColor;
      BackgroundOpacity = _BackgroundOpacity;
      if (_BackgroundColor == "")
      {
        BackgroundColorObject = defaultBackgroundColor;
      }
      else
      {
        if (_BackgroundColor.Contains(","))
        {
          var parts = _BackgroundColor.Split(',');
          BackgroundColorObject = new Color(
            Utils.TryGetInt(parts[0], 0),
            Utils.TryGetInt(parts[1], 0),
            Utils.TryGetInt(parts[2], 0)
          );
        }
        else
        {
          BackgroundColorObject = Colors.GetColor(_BackgroundColor);
        }

        BackgroundColorObject *= MathHelper.Clamp(_BackgroundOpacity, 0, 255) / 255f;
      }

      if (Background != null)
      {
        Background.Visible = HasBackground;
        Background.BillBoardColor = BackgroundColorObject;
      }
    }

    public void SetRange(bool _HasRange, int _Range = defaultRange)
    {
      if (HasRange == _HasRange && Range == _Range) return;
      needsUpdate = true;

      HasRange = _HasRange;
      Range = _Range;
    }

    public void SetNoHideHud(bool _NoHideHud = false)
    {
      if (NoHideHud == _NoHideHud) return;
      needsUpdate = true;

      NoHideHud = _NoHideHud;
      if (NoHideHud)
      {
        if (Message != null) Message.Options &= ~HudAPIv2.Options.HideHud;
        if (Background != null) Background.Options &= ~HudAPIv2.Options.HideHud;
      }
      else
      {
        if (Message != null) Message.Options |= HudAPIv2.Options.HideHud;
        if (Background != null) Background.Options |= HudAPIv2.Options.HideHud;
      }
    }

    public void SetRestrictions(RestrictionEnum _Restrictions = defaultRestrictions)
    {
      if (Restrictions == _Restrictions) return;
      needsUpdate = true;

      Restrictions = _Restrictions;
    }

    static readonly Regex regexPos;
    static readonly Regex regexScale;
    static readonly Regex regexColor;
    static readonly Regex regexBackground;
    static readonly Regex regexRestriction;
    static readonly Regex regexRange;

    static HudSurface()
    {
      regexPos = new Regex(@"(?xi) ( [\d.+-]+ )? \* ( [\d.+-]+ )? ", RegexOptions.Compiled | RegexOptions.RightToLeft);
      regexScale = new Regex(@"(?xi)  @ ( [\d.+-]+ ) ", RegexOptions.Compiled | RegexOptions.RightToLeft);
      regexColor = new Regex(@"(?xi)  \# ( [a-z]+ | \d{1,3},\d{1,3},\d{1,3} )", RegexOptions.Compiled | RegexOptions.RightToLeft);
      regexBackground = new Regex(@"(?xi) (?: background | \bbg\b ) (?: = ( [a-z]+ | \d{1,3},\d{1,3},\d{1,3} ) (?: ,(\d{1,3}) )? )?", RegexOptions.Compiled | RegexOptions.RightToLeft);
      regexRange = new Regex(@"(?xi) (?: range ) (?: = ( [a-z]+ | \d{1,3},\d{1,3},\d{1,3} ) )?", RegexOptions.Compiled | RegexOptions.RightToLeft);
      // someday we may support !first etc.
      regexRestriction = new Regex(@"(?xi) ( first | third | camera )", RegexOptions.Compiled);
    }

    public void ApplyConfig(string config)
    {
      Match match;
      match = regexPos.Match(config);
      SetPosition(
        match.Success ? Utils.TryGetDouble(match.Groups[1].Value, defaultPosX) : defaultPosX,
        match.Success ? Utils.TryGetDouble(match.Groups[2].Value, defaultPosY) : defaultPosY
      );
      match = regexScale.Match(config);
      SetScale(
        match.Success ? Utils.TryGetDouble(match.Groups[1].Value, defaultScale) : defaultScale
      );
      match = regexColor.Match(config);
      SetColor(match.Success ? match.Groups[1].Value : defaultColor);

      if (config.IndexOf(monospaceFont, StringComparison.OrdinalIgnoreCase) != -1)
        SetFont(monospaceFont);
      else
        SetFont();

      SetNoHideHud(config.IndexOf("nohide", StringComparison.OrdinalIgnoreCase) != -1);

      match = regexBackground.Match(config);
      if (match.Success)
        SetBackground(
          true, match.Groups[1].Value,
          Utils.TryGetInt(match.Groups[2].Value, defaultBackgroundOpacity)
        );
      else
        SetBackground(false);

      match = regexRange.Match(config);
      if (match.Success)
        SetRange(
          true, Utils.TryGetInt(match.Groups[1].Value, defaultRange)
        );
      else
        SetRange(false);

      var matches = regexRestriction.Matches(config);
      if (matches.Count > 0)
      {
        RestrictionEnum NewRestrictions = RestrictionEnum.None;
        foreach (Match one in matches)
        {
          switch (one.Groups[1].Value)
          {
            case "first": NewRestrictions |= RestrictionEnum.First; break;
            case "third": NewRestrictions |= RestrictionEnum.Third; break;
            case "camera": NewRestrictions |= RestrictionEnum.Camera; break;
          }
        }
        SetRestrictions(NewRestrictions);
      }
      else
      {
        SetRestrictions();
      }
    }
  }
}
