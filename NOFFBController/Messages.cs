namespace NOFFBController.Messages
{
    /// <summary>
    /// Message containing controller input state (axes and buttons)
    /// </summary>
    public class ControllerInputMessage
    {
        public Dictionary<string, int> Axes { get; set; } = new();
        public Dictionary<int, bool> Buttons { get; set; } = new();
        public long Timestamp { get; set; }

    }

    /// <summary>
    /// Message containing force feedback commands
    /// Format: EffectType,Axis,Magnitude
    /// Example: ConstantForce,1,1000
    /// </summary>
    public class ForceFeedbackMessage
    {
        public string EffectType { get; set; } = "ConstantForce";
        public int Magnitude { get; set; } // -10000 to 10000

        public int DirectionX { get; set; }

        public int DirectionY { get; set; }

        public string ToCsv()
        {
            return $"{EffectType},{Magnitude},{DirectionX},{DirectionY}";
        }

        public static ForceFeedbackMessage? FromCsv(string csv)
        {
            try
            {
                var parts = csv.Trim().Split(',');
                if (parts.Length < 3)
                {
                    Console.WriteLine($"Invalid CSV format: Expected at least 3 fields, got {parts.Length}");
                    return null;
                }

                var message = new ForceFeedbackMessage
                {
                    EffectType = parts[0].Trim(),
                    Magnitude = int.Parse(parts[1].Trim()),
                    DirectionX = int.Parse(parts[2].Trim()),
                    DirectionY = int.Parse(parts[3].Trim())

                };

                return message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing CSV: {ex.Message}");
                return null;
            }
        }

    }
}
