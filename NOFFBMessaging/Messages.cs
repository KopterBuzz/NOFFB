using System;
using SharpDX;
using System.Linq;
using System.Globalization;


namespace NOFFBMessaging
{

    public class FFBControlMessage
    {
        /*
        TYPES:
        autocenter
        constantforce
        damper
        vibration
        periodicx
        periodicy
        */
        public string Type { get; set; }
        public int[] Values { get; set; }

        public FFBControlMessage(string Type, int[] Values)
        {
            this.Type = Type;
            this.Values = Values;
        }
        public override string ToString()
        {
            string _string = $"{Type},";
            _string+= string.Join(",", Values.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            return _string;
        }

        public static FFBControlMessage FromCsv2(string csv)
        {
            try
            {
                string[] parts = csv.Trim().Split(',');
                string type = parts[0];

                int[] values = parts[1..].Select(int.Parse).ToArray();

                FFBControlMessage message = new FFBControlMessage(type, values);

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
/*
IDEAL STRUCTURE:

AUTOCENTER:
Type : autocenter
Values[0]: 0/1 for on-off

CONSTANTFORCE:
Type : constantforce
Values[0]: Axis Number - 1 or 2
Values[1]: Magnitude
Values[2]: Dir X
Values[3]: Dir y

PERIODIC_X:
Type : periodicx
Values[0]: sample rate
Values[1]: amplitude

PERIODIC_Y:
Type : periodicy
Values[0]: sample rate
Values[1]: amplitude

DAMPER:
Type : damper
Values[0]: Coefficient strength

FRICTION:
Type : friction
Values[0]: Coefficient strength

*/