using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using BepInEx.Configuration;
using NOFFB;
using UnityEngine;

namespace NOFFB
{

    public static class Configuration
    {

        public enum FFBTypes
        {
            [Description("NOFFBController - For DirectInput Sticks")]
            NOFFBController = 0,
            [Description("WIP DOES NOT WORK - DCS Style Telemetry - For MOZA FFB Sticks")]
            DCSTelemetry = 1,
            [Description("WIP DOES NOT WORK - Development Experimental")]
            Other = 2
        }

        internal const string GeneralSettings = "General Settings";
        internal const string FlightStickSettings = "Flight Stick Settings";
        internal const string SteeringWheelSettings = "Steering Wheel Settings";

        internal static ConfigEntry<bool> FFB_DebugUI;
        internal static bool FFB_DebugUI_Default = false;

        internal static ConfigEntry<FFBTypes> FFB_Type;
        internal static FFBTypes FFB_Type_Default = FFBTypes.NOFFBController;

        internal static ConfigEntry<bool> FFB_xAxisInvert;
        internal static bool FFB_xAxisInvert_Default = false;
        internal static ConfigEntry<bool> FFB_yAxisInvert;
        internal static bool FFB_yAxisInvert_Default = false;

        internal static ConfigEntry<bool> FFB_SwapStickAxis;
        internal static bool FFB_SwapStickAxis_Default = false;

        internal static ConfigEntry<float> FFB_Gain;
        internal static float FFB_Gain_Default = 0.5f;

        internal static ConfigEntry<bool> FFB_AutoCenter;
        internal static bool FB_AutoCenter_Default = true;

        internal static ConfigEntry<float> FFB_DamperGain;
        internal static float FFB_DamperGain_Default = 0.3f;


        internal static ConfigEntry<float> FFB_FBWPushBack_Factor;
        internal static float FFB_FBWPushBack_Factor_Default = 0.5f;

        internal static ConfigEntry<bool> FFB_CockpitShake;
        internal static bool FFB_CockpitShake_Default = false;
        internal static ConfigEntry<float> FFB_CockpitShake_Gain;
        internal static float FFB_CockpitShake_Gain_Default = 1.0f;

        internal static ConfigEntry<bool> FFB_GunRecoil;
        internal static bool FFB_GunRecoil_Default = false;
        internal static ConfigEntry<float> FFB_GunRecoil_Gain;
        internal static float FFB_GunRecoil_Gain_Default = 0.5f;

        internal static ConfigEntry<bool> FFB_StallEffect;
        internal static bool FFB_StallEffect_Default = false;
        internal static ConfigEntry<float> FFB_StallEffect_Factor;
        internal static float FFB_StallEffect_Factor_Default = 0.5f;
        
        
        //wheel config items
        internal static ConfigEntry<float> FFB_SuspensionForces_Gain;
        internal static float FFB_SuspensionForces_Gain_Default = 1f;

        internal static ConfigEntry<float> FFB_SelfAligningTorque_Gain;
        internal static float FFB_SelfAligningTorque_Gain_Default = 1f;

        internal static ConfigEntry<float> FFB_TyreFriction_Gain;
        internal static float FFB_TyreFriction_Gain_Default = 1f;

        internal static ConfigEntry<float> FFB_DynamicDamping_Gain;
        internal static float FFB_DynamicDamping_Gain_Default = 1f;

        internal static ConfigEntry<float> FFB_UnsteeredSuspension_Blend;
        internal static float FFB_UnsteeredSuspension_Blend_Default = 0.5f;

        internal static ConfigEntry<float> FFB_UnderSteer_Gain;
        internal static float FFB_UnderSteer_Gain_Default = 0.5f;
        //


