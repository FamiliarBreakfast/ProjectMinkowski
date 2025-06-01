using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectMinkowski.Entities;

namespace ProjectMinkowski.Multiplayer.Local;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
public class ControlAttribute : Attribute
{
    public string ControlName { get; }

    public ControlAttribute(string controlName)
    {
        ControlName = controlName;
    }
}

public static class InputSystem
{
    private static Dictionary<string, Func<float>> ControllerMap = new()
    {
        { "Parallel", () => _currentGamePadState.ThumbSticks.Left.Y },
        { "Perpendicular", () => _currentGamePadState.ThumbSticks.Right.X },
        { "Azimuth", () => _currentGamePadState.ThumbSticks.Left.X * 2 },
        { "Beam", () => PadPressed(Buttons.A) },
        { "Mine", () => PadPressed(Buttons.B) },
        { "Zoom", () => PadDown(Buttons.X) }
    };

    private static Dictionary<string, Func<float>> KeyboardMap = new()
    {
        { "Parallel", () => KeyDown(Keys.W) - KeyDown(Keys.S) },
        { "Perpendicular", () => KeyDown(Keys.D) - KeyDown(Keys.A) },
        { "Azimuth", () => KeyDown(Keys.E) - KeyDown(Keys.Q) },
        { "Beam", () => KeyPressed(Keys.Space) },
        { "Mine", () => KeyPressed(Keys.Z) },
        { "Zoom", () => KeyPressed(Keys.Tab) }
        //{ "Jump", () => KeyPressed(Keys.X) }
    };
    
    private static Dictionary<string, Func<float>> KeyboardMap2 = new()
    {
        { "Parallel", () => KeyDown(Keys.I) - KeyDown(Keys.K) },
        { "Perpendicular", () => KeyDown(Keys.J) - KeyDown(Keys.L) },
        { "Azimuth", () => KeyDown(Keys.U) - KeyDown(Keys.O) },
        { "Beam", () => KeyPressed(Keys.Enter) },
        { "Mine", () => KeyPressed(Keys.RightShift) },
        { "Zoom", () => KeyPressed(Keys.OemBackslash) }
    };
    
    private static List<(PlayerIndex?, Dictionary<string, Func<float>>)> ControlType = new()
    {
        (null, KeyboardMap),
        (PlayerIndex.One, ControllerMap), //(null, KeyboardMap2),
        (PlayerIndex.Two, ControllerMap),
        (PlayerIndex.Three, ControllerMap)
    };
    
    private static KeyboardState _previousKeyboardState;
    private static KeyboardState _currentKeyboardState;
    private static GamePadState _previousGamePadState;
    private static GamePadState _currentGamePadState;
    
    public static void InjectControls(object target, Dictionary<string, Func<float>> controlMap)
    {
        var type = target.GetType();

        // Handle fields
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var controlAttr = field.GetCustomAttribute<ControlAttribute>();
            if (controlAttr == null) continue;

            if (controlMap.TryGetValue(controlAttr.ControlName, out var inputFunc))
            {
                float value = inputFunc.Invoke();
                if (field.FieldType == typeof(int))
                    field.SetValue(target, (int)value);
                else if (field.FieldType == typeof(float))
                    field.SetValue(target, value);
            }
        }

        // Handle methods
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var controlAttr = method.GetCustomAttribute<ControlAttribute>();
            if (controlAttr == null) continue;

            if (controlMap.TryGetValue(controlAttr.ControlName, out var inputFunc))
            {
                float value = inputFunc.Invoke();
                var parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    if (value != 0f)
                        method.Invoke(target, null);
                }
                else if (parameters.Length == 1)
                {
                    if (parameters[0].ParameterType == typeof(float))
                        method.Invoke(target, new object[] { value });
                    else if (parameters[0].ParameterType == typeof(int))
                        method.Invoke(target, new object[] { (int)value });
                }
                else
                {
                    throw new InvalidOperationException($"Method {method.Name} has unsupported parameter count.");
                }
            }
        }
    }

    public static void Update(float dt, Ship ship)
    {
        _previousKeyboardState = _currentKeyboardState;
        _currentKeyboardState = Keyboard.GetState();

        if (ControlType[ship.Id].Item1 != null)
        {
            _previousGamePadState = _currentGamePadState; //todo: this needs to be fixed
            _currentGamePadState = GamePad.GetState(PlayerIndex.One);
        }
        InjectControls(ship, ControlType[ship.Id].Item2);
    }
    
    public static int KeyDown(Keys key) => _currentKeyboardState.IsKeyDown(key) ? 1 : 0;
    public static int KeyUp(Keys key) => _currentKeyboardState.IsKeyUp(key) ? 1 : 0;
    public static int KeyPressed(Keys key) => _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key) ? 1 : 0;
    public static int KeyReleased(Keys key) => _currentKeyboardState.IsKeyUp(key) && _previousKeyboardState.IsKeyDown(key) ? 1 : 0;
    
    public static int PadDown(Buttons button) => _currentGamePadState.IsButtonDown(button) ? 1 : 0;
    public static int PadUp(Buttons button) => _currentGamePadState.IsButtonUp(button) ? 1 : 0;
    public static int PadPressed(Buttons button) => _currentGamePadState.IsButtonDown(button) && _previousGamePadState.IsButtonUp(button) ? 1 : 0;
    public static int PadReleased(Buttons button) => _currentGamePadState.IsButtonUp(button) && _previousGamePadState.IsButtonDown(button) ? 1 : 0;
}