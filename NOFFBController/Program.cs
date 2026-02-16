using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NOFFBController;
using NOFFBController.Messages;
using NOFFBMessaging;
using SharpDX;
using SharpDX.DirectInput;

namespace NOFFBController
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Force Feedback Controller Application");
            Console.WriteLine("======================================");
            Console.WriteLine("Starting UDP listener...");

            //using var listener = new UdpChannelListener(port: 5001, capacity: 100);
            //listener.Start();

            FastUdpListener listener = new FastUdpListener(5001);
            var buffer = GC.AllocateUninitializedArray<byte>(65536, pinned: true);

            Console.WriteLine("Listening on UDP port 5001 (Ctrl+C to quit)");

            // Initialize controller
            ForceFeedbackController controller = new ForceFeedbackController();
            if (!controller.Initialize())
            {
                Console.WriteLine("Failed to initialize force feedback controller. Press any key to exit...");
                Console.ReadKey();
                return;
            }
            //controller.ApplyFFBDamper(null);
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Shutting down...");
            };

            while (true)
            {
                var bytesReceived = await listener.ReceiveAsync(buffer);

                ProcessPacket(buffer.AsSpan(0, bytesReceived),controller);
            }
            Console.WriteLine("Exited cleanly.");
        }

        private static void ProcessPacket(ReadOnlySpan<byte> data,ForceFeedbackController controller)
        {
            // Your processing logic here
            try
            {
                string message = Encoding.UTF8.GetString(data);
                Console.WriteLine(message);
                FFBControlMessage msg = FFBControlMessage.FromCsv2(message);
                switch (msg.Type)
                {
                    case "autocenter":
                        controller.FFBAutoCenter(msg);
                        break;
                    case "constantforce":
                        controller.FFBConstantForce(msg);
                        break;
                    case "damper":
                        controller.FFBDamper(msg);
                        break;
                    default:
                        break;
                }
                //controller.ApplyFFBConstantForce(ForceFeedbackMessage.FromCsv(message));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            } 
        }
    }
}
