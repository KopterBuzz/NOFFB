using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirage.Logging;
using NuclearOption.Workshop;
using RoadPathfinding;
using SharpDX.DirectInput;
using SharpDX.Multimedia;
using UnityEngine;

namespace NOFFB
{
    public class WheelPair
    {
        public string name = "Axle";
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool steered = false;
        [Range(0f, 1f)] public float steeringInfluence = 1f; // How much this axle affects steering (1.0 for front, 0 for rear on most cars)

        [HideInInspector] public WheelHit leftHit;
        [HideInInspector] public WheelHit rightHit;
        [HideInInspector] public bool leftGrounded;
        [HideInInspector] public bool rightGrounded;

        public bool BothGrounded => leftGrounded && rightGrounded;
        public bool AnyGrounded => leftGrounded || rightGrounded;

        public float GetSidewaysFrictionForce(WheelCollider wc)
        {
            WheelHit hit;
            if (!wc.GetGroundHit(out hit))
                return 0f;

            // hit.sidewaysSlip is the normalized slip value
            float slip = hit.sidewaysSlip;

            // Evaluate the built-in friction curve manually
            WheelFrictionCurve fc = wc.sidewaysFriction;
            float normalForce = hit.force; // normal load on the tire
            float frictionCoeff = EvaluateFrictionCurve(fc, Mathf.Abs(slip));

            // The sideways force = friction coefficient from curve * normal force
            float sidewaysForce = frictionCoeff * normalForce;
            return sidewaysForce;
        }
        public float GetForwardFrictionForce(WheelCollider wc)
        {
            WheelHit hit;
            if (!wc.GetGroundHit(out hit))
                return 0f;

            // hit.sidewaysSlip is the normalized slip value
            float slip = hit.forwardSlip;

            // Evaluate the built-in friction curve manually
            WheelFrictionCurve fc = wc.forwardFriction;
            float normalForce = hit.force; // normal load on the tire
            float frictionCoeff = EvaluateFrictionCurve(fc, Mathf.Abs(slip));

            // The sideways force = friction coefficient from curve * normal force
            float forwardForce = frictionCoeff * normalForce;
            return forwardForce;
        }

        public float EvaluateFrictionCurve(WheelFrictionCurve curve, float absSlip)
        {
            if (absSlip < curve.extremumSlip)
            {
                // Hermite interpolation from (0,0) to (extremumSlip, extremumValue)
                float t = absSlip / curve.extremumSlip;
                // Smoothstep-like rise
                return curve.extremumValue * (3f * t * t - 2f * t * t * t);
            }
            else if (absSlip < curve.asymptoteSlip)
            {
                // Hermite interpolation from (extremumSlip, extremumValue) to (asymptoteSlip, asymptoteValue)
                float t = (absSlip - curve.extremumSlip) / (curve.asymptoteSlip - curve.extremumSlip);
                float smooth = 3f * t * t - 2f * t * t * t;
                return Mathf.Lerp(curve.extremumValue, curve.asymptoteValue, smooth);
            }
            else
            {
                return curve.asymptoteValue;
            }
        }

        public void UpdateHits()
        {
            leftGrounded = leftWheel != null && leftWheel.GetGroundHit(out leftHit);
            rightGrounded = rightWheel != null && rightWheel.GetGroundHit(out rightHit);
        }

        public float GetAverageSidewaysSlip()
        {

            if (!AnyGrounded) return 0f;
            if (!leftGrounded) return rightHit.sidewaysSlip;
            if (!rightGrounded) return leftHit.sidewaysSlip;
            return (leftHit.sidewaysSlip + rightHit.sidewaysSlip) / 2f;
        }

        public float GetAverageNormalForce()
        {
            if (!AnyGrounded) return 0f;
            if (!leftGrounded) return rightHit.force;
            if (!rightGrounded) return leftHit.force;
            return (leftHit.force + rightHit.force) / 2f;
        }

        public float GetAverageSidewaysFrictionForce()
        {
            float force = ((GetSidewaysFrictionForce(leftWheel) + GetSidewaysFrictionForce(rightWheel)) / 2f);
            Plugin.Logger?.LogDebug($"{leftWheel.name} {rightWheel.name} SIDEWAYS FRICTION FORCE: {force}");
            return force;
        }

        public float GetAverageForwardFrictionForce()
        { 
            return ((GetForwardFrictionForce(leftWheel) + GetForwardFrictionForce(rightWheel)) / 2f);
        }