        internal static void InitSettings(ConfigFile config)
        {
            Plugin.Logger?.LogInfo("Loading Settings.");

            // General Settings
            FFB_DebugUI = config.Bind(GeneralSettings, "FFB_DebugUI", FFB_DebugUI_Default, "Toggle the debug UI overlay.");

            FFB_Type = config.Bind(GeneralSettings, "FFB_Type", FFB_Type_Default, "Select NOFFB Output Type.");

            FFB_xAxisInvert = config.Bind(GeneralSettings, "FFB_xAxisInvert", FFB_xAxisInvert_Default, "Invert force feedback on the X axis.");
            FFB_yAxisInvert = config.Bind(GeneralSettings, "FFB_yAxisInvert", FFB_yAxisInvert_Default, "Invert force feedback on the Y axis.");

            FFB_SwapStickAxis = config.Bind(GeneralSettings, "FFB_SwapStickAxis", FFB_SwapStickAxis_Default, "THE DREADED LOGITECH 940 BUTTON");

            FFB_AutoCenter = config.Bind(GeneralSettings, "FFB_AutoCenter", FB_AutoCenter_Default, "Toggle Auto Center.");

            FFB_Gain = config.Bind(GeneralSettings, "FFB_Gain", FFB_Gain_Default, new ConfigDescription("Master gain for all force feedback effects.",new AcceptableValueRange<float>(0f,1f)));



            FFB_DamperGain = config.Bind(GeneralSettings, "FFB_DamperGain", FFB_DamperGain_Default,new ConfigDescription("Gain for damper effect.", new AcceptableValueRange<float>(0f, 1f)));
            // Flight Stick Settings
            FFB_FBWPushBack_Factor = config.Bind(FlightStickSettings, "FFB_FBWPushBack_Factor", FFB_FBWPushBack_Factor_Default,new ConfigDescription("Factor for FBW push back effect.",new AcceptableValueRange<float>(0f,1f)));
            FFB_GunRecoil_Gain = config.Bind(FlightStickSettings, "FFB_GunRecoil_Gain", FFB_GunRecoil_Gain_Default,new ConfigDescription("Gain for gun recoil effect.",new AcceptableValueRange<float>(0f,1f)));


            // Wheel settings
            FFB_SuspensionForces_Gain = config.Bind(SteeringWheelSettings, "FFB_SuspensionForces_Gain", FFB_SuspensionForces_Gain_Default,new ConfigDescription("Gain for suspension forces effect.",new AcceptableValueRange<float>(0f,1f)));
            FFB_SelfAligningTorque_Gain = config.Bind(SteeringWheelSettings, "FFB_SelfAligningTorqueGain", FFB_SelfAligningTorque_Gain_Default,new ConfigDescription("Gain for Aelf-Aligning Torque effect.",new AcceptableValueRange<float>(0f,1f)));
            FFB_TyreFriction_Gain = config.Bind(SteeringWheelSettings, "FFB_TyreFriction_Gain", FFB_TyreFriction_Gain_Default, new ConfigDescription("Gain for tyre friction forces effect.", new AcceptableValueRange<float>(0f, 1f)));
            FFB_DynamicDamping_Gain = config.Bind(SteeringWheelSettings, "FFB_DynamicDamping_Gain", FFB_DynamicDamping_Gain_Default, new ConfigDescription("Variable Damper effect based on Speed.", new AcceptableValueRange<float>(0f, 1f)));
            FFB_UnsteeredSuspension_Blend = config.Bind(SteeringWheelSettings, "FFB_UnsteeredSuspension_Blend", FFB_UnsteeredSuspension_Blend_Default, new ConfigDescription("Ratio of how much to blend unsteered suspension into constant force effects.", new AcceptableValueRange<float>(0f, 1f)));
            FFB_UnderSteer_Gain = config.Bind(SteeringWheelSettings, "FFB_UnderSteer_Gain", FFB_UnderSteer_Gain_Default, new ConfigDescription("Influiences how much the Self-Aligning Torque effect is impacted by sideways tyre friction on Steered Wheels.", new AcceptableValueRange<float>(0f, 1f)));

        }
    }
}
