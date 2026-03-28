using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NOFFB
{
    public class DCSStyleTelemetry : MonoBehaviour
    {
        public string AircraftName = string.Empty;
        public float EngineRpmLeft = 0f;
        public float EngineRpmRight = 0f;
        public float LeftGear = 0f;
        public float NoseGear = 0f;
        public float RightGear = 0f;
        public float AccX = 0f;
        public float AccY = 0f;
        public float AccZ = 0f;
        public float VectorVelocityX = 0f;
        public float VectorVelocityY = 0f;
        public float VectorVelocityZ = 0f;
        public float VectorVelocityXPrev = 0f;
        public float VectorVelocityYPrev = 0f;
        public float VectorVelocityZPrev = 0f;
        public float Tas = 0f;
        public float Ias = 0f;
        public float VerticalVelocitySpeed = 0f;
        public float Aoa = 0f;
        public float Pitch = 0f;
        public float Bank = 0f;
        public float Aos = 0f;
        public float FlapPos = 0f;
        public float GearValue = 0f;
        public float SpeedbrakeValue = 0f;
        public float Afterburner1 = 0f;
        public float Afterburner2 = 0f;
        public int CannonShells = 0;
        public float Mach = 0f;
        public float HAboveSeaLevel = 0f;

        private Aircraft aircraft;
        UDPClient udp;

        float timer = 0;
        private string ToDCSResponse()
        {
            return new string($"aircraft_name,P-51D" +
                $"engine_rpm_left,{EngineRpmLeft};" +
                $"engine_rpm_right,{EngineRpmRight};" +
                $"left_gear,{LeftGear};" +
                $"nose_gear,{NoseGear};" +
                $"right_gear,{RightGear};" +
                $"acc_x,{AccX};" +
                $"acc_y,{AccY};" +
                $"acc_z,{AccZ};" +
                $"vector_velocity_x,{VectorVelocityX};" +
                $"vector_velocity_y,{VectorVelocityY};" +
                $"vector_velocity_z,{VectorVelocityZ};" +
                $"tas,{Tas};" +
                $"ias,{Ias};" +
                $"vertical_velocity_speed,{VerticalVelocitySpeed};" +
                $"aoa,{Aoa};" +
                $"pitch,{Pitch};" +
                $"bank,{Bank};" +
                $"aos,{Aos};" +
                $"flap_pos,{FlapPos};" +
                $"gear_value,{GearValue};" +
                $"speedbrake_value,{SpeedbrakeValue};" +
                $"afterburner_1,{Afterburner1};" +
                $"afterburner_2,{Afterburner2};" +
                $"weapon,;" +
                $"flare,{0};" +
                $"chaff,{0};" +
                $"cannon_shells,{CannonShells};" +
                $"mach,{Mach};" +
                $"h_above_sea_level,{HAboveSeaLevel};");
        }

        void Awake()
        {
            UDPClient udp = new UDPClient(8000);

        }
        void Update()
        {
            timer += Time.deltaTime;
            GameManager.GetLocalAircraft(out aircraft);


            if (timer >= Time.fixedDeltaTime)
            {
                //do stuff
                if (aircraft != null)
                {
                    PollAirCraft();
                }

                timer = 0f;
            }
        }
        void PollAirCraft()
        {

        }
        void GetEngineRPM()
        {
            if (aircraft.engines.Count == 1)
            {
                float rpm = aircraft.engines[0].GetRPM();
                EngineRpmLeft = rpm;
                EngineRpmRight = rpm;
                return;
            }
            if (aircraft.engines.Count == 2)
            {
                EngineRpmLeft = aircraft.engines[0].GetRPM();
                EngineRpmRight = aircraft.engines[1].GetRPM();
                return;
            }
            if (aircraft.engines.Count == 4)
            {
                EngineRpmLeft = (aircraft.engines[0].GetRPM() + aircraft.engines[2].GetRPM()) / 2f;
                EngineRpmRight = (aircraft.engines[1].GetRPM() + aircraft.engines[3].GetRPM()) / 2f;
                return;
            }
            return;
        }
        void GetLandingGear()
        {

        }
        void GetValocityAccel()
        {
            
        }
        void GetVectorVelocity()
        {

        }
        void GetTas()
        {

        }
        void GetIas()
        {

        }
        void GetCliMbRate()
        {

        }
        void GetAoA()
        {

        }
        void GetPitch()
        {

        }
        void GetBank()
        {

        }
        void GetAoS()
        {

        }
        void GetFlapPos()
        {

        }
        void GetGearValue()
        {

        }
        void GetSpeedBrakeValue()
        {

        }
        void GetAfterburners()
        {

        }
        void GetCannonShells()
        {

        }
        void GetMach()
        {

        }
        void GetASL()
        {

        }
    }
}
