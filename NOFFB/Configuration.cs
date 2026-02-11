using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

namespace NOFFB
{
    
    public static class Configuration
    {

        internal const string GeneralSettings = "General Settings";
        internal const string FlightStickSettings = "Flight Stick Settings";
        internal const string SteeringWheelSettings = "Steering Wheel Settings";

        internal static ConfigEntry<bool> FFB_DebugUI;
        internal static bool FFB_DebugUI_Default = false;
        /*
        internal static ConfigEntry<bool> FFB_xAxis;
        internal static bool FFB_xAxis_Default = false;
        internal static ConfigEntry<bool> FFB_yAxis;
        internal static bool FFB_yAxis_Default = false;
        */
        internal static ConfigEntry<bool> FFB_xAxisInvert;
        internal static bool FFB_xAxisInvert_Default = false;
        internal static ConfigEntry<bool> FFB_yAxisInvert;
        internal static bool FFB_yAxisInvert_Default = false;

        internal static ConfigEntry<float> FFB_Gain;
        internal static float FFB_Gain_Default = 0.5f;
        /*
        internal static ConfigEntry<bool> FFB_Damper;
        internal static bool FFB_Damper_Default = true;
        internal static ConfigEntry<float> FFB_DamperGain;
        internal static float FFB_DamperGain_Default = 0.2f;
        */
        /*
        internal static ConfigEntry<bool>  FFB_FBWPushBack;
        internal static bool FFB_FBWPushBack_Default = true;
        internal static ConfigEntry<float> FFB_FBWPushBack_Gain;
        internal static float FFB_FBWPushBack_Gain_Default = 1.0f;
        */
        internal static ConfigEntry<float> FFB_FBWPushBack_Factor;
        internal static float FFB_FBWPushBack_Factor_Default = 0.5f;
        /*
        internal static ConfigEntry<bool>  FFB_CockpitShake;
        internal static bool FFB_CockpitShake_Default = false;
        internal static ConfigEntry<float> FFB_CockpitShake_Gain;
        internal static float FFB_CockpitShake_Gain_Default = 1.0f;

        internal static ConfigEntry<bool>  FFB_GunRecoil;
        internal static bool FFB_GunRecoil_Default = false;
        internal static ConfigEntry<float> FFB_GunRecoil_Gain;
        internal static float FFB_GunRecoil_Gain_Default = 1.0f;

        internal static ConfigEntry<bool> FFB_StallEffect;
        internal static bool FFB_StallEffect_Default = false;
        internal static ConfigEntry<float> FFB_StallEffect_Factor;
        internal static float FFB_StallEffect_Factor_Default = 0.5f;

        internal static ConfigEntry<bool> FFB_SuspensionForces;
        internal static bool FFB_SuspensionForces_Default = true;
        internal static ConfigEntry<float> FFB_SuspensionForces_Gain;
        internal static float FFB_SuspensionForces_Gain_Default = 0.2f;

        internal static ConfigEntry<bool> FFB_SelfAligningTorque;
        internal static bool FFB_SelfAligningTorque_Default = true;
        internal static ConfigEntry<float> FFB_SelfAligningTorqueGain;
        internal static float FFB_SelfAligningTorqueGain_Default = 0.2f;
        */