        public float GetCompressionDifference()
        {
            if (!BothGrounded) return 0f;

            // In Unity 2022.3, we need to calculate compression from the wheel's world position
            // WheelHit doesn't have distance or suspensionDistance
            // We use GetWorldPose to get actual wheel position

            Vector3 leftWheelPos, rightWheelPos;
            Quaternion leftWheelRot, rightWheelRot;
            leftWheel.GetWorldPose(out leftWheelPos, out leftWheelRot);
            rightWheel.GetWorldPose(out rightWheelPos, out rightWheelRot);

            // Calculate how much suspension has compressed
            // suspensionDistance is the max travel from wheel collider position
            float leftTravel = leftWheel.transform.position.y - leftWheelPos.y;
            float rightTravel = rightWheel.transform.position.y - rightWheelPos.y;

            // Normalize to percentage
            float leftComp = leftTravel / leftWheel.suspensionDistance;
            float rightComp = rightTravel / rightWheel.suspensionDistance;

            return leftComp - rightComp;
        }

        public float GetSteerAngle()
        {
            try
            {
                return (leftWheel.steerAngle + rightWheel.steerAngle) / 2f;
            } catch (Exception ex)
            {
                Plugin.Logger?.LogError(ex.Message);
                return 0f;
            }
            
        }

        public void UpdateSteeringInfluence()
        {
            if (Mathf.Approximately(GetSteerAngle(),0.0f))
            {
                steeringInfluence = 0f;
                Plugin.Logger.LogDebug($"{leftWheel.name} + {rightWheel.name} steering influence: {steeringInfluence}");
                return;
            }
            steeringInfluence = 1f;
            steered = true;
            Plugin.Logger.LogDebug($"{leftWheel.name} + {rightWheel.name} steering influence: {steeringInfluence}");
            return;            
        }
    }
    public class WheelFFB : MonoBehaviour
    {
        [Header("Wheel Configuration")]
        [Tooltip("Add wheel pairs from front to back. Each pair = one axle (left + right wheels)")]
        public List<WheelPair> wheelPairs = new List<WheelPair>();

        [Header("Force Weights")]
        [Range(0f, 1f)] public float selfAlignWeight = 0.6f;
        [Range(0f, 1f)] public float bumpSteerWeight = 0.3f;
        [Range(0f, 1f)] public float understeerWeight = 0.4f;
        [Range(0f, 1f)] public float oversteerWeight = 0.5f;

        [Header("Constant Damper Effect")]
        [Tooltip("Enable constant damper (resistance to steering movement)")]
        public bool enableConstantDamper = false;

        [Range(0f, 10000f)]
        [Tooltip("Constant damper force magnitude (always active when enabled)")]
        public float constantDamperMagnitude = 2000f;

        [Range(0f, 10000f)]
        [Tooltip("Duration for damper effect in ms (0 = infinite)")]
        public float damperDuration = 0f;

        [Header("Tuning")]
        [Range(0f, 2f)] public float overallGain = 0.5f;
        public float updateRateMs = 16f; // ~60Hz

        [Header("Smoothing (Reduces Vibrations)")]
        [Tooltip("Enable force smoothing to reduce vibrations")]
        public bool enableSmoothing = true;

        [Range(0f, 1f)]
        [Tooltip("Higher = smoother but less responsive. 0 = no smoothing, 0.5 = balanced, 0.9 = very smooth")]
        public float smoothingFactor = 0.3f;

        [Tooltip("Ignore small force changes below this threshold to reduce jitter")]
        [Range(0f, 1000f)]
        public float deadzone = 0f;

        [Tooltip("Maximum force change per frame (limits sudden spikes)")]
        [Range(0f, 5000f)]
        public float maxForceChangePerFrame = 2000f;

        [Header("Debug")]
        public bool showDebugGUI = true;
        public bool logForces = true;

        

        [Header("UDP Sender (Optional)")]
        [Tooltip("Reference to FFBUdpSender component. If null, you must implement SendFFB yourself.")]
        UDPClient udp;

