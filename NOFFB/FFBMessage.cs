using System;
using System.Collections.Generic;
using System.Text;

namespace NOFFB
{
    public class FFBMessage
    {
        public string EffectType { get; set; } = "ConstantForce";
        public int Axis { get; set; } // -10000 to 10000
        public int Magnitude { get; set; } // milliseconds
        public static FFBMessage? FromCsv(string csv)
        {
            try
            {
                var parts = csv.Trim().Split(',');
                if (parts.Length < 3)
                {
                    Console.WriteLine($"Invalid CSV format: Expected at least 3 fields, got {parts.Length}");
                    return null;
                }

                var message = new FFBMessage
                {
                    EffectType = parts[0].Trim(),
                    Axis = int.Parse(parts[1].Trim()),
                    Magnitude = int.Parse(parts[2].Trim())
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
