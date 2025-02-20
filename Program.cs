using System;
using System.Collections.Generic;

public class Program
{
    public static CanBusSimulator can;
    [STAThread] // This ensures the main thread runs in STA mode
    static void Main(string[] args)
    {
        // Define ECU configurations (name, listeners, and messages with Time, Data, Priority)
        var ecuConfigs = new List<(string name, List<int> listeners, Queue<Message> messages)>
        {
            // EngineControl ECU
            (
                "EngineControl",
                new List<int> { 1, 2, 3 },
                new Queue<Message>(new[]
                {
                    new Message(1, "Engine status: OK", 2),  // Time = 1, Data = "Engine status: OK", Priority = 2
                    new Message(5, "Engine overheating warning", 1),
                    new Message(10, "Oil pressure low", 1),  
                    new Message(15, "Engine load: High", 2) 
                })
            ),

            // TransmissionControl ECU
            (
                "TransmissionControl",
                new List<int> { 0, 2 }, 
                new Queue<Message>(new[]
                {
                    new Message(3, "Transmission status: Stable", 3), // Priority = 3
                    new Message(8, "Transmission fluid low", 2),  
                    new Message(12, "Gear shifting abnormal", 1)  
                })
            ),

            // BrakeControl ECU
            (
                "BrakeControl",
                new List<int> { 0, 1, 3 },
                new Queue<Message>(new[]
                {
                    new Message(1, "Brake system: Normal", 2), // Priority = 2
                    new Message(2, "Brake system: Normal", 2),
                    new Message(6, "Brake fluid low warning", 1),
                    new Message(9, "ABS engaged", 3), 
                    new Message(14, "Brake pads wear warning", 1) 
                })
            ),

            // ClimateControl ECU 
            (
                "ClimateControl",
                new List<int> { 0, 2 }, 
                new Queue<Message>(new[]
                {
                    new Message(4, "Cabin temperature: Normal", 3), // Priority = 3
                    new Message(7, "AC system: Low refrigerant", 2), 
                    new Message(11, "Cabin air filter replacement needed", 1)  
                })
            ),

            // InfotainmentSystem ECU 
            (
                "InfotainmentSystem",
                new List<int> { 1, 3 }, 
                new Queue<Message>(new[]
                {
                    new Message(5, "Navigation system active", 3), // Priority = 3
                    new Message(13, "Update available for system", 2), 
                    new Message(17, "Traffic alert: Slowdown ahead", 1)  
                })
            )
        };


        // Initialize the simulation
        //Simulation simulation = new Simulation(timeInterval: 1000); // Time interval = 1 second

        // Start the simulation
        //simulation.StartSimulation(ecuConfigs);

        // Launch the GUI
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        can = new CanBusSimulator();
        System.Windows.Forms.Application.Run(can.InitializeComponents());

        Console.Read();
    }



}
