using System.Diagnostics;
using System.Net.Http.Headers;
using NOFFBController.Messages;
using SharpDX;
using SharpDX.DirectInput;

namespace NOFFBController
{
    public class ForceFeedbackController : IDisposable
    {
        private readonly DirectInput _directInput;
        private Joystick? _joystick;
        private readonly Dictionary<Guid, Effect> _activeEffects = new();
        private bool _isRunning;
        private Task? _pollingTask;
        private Stopwatch timer = new Stopwatch();

        private int xAxisOffset = 0, yAxisOffset = 0;
        private int nextOffset = 0;
        Effect ffbConstantEffect;
        Effect damperEffect;
        Effect periodicEffect;
        Effect suspensionEffect;
        Effect steeringForceEffect;

        List<int> ffbAxes = new List<int>();

        public event Action<ControllerInputMessage>? OnInputUpdate;

        public ForceFeedbackController()
        {
            _directInput = new DirectInput();
        }

        public bool Initialize()
        {
            

            // Find all game control devices
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            var ffDevices = new List<DeviceInstance>();

            Console.WriteLine("\nScanning for devices...\n");

            // List all devices and identify force feedback capable ones
            foreach (var deviceInstance in devices)
            {
                try
                {
                    var joystick = new Joystick(_directInput, deviceInstance.InstanceGuid);
                    bool hasForceFeedback = (joystick.Capabilities.Flags & DeviceFlags.ForceFeedback) != 0;

                    if (hasForceFeedback)
                    {
                        ffDevices.Add(deviceInstance);
                    }

                    joystick.Dispose();
                }
                catch (Exception ex)
                {
                    string err = $"Error checking device {deviceInstance.ProductName}: {ex.Message}";
                    Console.WriteLine(err);
                    

                }
            }

            if (ffDevices.Count == 0)
            {
                string err = "No force feedback capable devices found!";
                Console.WriteLine(err);
                
                return false;
            }

            // Display available force feedback devices
            Console.WriteLine("Available Force Feedback Devices:");
            Console.WriteLine("==================================");
            for (int i = 0; i < ffDevices.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {ffDevices[i].ProductName}");
                Console.WriteLine($"   Type: {ffDevices[i].Type}");
                Console.WriteLine($"   Instance GUID: {ffDevices[i].InstanceGuid}");
                Console.WriteLine();
            }

            // Get user selection
            int selectedIndex = -1;
            if (ffDevices.Count == 1)
            {
                Console.WriteLine("Only one device found. Using it automatically.\n");
                selectedIndex = 0;
            }
            else
            {
                while (selectedIndex < 0 || selectedIndex >= ffDevices.Count)
                {
                    Console.Write($"Select device (1-{ffDevices.Count}): ");
                    string? input = Console.ReadLine();
                    if (int.TryParse(input, out int selection) && selection >= 1 && selection <= ffDevices.Count)
                    {
                        selectedIndex = selection - 1;
                    }
                    else
                    {
                        Console.WriteLine("Invalid selection. Please try again.");
                    }
                }
            }

            // Initialize selected device
            var selectedDevice = ffDevices[selectedIndex];
            try
            {
                _joystick = new Joystick(_directInput, selectedDevice.InstanceGuid);
                Console.WriteLine($"\nInitializing: {selectedDevice.ProductName}");

                // CRITICAL: Set cooperative level BEFORE acquiring
                // Force feedback requires exclusive access
                try
                {
                    // Get console window handle
                    IntPtr consoleHandle = GetConsoleWindow();
                    _joystick.SetCooperativeLevel(consoleHandle,
                        CooperativeLevel.Exclusive | CooperativeLevel.Background);
                    Console.WriteLine("Set exclusive cooperative level");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not set cooperative level: {ex.Message}");
                    // Try with foreground instead
                    try
                    {
                        IntPtr consoleHandle = GetConsoleWindow();
                        _joystick.SetCooperativeLevel(consoleHandle,
                            CooperativeLevel.Exclusive | CooperativeLevel.Foreground);
                        Console.WriteLine("Set exclusive foreground cooperative level");
                    }
                    catch
                    {
                        Console.WriteLine("Warning: Using default cooperative level");
                    }
                }

                // Set properties
                _joystick.Properties.BufferSize = 8192;
                _joystick.Properties.AutoCenter = false;
                
                // Acquire the device
                _joystick.Acquire();
                Console.WriteLine("Device acquired successfully");
                


                var deviceObjs = _joystick.GetObjects().ToList().Select(o => o.ObjectId).ToList();
                List<int> intList = deviceObjs
                    .FindAll(o => o.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator))
                    .Select(o => (int)o).ToList();

                foreach (int i in intList)
                {
                    Console.WriteLine($"Found FFB actuator: {i}");
                    if (nextOffset == 0)
                        xAxisOffset = i;
                    else
                        yAxisOffset = i;
                    nextOffset++;
                }

                foreach (DeviceObjectInstance doi in _joystick.GetObjects())
                {
                    if (doi.ObjectId.Flags.HasFlag(DeviceObjectTypeFlags.ForceFeedbackActuator))
                    {
                        ffbAxes.Add((int)doi.ObjectId);
                        Console.WriteLine($"Found FFB Axis: {doi.Name}, ObjectId: {doi.ObjectId}");
                    }
                }

                Console.WriteLine($"x axis offset: {xAxisOffset}, y axis offser: {yAxisOffset}");

                Console.WriteLine("Device initialized successfully!\n");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing device {selectedDevice.ProductName}: {ex.Message}");
                return false;
            }
        }
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        /*
        public void StartPolling(int intervalMs = 16)
        {
            if (_isRunning || _joystick == null) return;

            _isRunning = true;
            _pollingTask = Task.Run(() => PollLoop(intervalMs));
            Console.WriteLine("Started controller polling");
        }
        */
        public void StopPolling()
        {
            _isRunning = false;
            _pollingTask?.Wait(1000);
        }
        /*
        private async Task PollLoop(int intervalMs)
        {
            while (_isRunning && _joystick != null)
            {
                try
                {
                    _joystick.Poll();
                    var state = _joystick.GetCurrentState();

                    var message = new ControllerInputMessage
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    // Read axes
                    message.Axes["X"] = state.X;
                    message.Axes["Y"] = state.Y;
                    message.Axes["Z"] = state.Z;
                    message.Axes["RotationX"] = state.RotationX;
                    message.Axes["RotationY"] = state.RotationY;
                    message.Axes["RotationZ"] = state.RotationZ;

                    // Read buttons
                    for (int i = 0; i < state.Buttons.Length; i++)
                    {
                        message.Buttons[i] = state.Buttons[i];
                    }

                    OnInputUpdate?.Invoke(message);

                    await Task.Delay(intervalMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error polling controller: {ex.Message}");
                    await Task.Delay(intervalMs);
                }
            }
        }
        */
        private void ReacquireDevice()
        {
            try
            {
                _joystick.Unacquire();
                _joystick.Acquire();
            }
            catch (SharpDXException ex)
            {
                Console.WriteLine($"ex");
            }
        }
        private bool IsDeviceReady()
        {
            try
            {
                _joystick.Poll();
                var state = _joystick.GetCurrentState();
                return true;
            }
            catch
            {
                return false;
            }
        }



