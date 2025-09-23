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
        { "Parallel", () => _currentGamePadState[_currship].ThumbSticks.Left.Y },
        { "Perpendicular", () => _currentGamePadState[_currship].ThumbSticks.Right.X },
        { "Azimuth", () => _currentGamePadState[_currship].ThumbSticks.Left.X * 2 },
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
        { "Zoom", () => KeyDown(Keys.Tab) }
        //{ "Jump", () => KeyPressed(Keys.X) }
    };
    
    private static Dictionary<string, Func<float>> KeyboardMap2 = new()
    {
        { "Parallel", () => KeyDown(Keys.I) - KeyDown(Keys.K) },
        { "Perpendicular", () => KeyDown(Keys.L) - KeyDown(Keys.J) },
        { "Azimuth", () => KeyDown(Keys.U) - KeyDown(Keys.O) },
        { "Beam", () => KeyPressed(Keys.M) },
        { "Mine", () => KeyPressed(Keys.N) },
        { "Zoom", () => KeyDown(Keys.B) }
    };
    
    private static List<(PlayerIndex?, Dictionary<string, Func<float>>)> ControlType = new()
    {
        (null, KeyboardMap),
        (null, KeyboardMap2),
        (PlayerIndex.One, ControllerMap),
        (PlayerIndex.Two, ControllerMap),
        
        
        
        
        //,
        //(PlayerIndex.Three, ControllerMap),
        //(PlayerIndex.Four, ControllerMap)
    };
    
    private static KeyboardState _previousKeyboardState;
    private static KeyboardState _currentKeyboardState;
    private static Dictionary<int, GamePadState> _previousGamePadState = new();
    private static Dictionary<int, GamePadState> _currentGamePadState = new();
    private static int _currship = 0;
    
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
            _currship = ship.Id;
            if (_currentGamePadState.ContainsKey(_currship))
            {
                _previousGamePadState[_currship] = _currentGamePadState[_currship]; //todo: this needs to be fixed
            }

            _currentGamePadState[_currship] = GamePad.GetState((PlayerIndex)ControlType[ship.Id].Item1);
        }
        InjectControls(ship, ControlType[ship.Id].Item2);
    }
    
    public static int KeyDown(Keys key) => _currentKeyboardState.IsKeyDown(key) ? 1 : 0;
    public static int KeyUp(Keys key) => _currentKeyboardState.IsKeyUp(key) ? 1 : 0;
    public static int KeyPressed(Keys key) => _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key) ? 1 : 0;
    public static int KeyReleased(Keys key) => _currentKeyboardState.IsKeyUp(key) && _previousKeyboardState.IsKeyDown(key) ? 1 : 0;
    
    public static int PadDown(Buttons button) => _currentGamePadState[_currship].IsButtonDown(button) ? 1 : 0;
    public static int PadUp(Buttons button) => _currentGamePadState[_currship].IsButtonUp(button) ? 1 : 0;
    public static int PadPressed(Buttons button) => _currentGamePadState[_currship].IsButtonDown(button) && _previousGamePadState[_currship].IsButtonUp(button) ? 1 : 0;
    public static int PadReleased(Buttons button) => _currentGamePadState[_currship].IsButtonUp(button) && _previousGamePadState[_currship].IsButtonDown(button) ? 1 : 0;
}