        internal static void InitSettings(ConfigFile config)
        {
            Plugin.Logger?.LogInfo("Loading Settings.");

            // General Settings
            FFB_DebugUI = config.Bind(GeneralSettings, "FFB_DebugUI", FFB_DebugUI_Default, "Toggle the debug UI overlay.");

            //FFB_xAxis = config.Bind(GeneralSettings, "FFB_xAxis", FFB_xAxis_Default, "Enable force feedback on the X axis. (Steering Wheel axis, or Roll on Flight Stick)");
            //FFB_yAxis = config.Bind(GeneralSettings, "FFB_yAxis", FFB_yAxis_Default, "Enable force feedback on the Y axis. (Pitch on Flight Stick)");

            FFB_xAxisInvert = config.Bind(GeneralSettings, "FFB_xAxisInvert", FFB_xAxisInvert_Default, "Invert force feedback on the X axis.");
            FFB_yAxisInvert = config.Bind(GeneralSettings, "FFB_yAxisInvert", FFB_yAxisInvert_Default, "Invert force feedback on the Y axis.");

            FFB_Gain = config.Bind(GeneralSettings, "FFB_Gain", FFB_Gain_Default, "Master gain for all force feedback effects.");
            Mathf.Clamp(FFB_Gain.Value, 0f, 1f);
            //FFB_Damper = config.Bind(GeneralSettings, "FFB_Damper", FFB_Damper_Default, "Enable damper effect.");
            //FFB_DamperGain = config.Bind(GeneralSettings, "FFB_DamperGain", FFB_DamperGain_Default, "Gain for damper effect.");

            // Flight Stick Settings
            //FFB_FBWPushBack = config.Bind(FlightStickSettings, "FFB_FBWPushBack", FFB_FBWPushBack_Default, "Enable FBW push back effect.");
            //FFB_FBWPushBack_Gain = config.Bind(FlightStickSettings, "FFB_FBWPushBack_Gain", FFB_FBWPushBack_Gain_Default, "Gain for FBW push back effect.");
            FFB_FBWPushBack_Factor = config.Bind(FlightStickSettings, "FFB_FBWPushBack_Factor", FFB_FBWPushBack_Factor_Default, "Factor for FBW push back effect.");
            Mathf.Clamp(FFB_FBWPushBack_Factor.Value,0f,1f);
            //FFB_CockpitShake = config.Bind(FlightStickSettings, "FFB_CockpitShake", FFB_CockpitShake_Default, "NOTIMPLEMENTED_Enable cockpit shake effect.");
            //FFB_CockpitShake_Gain = config.Bind(FlightStickSettings, "FFB_CockpitShake_Gain", FFB_CockpitShake_Gain_Default, "NOTIMPLEMENTED_Gain for cockpit shake effect.");

            //FFB_GunRecoil = config.Bind(FlightStickSettings, "FFB_GunRecoil", FFB_GunRecoil_Default, "NOTIMPLEMENTED_Enable gun recoil effect.");
            //FFB_GunRecoil_Gain = config.Bind(FlightStickSettings, "FFB_GunRecoil_Gain", FFB_GunRecoil_Gain_Default, "NOTIMPLEMENTED_Gain for gun recoil effect.");

            //FFB_StallEffect = config.Bind(FlightStickSettings, "FFB_StallEffect", FFB_StallEffect_Default, "NOTIMPLEMENTED_Enable stall effect.");
            //FFB_StallEffect_Factor = config.Bind(FlightStickSettings, "FFB_StallEffect_Factor", FFB_StallEffect_Factor_Default, "NOTIMPLEMENTED_Factor for stall effect.");

            // Steering Wheel Settings
            //FFB_SuspensionForces = config.Bind(SteeringWheelSettings, "FFB_SuspensionForces", FFB_SuspensionForces_Default, "NOTIMPLEMENTED_Enable suspension forces effect.");
            //FFB_SuspensionForces_Gain = config.Bind(SteeringWheelSettings, "FFB_SuspensionForces_Gain", FFB_SuspensionForces_Gain_Default, "NOTIMPLEMENTED_Gain for suspension forces effect.");

            //FFB_SelfAligningTorque = config.Bind(SteeringWheelSettings, "FFB_SelfAligningTorque", FFB_SelfAligningTorque_Default, "NOTIMPLEMENTED_Enable self-aligning torque effect.");
            //FFB_SelfAligningTorqueGain = config.Bind(SteeringWheelSettings, "FFB_SelfAligningTorqueGain", FFB_SelfAligningTorqueGain_Default, "NOTIMPLEMENTED_Gain for self-aligning torque effect.");
        }
    }
}
