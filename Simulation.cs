using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

public class Simulation
{
    public CANBus CANBus { get; private set; }
    public int TimeInterval { get; private set; }
    private int time = 0;
    public bool StopSimulation = false;
    public bool PauseSimulation = false;
    public event Action<int, ECUStatus> OnStatusChanged;

    public Simulation(int timeInterval, RichTextBox TextBox)
    {
        CANBus = new CANBus();
        CANBus.TextBox = TextBox;
        TimeInterval = timeInterval;
    }

    private void SetupECUs(List<ECUConfiguration> ecuConfigs)
    {
        foreach (var ecuC in ecuConfigs)
        {
            // Create an ECU with the provided configuration
            var ecu = new ECU(ecuC.ID, $"ECU {ecuC.ID}", ecuC.Listeners, new Queue<Message>(ecuC.GenerateRandomMessages()));
            CANBus.AddECU(ecu);
        }
    }

    private void RunCycle()
    {

        // Perform CAN bus message transmission
        CANBus.time = time;
        CANBus.TransmitMessage();

        // Wait for the time interval to simulate the cycle delay
        Thread.Sleep(TimeInterval);
    }





    public void StartSimulation(List<ECUConfiguration> ecuConfigs)
    {
        SetupECUs(ecuConfigs);

        var stop = StopCondition();
        while (stop && !StopSimulation)
        {
            while (stop && !StopSimulation && !PauseSimulation)
            {
                RunCycle();
                time += 1;
                stop = StopCondition();
            }

            if (PauseSimulation)
            {
                Console.WriteLine("Pause");
            }
        }

        if (!StopSimulation)
        {
            Console.WriteLine("No more messages, simulation stopped.");
        }
        else
        {
            Console.WriteLine("Simulation stopped by user.");
        }
    }

    public bool StopCondition()
    {
        foreach (ECU ecu in CANBus.ConnectedECUs)
        {
            if (ecu.NextMessage != null)
            {
                return true;
            }
        }
        return false;
    }
}