        public AnimationCurve dampingVsSpeed = new AnimationCurve(
            new Keyframe(0f, 1.0f),    // Heavy at standstill
            new Keyframe(0.15f, 0.7f), // Still heavy at parking speeds
            new Keyframe(0.5f, 0.3f),  // Medium at city speeds
            new Keyframe(1f, 0.1f)     // Light at highway speeds
        );
        public float maxSpeed = 100f;
        public AnimationCurve centerWheelCheckCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.025f, 0.15f),
            new Keyframe(0.05f, 0.75f),
            new Keyframe(1f, 1f)
        );
        public AnimationCurve lateralLoadVSSpeedCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.1f, 1f)
        );


        private WheelPair[] steeringPairs; // Cached steering axles
        private WheelPair[] nonSteeringPairs; // Cached non-steering axles
        private bool isInitialized = false;

        internal Aircraft aircraft;

        // Smoothing variables
        private float previousForce = 0f;
        private float smoothedForce = 0f;
        private float previousFriction = 0f;

        #region Runtime Configuration (for BepInEx plugins)

        /// <summary>
        /// Add a wheel pair at runtime. Call this from your BepInEx plugin.
        /// </summary>
        /// <param name="name">Display name for this axle</param>
        /// <param name="leftWheel">Left wheel collider (can be null for single wheels)</param>
        /// <param name="rightWheel">Right wheel collider (can be null for single wheels)</param>
        /// <param name="steeringInfluence">0.0 = non-steering, 1.0 = full steering influence</param>
        public void AddWheelPair(string name, WheelCollider leftWheel, WheelCollider rightWheel, float steeringInfluence)
        {
            var pair = new WheelPair
            {
                name = name,
                leftWheel = leftWheel,
                rightWheel = rightWheel,
                steeringInfluence = steeringInfluence
            };
            wheelPairs.Add(pair);

            Plugin.Logger?.LogDebug($"WheelColliderFFB: Added wheel pair '{name}' (Steering: {steeringInfluence})");
        }

        /// <summary>
        /// Clear all wheel pairs. Use this before reconfiguring.
        /// </summary>
        public void ClearWheelPairs()
        {
            wheelPairs.Clear();
            isInitialized = false;
            Plugin.Logger?.LogDebug("WheelColliderFFB: Cleared all wheel pairs");
        }

        /// <summary>
        /// Initialize the FFB system after adding wheel pairs.
        /// Call this after you've added all your wheel pairs.
        /// </summary>
        public void Initialize(Aircraft aircraft)
        {
            Plugin.Logger?.LogDebug($"WheelFFB INIT!");

            udp = new UDPClient(5001);
            ConfigureWheelsAutomatically(aircraft);
            if (wheelPairs == null || wheelPairs.Count == 0)
            {
                Plugin.Logger?.LogError("WheelColliderFFB: Cannot initialize - no wheel pairs configured!");
                return;
            }
            ValidateSetup();
            CacheWheelGroups();
            isInitialized = true;

            Plugin.Logger?.LogDebug($"WheelColliderFFB: Initialized with {wheelPairs.Count} wheel pairs");
        }

        void Awake()
        {

        }

        /// <summary>
        /// Check if the FFB system is ready to use
        /// </summary>
        public bool IsInitialized => isInitialized;

        #endregion

        void FixedUpdate()
        {
            
            // Don't run if not initialized
            if (!isInitialized || wheelPairs == null || wheelPairs.Count == 0)
                return;
            float totalInfluence = 0f;
            // Update all wheel hit data
            foreach (var pair in wheelPairs)
            {
                pair.UpdateHits();
                pair.UpdateSteeringInfluence();
                totalInfluence += pair.steeringInfluence;
            }
            Plugin.Logger.LogDebug($"TOTAL INFLUENCE: {totalInfluence}");
            // Calculate total force feedback
            float[] rawForces = CalculateTotalForce();

            // Apply smoothing if enabled
            float finalForce = rawForces[0] + rawForces[1];
            Plugin.Logger?.LogDebug($"CONSTANTFORCE BREAKDOWN: LATERAL LOAD {rawForces[0]}, SUSPENSION: {rawForces[1]}");
            /*
            if (enableSmoothing)
            {
                finalForce = ApplySmoothing(rawForce);
            }
            */
            // Create CSV message for UDP transmission
            finalForce = finalForce * Configuration.FFB_Gain.Value;
            
            int magnitude = Mathf.Clamp((int)finalForce, -10000, 10000);
            int friction = Mathf.Clamp((int)rawForces[2], 0, 10000);
            int dynamicDamping = Mathf.Clamp((int)rawForces[3], 0, 10000);
            string ffbMessage = $"constantforce2,1,{magnitude},0,0,{dynamicDamping},0,{friction},0";

            // Send main force via UDP
            SendFFB(ffbMessage);
        }

        void ConfigureWheelsAutomatically(Aircraft vehicle)
        {
            WheelCollider[] allWheels = vehicle.GetComponentsInChildren<WheelCollider>();

            if (allWheels.Length == 0)
            {
                Plugin.Logger?.LogError("No WheelColliders found on vehicle!");
                return;
            }

            Plugin.Logger?.LogInfo($"Found {allWheels.Length} wheels, organizing...");

            // Sort wheels by Z position (front to back)
            System.Array.Sort(allWheels, (a, b) =>
                b.transform.position.z.CompareTo(a.transform.position.z));

            // Group wheels into pairs (left/right)
            for (int i = 0; i < allWheels.Length - 1; i += 2)
            {
                WheelCollider wheel1 = allWheels[i];
                WheelCollider wheel2 = allWheels[i + 1];

                // Determine which is left and which is right
                WheelCollider leftWheel = wheel1.transform.position.x < wheel2.transform.position.x ? wheel1 : wheel2;
                WheelCollider rightWheel = wheel1.transform.position.x < wheel2.transform.position.x ? wheel2 : wheel1;

                // First pair (frontmost) is steering, rest are non-steering
                float steeringInfluence = 0f;
                
                string axleName = (i == 0) ? "Front Axle" : $"Rear Axle {i / 2}";

                AddWheelPair(axleName, leftWheel, rightWheel, steeringInfluence);
                Plugin.Logger?.LogInfo($"Added {axleName}: {leftWheel.name} + {rightWheel.name}");
            }

            // Handle odd number of wheels (trike, etc.)
            if (allWheels.Length % 2 != 0)
            {
                WheelCollider singleWheel = allWheels[allWheels.Length - 1];
                bool isFront = singleWheel.transform.position.z > 0;

                if (isFront)
                {
                    AddWheelPair("Front Wheel", null, singleWheel, 1.0f);
                }
                else
                {
                    AddWheelPair("Rear Wheel", singleWheel, null, 0.0f);
                }
                Plugin.Logger?.LogInfo($"Added single wheel: {singleWheel.name}");
            }
        }

        /// <summary>
        /// Validate wheel setup and warn about common issues
        /// </summary>
        void ValidateSetup()
        {
            if (wheelPairs == null || wheelPairs.Count == 0)
            {
                Plugin.Logger?.LogError("WheelColliderFFB: No wheel pairs configured! Add at least one wheel pair.");
                return;
            }

            foreach (var pair in wheelPairs)
            {
                if (pair.leftWheel == null && pair.rightWheel == null)
                {
                    Plugin.Logger?.LogDebug($"WheelColliderFFB: Wheel pair '{pair.name}' has no wheels assigned!");
                }
            }

            bool hasSteeringPair = wheelPairs.Any(p => p.steeringInfluence > 0f);
            if (!hasSteeringPair)
            {
                Plugin.Logger?.LogDebug("WheelColliderFFB: No wheel pairs have steering influence > 0. Set steeringInfluence on front axle(s).");
            }
        }

        /// <summary>
        /// Cache steering and non-steering wheel groups for performance
        /// </summary>
        void CacheWheelGroups()
        {
            steeringPairs = wheelPairs.Where(p => p.steeringInfluence > 0f).ToArray();
            nonSteeringPairs = wheelPairs.Where(p => p.steeringInfluence == 0f).ToArray();

            Plugin.Logger?.LogDebug($"WheelColliderFFB: Configured {wheelPairs.Count} wheel pairs ({steeringPairs.Length} steering, {nonSteeringPairs.Length} non-steering)");
        }

        /// <summary>
        /// Calculate combined force feedback from all effects
        /// </summary>
        private float[] CalculateTotalForce()
        {
            float[] forces = {0f, 0f, 0f, 0f};
            forces[0] = CalculateLateralLoad() * Configuration.FFB_SelfAligningTorque_Gain.Value;
            forces[1] = CalculateBumpSteer() * Configuration.FFB_SuspensionForces_Gain.Value;
            forces[2] = CalculateTyreFriction() * Configuration.FFB_TyreFriction_Gain.Value;
            forces[3] = CalculateDynamicDamping() * Configuration.FFB_DynamicDamping_Gain.Value;
            return forces;
        }


        private float CalculateTyreFriction()
        {
            float tyreFriction = 0f;
            float totalSteeringInfluence = 0f;

            
            foreach (var pair in wheelPairs)
            {
                if (!pair.steered) continue;
                if (!pair.AnyGrounded)
                {
                    tyreFriction += 0f;
                }
                else
                {
                    totalSteeringInfluence += 1f;
                    tyreFriction += pair.GetAverageForwardFrictionForce() + pair.GetAverageForwardFrictionForce();
                }

            }

            tyreFriction = Mathf.Abs(tyreFriction / totalSteeringInfluence);

            return Mathf.Clamp(tyreFriction, 0f, 10000f);
        }
        private float CalculateLateralLoad()
        {
            float totalLeftSteered = 0f;
            float totalRightSteered = 0f;
            float totalLeftUnsteered = 0f;
            float totalRightUnsteered = 0f;

            float avgLeftSteered = 0f;
            float avgRightSteered = 0f;
            float avgLeftUnsteered = 0f;
            float avgRightUnsteered = 0f;

            float totalAngle = 0f;
            float avgAngle = 0f;
            float normalizedAngle = 0f;
            float angleEval = 0f;

            float totalSteeredCount = 0f;
            float totalUnsteeredCount = 0f;
            float finalForce = 0f;
            foreach (var pair in wheelPairs)
            {
                if (!pair.BothGrounded) continue;

                

                if (pair.steered)
                {
                    totalAngle += pair.GetSteerAngle();
                    totalSteeredCount += 1f;
                    
                    if (pair.leftGrounded)
                    {
                        totalLeftSteered += pair.leftHit.force - (pair.GetSidewaysFrictionForce(pair.leftWheel) * Configuration.FFB_UnderSteer_Gain.Value);
                    }
                    else
                    {
                        totalLeftSteered += 0f;
                    }
                    if (pair.rightGrounded)
                    {
                        totalRightSteered += pair.rightHit.force - (pair.GetSidewaysFrictionForce(pair.rightWheel) * Configuration.FFB_UnderSteer_Gain.Value);
                    }
                    else
                    {
                        totalRightSteered += 0f;
                    }
                } else
                {
                    totalUnsteeredCount += 1f;
                    if (pair.leftGrounded)
                    {
                        totalLeftUnsteered += pair.leftHit.force;
                    }
                    else
                    {
                        totalLeftUnsteered += 0f;
                    }
                    if (pair.rightGrounded)
                    {
                        totalRightUnsteered += pair.rightHit.force;
                    }
                    else
                    {
                        totalRightUnsteered += 0f;
                    }
                }
            }
            if (totalUnsteeredCount != 0f)
            {
                avgLeftUnsteered = (totalLeftUnsteered / totalUnsteeredCount) * Configuration.FFB_UnsteeredSuspension_Blend.Value;
                avgRightUnsteered = (totalRightUnsteered / totalUnsteeredCount) * Configuration.FFB_UnsteeredSuspension_Blend.Value;
            } else
            {
                avgLeftUnsteered = 0f;
                avgRightUnsteered = 0f;
            }

            if (totalSteeredCount != 0f)
            {
                avgLeftSteered = totalLeftSteered / totalSteeredCount;
                avgRightSteered = totalRightSteered / totalSteeredCount;
            } else
            {
                avgLeftSteered = 0f;
                avgRightSteered = 0f;
            }
            avgAngle = totalAngle / totalSteeredCount;
            normalizedAngle = avgAngle / 57.2958f;
            angleEval = centerWheelCheckCurve.Evaluate(Mathf.Abs(normalizedAngle));
            float speedEval = lateralLoadVSSpeedCurve.Evaluate(Mathf.Clamp(Plugin.barValues["speed"] / maxSpeed, 0f, 1f));
            if (avgAngle < 0f)
            {
                finalForce = -Mathf.Abs(Mathf.Clamp(((avgRightSteered + avgRightUnsteered) - (avgLeftSteered + avgLeftUnsteered)),-10000f,10000f));
                finalForce = finalForce * angleEval * speedEval;
                return finalForce;
            }
            if (avgAngle > 0f)
            {
                finalForce = Mathf.Abs(Mathf.Clamp(((avgLeftSteered + avgLeftUnsteered) - (avgRightSteered + avgRightUnsteered)),-10000f,10000f));
                finalForce = finalForce * angleEval * speedEval;
                return finalForce;
            }
            return 0f;
        }
        private float CalculateDynamicDamping()
        {
            //Plugin.Logger?.LogDebug($"SPEED: {Plugin.barValues["speed"]}");
            try 
            {
                float normalizedSpeed = Mathf.Clamp(Plugin.barValues["speed"] / maxSpeed,0f,1f);

                float speedDamping = (Configuration.FFB_DamperGain.Value + dampingVsSpeed.Evaluate(normalizedSpeed)) * 10000f;
                return Mathf.Clamp(speedDamping, 0f, 10000f);
            }
            catch
            {
                return Configuration.FFB_DamperGain.Value * 10000f;
            }
        }
        /// <summary>
        /// Bump Steer - Calculated from all steering axles
        /// </summary>
        private float CalculateBumpSteer()
        {

            float totalBumpSteer = 0f;
            float totalInfluence = 0f;

            float totalUnsteeredSuspension = 1f;
            float totalUnsteeredInfluence = 1f;

            foreach (var pair in wheelPairs)
            {
                if (!pair.BothGrounded) continue;
                if (pair.steered)
                {
                    // Compression difference
                    float compressionDiff = pair.GetCompressionDifference();

                    // For velocity, we'll use the compression ratio change over time
                    // Store previous compression in a class variable or estimate from spring force
                    // For now, we'll approximate velocity from current compression and spring stiffness

                    Vector3 leftPos, rightPos;
                    Quaternion leftRot, rightRot;
                    pair.leftWheel.GetWorldPose(out leftPos, out leftRot);
                    pair.rightWheel.GetWorldPose(out rightPos, out rightRot);

                    float leftTravel = pair.leftWheel.transform.position.y - leftPos.y;
                    float rightTravel = pair.rightWheel.transform.position.y - rightPos.y;

                    // Estimate velocity from compression and spring rate
                    float leftForce = leftTravel * pair.leftWheel.suspensionSpring.spring;
                    float rightForce = rightTravel * pair.rightWheel.suspensionSpring.spring;
                    float velocityDiff = (leftForce - rightForce) * 0.00001f;

                    // Bump steer force
                    float axleBumpSteer = (compressionDiff * 3000f) + (velocityDiff * 500f);

                    // Weight by steering influence
                    totalBumpSteer += axleBumpSteer;
                    totalInfluence += 1f;
                } else
                {
                    float compressionDiff = pair.GetCompressionDifference();
                    Vector3 leftPos, rightPos;
                    Quaternion leftRot, rightRot;
                    pair.leftWheel.GetWorldPose(out leftPos, out leftRot);
                    pair.rightWheel.GetWorldPose(out rightPos, out rightRot);

                    float leftTravel = pair.leftWheel.transform.position.y - leftPos.y;
                    float rightTravel = pair.rightWheel.transform.position.y - rightPos.y;

                    // Estimate velocity from compression and spring rate
                    float leftForce = leftTravel * pair.leftWheel.suspensionSpring.spring;
                    float rightForce = rightTravel * pair.rightWheel.suspensionSpring.spring;
                    float velocityDiff = (leftForce - rightForce) * 0.00001f;

                    // Bump steer force
                    float axleBumpSteer = (compressionDiff * 3000f) + (velocityDiff * 500f);

                    // Weight by steering influence
                    totalUnsteeredSuspension += axleBumpSteer;
                    totalUnsteeredInfluence += 1f;
                }
            }
            float totalForce = (totalBumpSteer / totalInfluence) + ((totalUnsteeredSuspension * Configuration.FFB_UnsteeredSuspension_Blend.Value) / totalUnsteeredInfluence);
            return Mathf.Clamp(totalInfluence > 0 ? totalBumpSteer / totalInfluence : 0f,-10000f,10000f);
        }

        /// <summary>
        /// Send FFB message via UDP
        /// IMPORTANT: Both ConstantForce AND Damper messages are sent separately
        /// </summary>
        private void SendFFB(string message)
        {
            try
            {
                // If UDP sender component is assigned, use it
                if (udp != null)
                {
                    udp.SendData(message);
                }
                else
                {
                    // TODO: Implement your custom UDP sending here
                    // Example:
                    // udpClient.Send(Encoding.UTF8.GetBytes(message), message.Length);

                    // For debugging only:
                    if (logForces)
                    {
                        Plugin.Logger?.LogDebug($"FFB (no sender): {message}");
                    }
                }
            } catch (Exception e)
            {
                Plugin.Logger?.LogError($"{e.Message}");
            }

        }
    }
}
