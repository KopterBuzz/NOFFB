using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using SharpDX.DirectInput;
using System.Threading.Tasks;
using NuclearOption.Networking;

namespace NOFFB
{
    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "NOFFB";
        public const string PLUGIN_NAME = "NOFFB";
        public const string PLUGIN_VERSION = "0.1";
    }
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("NuclearOption.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource? Logger;
        //internal FFBManager ffb;
        internal bool ffbAcquired = false;

        internal bool showGUI = false;
        internal bool showGUIList = true;
        internal Dictionary<string, int> axisStateMsg;
        internal Dictionary<string, int> axisState;

        internal bool testFFB = false;
        internal int testX = 2000;
        internal int testY = 2000;

        bool isMission;

        Player player;
        PilotPlayerState playerState;
        Aircraft aircraft;
        bool pollAircraft;
        Vector3 aircraftAccel;


        UDPClient udp;


        private Dictionary<string, float> barValues = new Dictionary<string, float>
        {
            { "RawPitch", 0.0f },
            { "RawRoll", 0.0f },
            { "RawYaw", 0.0f },
            { "FBWPitch", 0.0f },
            { "FBWRoll", 0.0f },
            { "FBWYaw", 0.0f },
            { "ForcePitch", 0.0f },
            { "ForceRoll", 0.0f },
            { "ForceMagnitude", 0.0f },
            { "ForceNormMag", 0.0f },
            { "ForceNormMagPrev", 0.0f },
            { "ForceAngle", 0.0f },
            { "pitchDiffBuffer", 0.0f},
            { "rollDiffBuffer", 0.0f},
            { "tRoll", 0.0f },
            { "tPitch", 0.0f }

        };


        // Required fields for FFB Manager GUI
        private Rect windowRect = new Rect(100, 100, 450, 800);
        private bool stylesInit = false;
        private Texture2D stickBoxTexture;
        private Texture2D forceBoxTexture;

        

        public static int selectedIndex = -1;

        void Awake()
        {
            udp = new UDPClient();
            GameObject managerObject = Chainloader.ManagerObject;
            bool flag = managerObject != null;
            if (flag)
            {
                managerObject.hideFlags = HideFlags.HideAndDontSave;
                global::UnityEngine.Object.DontDestroyOnLoad(managerObject);
                Logger?.LogWarning("Force Hide ManagerGameObject");
            }

            //ffb = new FFBManager();
            showGUI = true;
            Logger = base.Logger;
            Logger.LogDebug("AWAKE!");
            // Setup config
            Configuration.InitSettings(Config);

        }

        void Update()
        {
            isMission = MissionManager.IsRunning;
            if (isMission)
            {
                GameManager.GetLocalAircraft(out aircraft);
                if (null != aircraft)
                {
                    pollAircraft = true;
                    PollAircraft(aircraft);
                    
                } else
                {
                    pollAircraft = false;
                }
            }
        }

        void PollAircraft(Aircraft aircraft)
        {
            aircraftAccel = aircraft.accel;
            
            if (aircraft.pilots[0].currentState.GetType() == typeof(PilotPlayerState))
            {
                playerState = (PilotPlayerState)aircraft.pilots[0].currentState;
                ControlInputs input = aircraft.GetInputs();
                barValues["RawPitch"] = playerState.pitchInput;
                barValues["RawRoll"] = playerState.rollInput;
                barValues["RawYaw"] = playerState.yawInput;
                barValues["FBWPitch"] = input.pitch;
                barValues["FBWRoll"] = input.roll;
                barValues["FBWYaw"] = input.yaw;
            }
        }

       
        int[] CalculateFFFB_FBWPushback()
        {

            float ConvertToPositiveAngle2(float angle)
            {
                float angle_ = angle < 0 ? angle + 360 : angle;
                if (angle_ > 359.99f)
                {
                    angle = 359.99f;
                }
                return angle_;
                
            }
            int[] force = new int[3];
            float pitchDiff = 0f;
            float rollDiff = 0f;

            float xForce = 0f;
            float yForce = 0f;

            float pitch_ = Mathf.Abs(barValues["RawPitch"]);
            float fbwPitch_ = Mathf.Abs(barValues["FBWPitch"]);
            float roll_ = Mathf.Abs(barValues["RawRoll"]);
            float fbwRoll_ = Mathf.Abs(barValues["FBWRoll"]);

            if (roll_ > fbwRoll_)
            {
                
                rollDiff = barValues["RawRoll"] - barValues["FBWRoll"];
                if(rollDiff > barValues["rollDiffBuffer"])
                {
                    barValues["rollDiffBuffer"] = rollDiff;
                    
                }
                xForce += (Mathf.Lerp(barValues["rollDiffBuffer"], barValues["RawRoll"], barValues["tRoll"]));
                barValues["tRoll"] += 0.5f * Time.fixedDeltaTime;
                barValues["rollDiffBuffer"] = xForce;
            } else
            {
                barValues["rollDiffBuffer"] = 0f;
                barValues["tRoll"] = Configuration.FFB_FBWPushBack_Factor.Value;
            }

            if (pitch_ > fbwPitch_)
            {

                pitchDiff = barValues["RawPitch"] - barValues["FBWPitch"];
                if (pitchDiff > barValues["pitchDiffBuffer"])
                {
                    barValues["pitchDiffBuffer"] = pitchDiff;

                }
                yForce += (Mathf.Lerp(barValues["pitchDiffBuffer"], barValues["RawPitch"], barValues["tPitch"]));
                barValues["tPitch"] += 0.5f * Time.fixedDeltaTime;
                barValues["pitchDiffBuffer"] = yForce;
            }
            else
            {
                barValues["pitchDiffBuffer"] = 0f;
                barValues["tPitch"] = Configuration.FFB_FBWPushBack_Factor.Value;
            }

            xForce = Mathf.Clamp(xForce, -1.0f, 1.0f);
            yForce = Mathf.Clamp(yForce, -1.0f, 1.0f);

            float normalizedMagnitude = Mathf.Clamp((float)Mathf.Sqrt(yForce * yForce + xForce * xForce),-1f,1f);

            force[0] = (int)(normalizedMagnitude * 10000f * Configuration.FFB_Gain.Value);

            if (Configuration.FFB_xAxisInvert.Value) { xForce = -xForce; }
            if (Configuration.FFB_yAxisInvert.Value) { yForce = -yForce; }

            float angle = ConvertToPositiveAngle2(Mathf.Atan2(yForce, xForce) * 180f / Mathf.PI);
            barValues["ForcePitch"] = yForce;
            barValues["ForceRoll"] = xForce;
            barValues["ForceMagnitude"] = force[0];
            barValues["ForceAngle"] = angle;
            force[1] = (int)(Mathf.Cos(angle * 3.1415926f / 180.0f) * 10000);
            force[2] = (int)(Mathf.Sin(angle * 3.1415926f / 180.0f) * 10000);

            return force;
        }
        int[] CalculateFFB_CockpitShake()
        {
            int[] force = new int[3];
            return force;
        }
        int[] CalculateFFB_GunRecoil()
        {
            int[] force = new int[3];
            return force;
        }
        int[] CalculateFFB_Stall()
        {
            int[] force = new int[3];
            return force;
        }
        int[] CalculateFFB_Suspension()
        {
            int[] force = new int[3];
            return force;
        }
        int[] CalculateFFB_Steering()
        {
            int[] force = new int[3];
            return force;
        }

        void FixedUpdate()
        {
            if (null != aircraft && pollAircraft)
            {
                int[] data = CalculateFFFB_FBWPushback();
                SendFFB(data);
                //CalculateStickWeightForce();
            }
        }

        void SendFFB(int[] data)
        {
            string msg = $"ConstantForce,{data[0]},{data[1]},{data[2]}";
            Plugin.Logger.LogDebug($"SENDER SIDE: {msg}");
            udp.SendData(msg);
        }

        private async Task GetAxisState(Dictionary<string, int> stateMsg)
        {
            axisState = stateMsg;
            Plugin.Logger?.LogDebug($"X: {axisState["X"]}\n" +
                $"Y: {axisState["Y"]}\n" +
                $"Z: {axisState["Z"]}\n" +
                $"");
        }
        void OnGUI()
        {
            if (Configuration.FFB_DebugUI.Value)
            {
                windowRect = GUI.Window(0, windowRect, DrawWindow, "Draggable Window");
            }
            
        }

        /// <summary>
        /// Draws a widget with 6 bars where each bar length is proportional to its float value (-1 to 1)
        /// </summary>
        /// <param name="values">Array of 6 float values (range -1 to 1)</param>
        /// <param name="labels">Optional array of 6 labels for each bar</param>
        /// <param name="maxBarWidth">Maximum width of each bar in pixels (total width for both directions)</param>
        /// <param name="barHeight">Height of each bar in pixels</param>
        void DrawWindow(int windowID)
        {

            if (!stylesInit)
            {
                InitStyles();
            }
           
            // Calculate center position for the 400x400 rectangle within the window
            float rectX = (windowRect.width - 400) / 2;
            float rectY = 30; // Offset from top to account for title bar

            // Draw the 400x400 rectangle
            GUI.Box(new Rect(rectX, rectY, 400, 400), "");



            // Calculate center of the 400x400 rectangle for the 10x10 rectangle
            float smallRectX = rectX + (400 - 10) / 2;
            float smallRectY = rectY + (400 - 10) / 2;
            float offsetX = 400 * barValues["RawRoll"] / 2;
            float offsetY = 400 * barValues["RawPitch"] / 2;
            float forceOffsetX = 400 * barValues["ForceRoll"] / 2;
            float forceOffsetY = 400 *  barValues["ForcePitch"] / 2;


            // Draw the 10x10 rectangle in the middle
            
            GUI.DrawTexture(new Rect(smallRectX + forceOffsetX, smallRectY + forceOffsetY, 10, 10), forceBoxTexture);
            GUI.DrawTexture(new Rect(smallRectX + offsetX, smallRectY + offsetY, 10, 10), stickBoxTexture);

            float labelY = rectY + 400 + 10; // 10 pixels below the rectangle
            GUI.Label(new Rect(rectX, labelY, 200, 20), "ForcePitch: " + barValues["ForcePitch"].ToString("F2"));
            GUI.Label(new Rect(rectX, labelY + 20, 200, 20), "ForceRoll: " + barValues["ForceRoll"].ToString("F2"));
            GUI.Label(new Rect(rectX, labelY + 40, 200, 20), "ForceMagnitude: " + barValues["ForceMagnitude"].ToString("F2"));
            GUI.Label(new Rect(rectX, labelY + 60, 200, 20), "ForceAngle: " + barValues["ForceAngle"].ToString("F2"));


            // Make the window draggable
            GUI.DragWindow();
        }

        private void InitStyles()
        {
            if (stickBoxTexture == null || forceBoxTexture == null)
            {
                // Create a 1x1 texture with a custom color
                stickBoxTexture = new Texture2D(1, 1);
                stickBoxTexture.SetPixel(0, 0, Color.green); // Change this to your desired color
                stickBoxTexture.Apply();
                // Create a 1x1 texture with a custom color
                forceBoxTexture = new Texture2D(1, 1);
                forceBoxTexture.SetPixel(0, 0, Color.cyan); // Change this to your desired color
                forceBoxTexture.Apply();
            }

        }
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        // Helper function to get color based on value (green->yellow->red)
        private Color GetBarColor(float value)
        {
            if (value > 0.6f)
                return Color.green;
            else if (value > 0.3f)
                return Color.yellow;
            else
                return Color.red;
        }

    }
}