        public void ApplyFFBDamper(ForceFeedbackMessage message)
        {
            if (_joystick == null) return;
            try
            {
                var conditions = new Condition[2];

                // X-Axis Damper
                conditions[0] = new Condition
                {
                    Offset = 0,                      // Center point offset (-10000 to 10000)
                    PositiveCoefficient = 3000,     // Resistance moving positive (-10000 to 10000)
                    NegativeCoefficient = 3000,     // Resistance moving negative (-10000 to 10000)
                    PositiveSaturation = 10000,      // Max force positive (0 to 10000)
                    NegativeSaturation = 10000,      // Max force negative (0 to 10000)
                    DeadBand = 0                     // Dead zone around center (0 to 10000)
                };

                // Y-Axis Damper
                conditions[1] = new Condition
                {
                    Offset = 0,
                    PositiveCoefficient = 3000,
                    NegativeCoefficient = 3000,
                    PositiveSaturation = 10000,
                    NegativeSaturation = 10000,
                    DeadBand = 0
                };
                // Create ConditionSet from the array
                var conditionSet = new ConditionSet();
                conditionSet.Conditions = conditions;
                // Create effect parameters
                var effectParams = new EffectParameters
                {
                    Duration = int.MaxValue,         // Infinite duration
                    Gain = 10000,                    // Overall strength (0-10000)
                    TriggerButton = -1,              // No button trigger
                    TriggerRepeatInterval = 0,
                    SamplePeriod = 0,                // Use default
                    Flags = EffectFlags.Polar | EffectFlags.ObjectIds,
                    Parameters = conditionSet

                };
                effectParams.SetAxes(new int[] {ffbAxes[0],ffbAxes[1] },new int[]{0,0});
                damperEffect = new Effect(_joystick, EffectGuid.Damper, effectParams);
                damperEffect.Start();
                Console.WriteLine("Damper Enabled");
            }
            catch (SharpDXException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public void ApplyFFBConstantForce(ForceFeedbackMessage message)
        {
            //timer.Restart();
            if (_joystick == null) return;

            try
            {
                if (!IsDeviceReady())
                {
                    ReacquireDevice();
                }
                //AcquireDevice();
                int[] axes = { ffbAxes[0], ffbAxes[1] };

                int[] directions = { message.DirectionX, message.DirectionY};
                //int[] directions = { message.DirectionX };

                int magnitude = message.Magnitude;

                EffectParameters parameters = new EffectParameters
                {
                    Duration = int.MaxValue,
                    Gain = 10000,
                    TriggerButton = -1,
                    Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds,  // Changed to ObjectIds
                    Axes = axes,
                    Directions = directions,
                    Parameters = new ConstantForce { Magnitude = magnitude }
                };
                
                if (null == ffbConstantEffect)
                {
                    ffbConstantEffect = new Effect(_joystick, EffectGuid.ConstantForce, parameters);
                    ffbConstantEffect.Start();
                } else
                {
                    ffbConstantEffect.SetParameters(parameters, EffectParameterFlags.TypeSpecificParameters);
                    parameters.Directions = directions;
                    ffbConstantEffect.SetParameters(parameters, EffectParameterFlags.Direction);
                }


                Console.WriteLine($"{message.Magnitude}, {message.DirectionX}, {message.DirectionY}");

                    //Effect? ffbEffect = null;
                //int sleep = 16 - (int)timer.ElapsedMilliseconds;
                //if (sleep <= 0) { sleep = 0; }
                //Thread.Sleep(sleep);
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode == ResultCode.NotAcquired ||
                    ex.ResultCode == ResultCode.InputLost ||
                    ex.ResultCode == ResultCode.NotInitialized)
                {
                    ReacquireDevice();
                }
                string err = $"Error applying force feedback: {ex.Message}";
                Console.WriteLine(err);

                Console.WriteLine($"HRESULT: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
        public void ApplyFFBPeriodic(ForceFeedbackMessage message)
        {

        }

        public void ApplyFFBSuspension(ForceFeedbackMessage message)
        {

        }

        public void ApplyFFBSteeringForce(ForceFeedbackMessage message)
        {

        }

        public void StopStaleEffects()
        {

            List<Guid> staleFx = new List<Guid>();
            foreach (Guid key in _activeEffects.Keys)
            {
                if (_activeEffects[key].Status == 0)
                {
                    _activeEffects[key].Stop();
                    _activeEffects[key].Dispose();
                    staleFx.Add(key);
                }
            }
            foreach (Guid k in staleFx)
            {
                _activeEffects.Remove(k);
            }
            staleFx.Clear();
        }

        public void StopAllEffects()
        {
            foreach (var fx in _activeEffects.Values)
            {
                fx.Stop();
                fx.Dispose();
            }
            _activeEffects.Clear();
        }

        public void Dispose()
        {
            StopPolling();
            StopAllEffects();
            _joystick.GetEffects();
            _joystick?.Unacquire();
            _joystick?.Dispose();
            _directInput?.Dispose();
        }
    }
}
