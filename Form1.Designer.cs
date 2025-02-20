using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Linq;
using CAN_Bus_Simulation;

public class CanBusSimulator : Form
{
    private Panel toolbar;
    private Panel simulationArea;
    private Label timeLabel;
    private Timer simulationTimer;

    private int ecuCounter = 0; // Tracks the number of ECUs added
    private int simulationTime = 0; // Tracks the simulation time
    private int timeInterval = 1000; // Default time interval in milliseconds (1 second)
    private Panel configPanel; // Floating panel for ECU configuration
    private List<ECUConfiguration> ecus = new List<ECUConfiguration>(); // List to store ECUs
    private List<Label> labelEcus = new List<Label>(); // Store references to ECU labels
    private Simulation simulation;
    private Button startSimulationButton; // Reference to Start Simulation Button
    private bool start = false;

    private Color firstBusLineColor = Color.White; // Default color for the CAN bus line
    private Color secondBusLineColor = Color.White; // Default color for the CAN bus line
    private int busIndex = 0;
    private Color connectionLineColor_1 = Color.White; // Default color for the ECU connection lines
    private Color connectionLineColor_2 = Color.White; // Default color for the ECU connection lines

    private int firstBusLineY;
    private int secondBusLineY;

    Graphics g;


    public CanBusSimulator()
    {

    }


    public enum CanBusesColor
    {
        SENDING_1,
        SENDING_0,
        ARBITRATION,
        EMPTY
    };

    public enum ECUConnectionState
    {
        RECEIVING,
        SENDING_0,
        SENDING_1,
        ARBITRATING,
        EMPTY,
        PASSIVE
    }


    private RichTextBox messageLog; // Declare the RichTextBox for the log

    public Form InitializeComponents()
    {
        var form = new Form();
        form.Text = "CAN Bus Simulator";
        form.Size = new Size(1000, 600);
        form.BackColor = Color.FromArgb(40, 40, 40); // Dark background

        // Simulation Timer
        simulationTimer = new Timer
        {
            Interval = timeInterval // Set the interval in milliseconds
        };
        simulationTimer.Tick += SimulationTimer_Tick; // Attach the Tick event handler


        // Toolbar
        toolbar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 150,
            BackColor = Color.FromArgb(60, 60, 60) // Darker toolbar
        };
        form.Controls.Add(toolbar);

        // Drag source: ECU representation
        Label ecuDragSource = new Label
        {
            Text = "Drag ECU",
            Font = new Font("Arial", 10, FontStyle.Bold),
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(100, 40),
            Location = new Point(25, 25)
        };
        ecuDragSource.MouseDown += EcuDragSource_MouseDown;
        toolbar.Controls.Add(ecuDragSource);

        Label testDragSource = new Label
        {
            Text = "Test1 ECU",
            Font = new Font("Arial", 10, FontStyle.Bold),
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(100, 40),
            Location = new Point(25, 100)
        };
        testDragSource.MouseDown += Test1DragSource_MouseDown;
        toolbar.Controls.Add(testDragSource);


        Label test2DragSource = new Label
        {
            Text = "Test2 ECU",
            Font = new Font("Arial", 10, FontStyle.Bold),
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(100, 40),
            Location = new Point(25, 175)
        };
        test2DragSource.MouseDown += Test2DragSource_MouseDown;
        toolbar.Controls.Add(test2DragSource);


        // Simulation area
        simulationArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30) // Dark simulation area
        };

        firstBusLineY = 150; // First bus line at 1/3 of the height
        secondBusLineY = 200; // Second bus line at 2/3 of the height

        simulationArea.Paint += SimulationArea_Paint; // Paint CAN bus line
        simulationArea.AllowDrop = true;
        simulationArea.DragEnter += SimulationArea_DragEnter;
        simulationArea.DragDrop += SimulationArea_DragDrop;
        form.Controls.Add(simulationArea);


        //Console.WriteLine($"First bus line Y position: {firstBusLineY}");
        //Console.WriteLine($"Second bus line Y position: {secondBusLineY}");

        // print first bus and second bus line

        // Message log (RichTextBox)
        messageLog = new RichTextBox
        {
            Dock = DockStyle.Bottom,
            Height = 150, // Adjust height as needed
            ReadOnly = true,
            BackColor = Color.Black,
            ForeColor = Color.White,
            Font = new Font("Consolas", 10), // Monospaced font for better alignment
            ScrollBars = RichTextBoxScrollBars.Vertical
        };
        form.Controls.Add(messageLog);

        // Start/Pause Simulation Button
        startSimulationButton = new Button
        {
            Text = "Start Simulation",
            Dock = DockStyle.Bottom,
            Height = 40,
            ForeColor = Color.Black,
            BackColor = Color.White
        };
        startSimulationButton.Click += StartSimulationButton_Click;
        form.Controls.Add(startSimulationButton);

        // Floating configuration panel (hidden initially)
        configPanel = new Panel
        {
            Size = new Size(300, 250),
            BackColor = Color.FromArgb(60, 60, 60), // Dark config panel
            Visible = false,
            BorderStyle = BorderStyle.FixedSingle
        };
        form.Controls.Add(configPanel);

        return form;
    }


    // Draw the CAN bus line and ECU connections in the simulation area
    private void SimulationArea_Paint(object sender, PaintEventArgs e)
    {
        g = e.Graphics;

        // Draw the first bus line
        using (Pen busPen = new Pen(firstBusLineColor, 3))
        {
            g.DrawLine(busPen, 10, firstBusLineY, simulationArea.Width - 10, firstBusLineY);
        }

        // Draw the second bus line
        using (Pen busPen = new Pen(secondBusLineColor, 3))
        {
            g.DrawLine(busPen, 10, secondBusLineY, simulationArea.Width - 10, secondBusLineY);
        }

        // Draw connections for all ECUs to both bus lines
        foreach (var ecu in labelEcus)
        {
            int ecuCenterX = ecu.Left + ecu.Width / 2;
            int ecuBottomY = ecu.Top + ecu.Height;

            int ecuId = (ecu.Tag as ECUConfiguration)?.ID ?? -1;
            if (ecuId >= 0 && ecuConnectionColors.ContainsKey(ecuId))
            {
                var (color1, color2) = ecuConnectionColors[ecuId];

                // Draw connection to the first bus line
                using (Pen connectionPen = new Pen(color1, 2))
                {
                    g.DrawLine(connectionPen, ecuCenterX - 20, ecuBottomY, ecuCenterX - 20, firstBusLineY);
                }

                // Draw connection to the second bus line
                using (Pen connectionPen = new Pen(color2, 2))
                {
                    g.DrawLine(connectionPen, ecuCenterX + 20, ecuBottomY, ecuCenterX + 20, secondBusLineY);
                }
            }
        }
    }




    // Handles drag-and-drop from the ECU source
    private void EcuDragSource_MouseDown(object sender, MouseEventArgs e)
    {
        Label ecu = sender as Label;
        if (ecu != null)
        {
            DoDragDrop(ecu.Text, DragDropEffects.Copy);
        }
    }

    private void Test1DragSource_MouseDown(object sender, MouseEventArgs e)
    {
        // Define the default configurations for 3 ECUs
        var defaultConfigs = new[]
        {
            new ECUConfiguration
            {
                ID = ecuCounter,
                Frequency = "60 messages/min",
                ErrorRate = "1/1",
                MinPriority = 0,
                MaxPriority = 512,
                Listeners = new List<int>() // Default listeners will be populated after all ECUs are added
            },
            new ECUConfiguration
            {
                ID = ecuCounter + 1,
                Frequency = "25 messages/min",
                ErrorRate = "1/10",
                MinPriority = 513,
                MaxPriority = 1024,
                Listeners = new List<int>()
            },
            new ECUConfiguration
            {
                ID = ecuCounter + 2,
                Frequency = "60 messages/min",
                ErrorRate = "1/25",
                MinPriority = 1025,
                MaxPriority = 2047,
                Listeners = new List<int>()
            }
        };

        foreach (var config in defaultConfigs)
        {
            ecus.Add(config);

            // Create ECU representation
            Label ecuRepresentation = new Label
            {
                Text = $"ECU {config.ID}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(100, 40),
                Location = new Point(300 + (labelEcus.Count * 120), 100) // Auto-arrange horizontally
            };

            ecuRepresentation.Tag = config;
            simulationArea.Controls.Add(ecuRepresentation);
            labelEcus.Add(ecuRepresentation);

            // Initialize connection colors for the ECU
            ecuConnectionColors[config.ID] = (Color.White, Color.White);

            ecuRepresentation.Click += EcuRepresentation_Click; // Attach click handler for configuration
        }

        // Update listeners for each ECU to include the other two
        foreach (var config in defaultConfigs)
        {
            config.Listeners = defaultConfigs.Where(c => c.ID != config.ID).Select(c => c.ID).ToList();
        }

        ecuCounter += 3; // Increment ECU counter
        simulationArea.Invalidate(); // Redraw simulation area
    }

    private void Test2DragSource_MouseDown(object sender, MouseEventArgs e)
    {
        // Define a new set of configurations for testing higher message frequencies and dynamic listeners
        var dynamicConfigs = new[]
        {
        new ECUConfiguration
        {
            ID = ecuCounter,
            Frequency = "120 messages/min", // Higher frequency
            ErrorRate = "1/5", // Moderate error rate
            MinPriority = 0,
            MaxPriority = 512,
            Listeners = new List<int>() // Listeners will be added dynamically
        },
        new ECUConfiguration
        {
            ID = ecuCounter + 1,
            Frequency = "40 messages/min", // Lower frequency
            ErrorRate = "1/15", // Lower error rate
            MinPriority = 513,
            MaxPriority = 1024,
            Listeners = new List<int>()
        },
        new ECUConfiguration
        {
            ID = ecuCounter + 2,
            Frequency = "80 messages/min", // Medium frequency
            ErrorRate = "1/20", // Very low error rate
            MinPriority = 1025,
            MaxPriority = 2047,
            Listeners = new List<int>()
        }
    };

        foreach (var config in dynamicConfigs)
        {
            ecus.Add(config);

            // Create ECU representation
            Label ecuRepresentation = new Label
            {
                Text = $"ECU {config.ID}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightBlue, // Different color to distinguish this test
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(100, 40),
                Location = new Point(300 + (labelEcus.Count * 120), 200) // Auto-arrange horizontally in a lower row
            };

            ecuRepresentation.Tag = config;
            simulationArea.Controls.Add(ecuRepresentation);
            labelEcus.Add(ecuRepresentation);

            // Initialize connection colors for the ECU
            ecuConnectionColors[config.ID] = (Color.LightBlue, Color.White);

            ecuRepresentation.Click += EcuRepresentation_Click; // Attach click handler for configuration
        }

        // Assign dynamic listeners
        dynamicConfigs[0].Listeners.Add(dynamicConfigs[1].ID); // ECU0 listens to ECU1
        dynamicConfigs[1].Listeners.Add(dynamicConfigs[2].ID); // ECU1 listens to ECU2
        dynamicConfigs[2].Listeners.Add(dynamicConfigs[0].ID); // ECU2 listens to ECU3

    }


    private void SimulationArea_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.Text))
        {
            e.Effect = DragDropEffects.Copy;
        }
        else
        {
            e.Effect = DragDropEffects.None;
        }
    }

    private Dictionary<int, (Color connectionLineColor1, Color connectionLineColor2)> ecuConnectionColors = new Dictionary<int, (Color, Color)>();

    private void SimulationArea_DragDrop(object sender, DragEventArgs e)
    {
        Point dropLocation = simulationArea.PointToClient(new Point(e.X, e.Y));

        var newEcu = new ECUConfiguration
        {
            ID = ecuCounter,
            Frequency = "10 messages/min",
            ErrorRate = "1/25",
            MinPriority = 0,
            MaxPriority = 2047
        };

        ecus.Add(newEcu);

        Label ecuRepresentation = new Label
        {
            Text = $"ECU {ecuCounter++}",
            Font = new Font("Arial", 10, FontStyle.Bold),
            BackColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(100, 40),
            Location = new Point(dropLocation.X - 50, dropLocation.Y - 20)
        };

        ecuRepresentation.Tag = newEcu;
        simulationArea.Controls.Add(ecuRepresentation);
        labelEcus.Add(ecuRepresentation);

        // Initialize connection colors for the new ECU
        ecuConnectionColors[newEcu.ID] = (Color.White, Color.White);

        simulationArea.Invalidate();
        ecuRepresentation.Click += EcuRepresentation_Click;
    }

    // Display the configuration panel when an ECU is clicked
    private void EcuRepresentation_Click(object sender, EventArgs e)
    {
        Label ecu = sender as Label;
        if (ecu != null)
        {
            int panelX = ecu.Location.X + ecu.Width + 10;
            int panelY = ecu.Location.Y;

            // Increase the height of the configuration panel
            configPanel.Size = new Size(300, 450); // Adjust width and height as needed

            // Adjust position if the panel exceeds screen bounds
            if (panelX + configPanel.Width > simulationArea.Width)
            {
                panelX = ecu.Location.X - configPanel.Width - 10; // Move to the left
            }
            if (panelY + configPanel.Height > simulationArea.Height)
            {
                panelY = simulationArea.Height - configPanel.Height - 10; // Move up to avoid overlap
            }
            if (panelY < 0)
            {
                panelY = 10; // Move down to avoid going out of the top boundary
            }

            configPanel.Location = new Point(panelX, panelY);
            configPanel.BringToFront();
            configPanel.Visible = true;

            // Populate the configuration panel with current values
            configPanel.Controls.Clear();

            Label title = new Label
            {
                Text = $"Configure {ecu.Text}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White
            };
            configPanel.Controls.Add(title);

            Label frequencyLabel = new Label
            {
                Text = "Messages/min:",
                Location = new Point(10, 40),
                AutoSize = true,
                ForeColor = Color.White
            };
            configPanel.Controls.Add(frequencyLabel);

            ComboBox frequencyDropdown = new ComboBox
            {
                Location = new Point(150, 40),
                Width = 100,
                Items = { "1 messages/min", "5 messages/min", "10 messages/min", "25 messages/min", "40 messages/min", "60 messages/min" },
                SelectedItem = (ecu.Tag as ECUConfiguration)?.Frequency ?? "10 messages/min"
            };
            configPanel.Controls.Add(frequencyDropdown);

            Label errorRateLabel = new Label
            {
                Text = "Errors/messages:",
                Location = new Point(10, 80),
                AutoSize = true,
                ForeColor = Color.White
            };
            configPanel.Controls.Add(errorRateLabel);

            ComboBox errorRateDropdown = new ComboBox
            {
                Location = new Point(150, 80),
                Width = 100,
                Items = { "1/60", "1/40", "1/25", "1/10", "1/5", "1/1" },
                SelectedItem = (ecu.Tag as ECUConfiguration)?.ErrorRate ?? "1/25"
            };
            configPanel.Controls.Add(errorRateDropdown);

            Label priorityMinLabel = new Label
            {
                Text = "Min Priority:",
                Location = new Point(10, 120),
                AutoSize = true,
                ForeColor = Color.White
            };
            configPanel.Controls.Add(priorityMinLabel);

            NumericUpDown minPriorityInput = new NumericUpDown
            {
                Location = new Point(150, 120),
                Width = 100,
                Minimum = 0,
                Maximum = 2048,
                Value = (ecu.Tag as ECUConfiguration)?.MinPriority ?? 0
            };
            configPanel.Controls.Add(minPriorityInput);

            Label priorityMaxLabel = new Label
            {
                Text = "Max Priority:",
                Location = new Point(10, 160),
                AutoSize = true,
                ForeColor = Color.White
            };
            configPanel.Controls.Add(priorityMaxLabel);

            NumericUpDown maxPriorityInput = new NumericUpDown
            {
                Location = new Point(150, 160),
                Width = 100,
                Minimum = 0,
                Maximum = 2047,
                Value = (ecu.Tag as ECUConfiguration)?.MaxPriority ?? 2047
            };
            configPanel.Controls.Add(maxPriorityInput);

            Label listenersLabel = new Label
            {
                Text = "Listeners:",
                Location = new Point(10, 200),
                AutoSize = true,
                ForeColor = Color.White
            };
            configPanel.Controls.Add(listenersLabel);

            CheckedListBox listenersCheckedListBox = new CheckedListBox
            {
                Location = new Point(150, 200),
                Width = 100,
                Height = 200 // Increased height to accommodate more items
            };

            // Add items to the CheckedListBox, excluding the current ECU
            foreach (var key in ecus)
            {
                if (key.ID != (ecu.Tag as ECUConfiguration)?.ID)
                {
                    listenersCheckedListBox.Items.Add(key.ID);
                }
            }

            // Pre-select the current listeners
            ECUConfiguration ecuConfig = ecu.Tag as ECUConfiguration;
            if (ecuConfig?.Listeners != null)
            {
                foreach (var listener in ecuConfig.Listeners)
                {
                    int index = listenersCheckedListBox.Items.IndexOf(listener);
                    if (index >= 0)
                    {
                        listenersCheckedListBox.SetItemChecked(index, true);
                    }
                }
            }
            configPanel.Controls.Add(listenersCheckedListBox);

            Button saveButton = new Button
            {
                Text = "Save",
                Dock = DockStyle.Bottom,
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            saveButton.Click += (s, args) =>
            {
                ECUConfiguration config = ecu.Tag as ECUConfiguration;
                if (config != null)
                {
                    config.Frequency = frequencyDropdown.SelectedItem?.ToString();
                    config.ErrorRate = errorRateDropdown.SelectedItem?.ToString();
                    config.MinPriority = (int)minPriorityInput.Value;
                    config.MaxPriority = (int)maxPriorityInput.Value;
                    config.Listeners = listenersCheckedListBox.CheckedItems.Cast<int>().ToList();
                }
                configPanel.Visible = false;
                simulationArea.Invalidate(); // Force redraw of simulation area
            };
            configPanel.Controls.Add(saveButton);
        }
    }


    // Handle simulation timer tick
    private void SimulationTimer_Tick(object sender, EventArgs e)
    {
        simulationTime++;
        timeLabel.Text = $"Time: {simulationTime}s";
    }

    private void StartSimulationButton_Click(object sender, EventArgs e)
    {
        if (simulationTimer.Enabled)
        {
            // Pause simulation
            simulationTimer.Stop();
            simulation.PauseSimulation = true;
            startSimulationButton.Text = "Start Simulation";
        }
        else
        {
            // Start simulation
            if (!start)
            {
                simulation = new Simulation(timeInterval, messageLog);

                simulation.StartSimulation(ecus);
                start = true;
            }

            simulation.PauseSimulation = false;
            simulationTimer.Start();
            startSimulationButton.Text = "Pause Simulation";
        }
    }


    private void UpdateSimulationTime(int time)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdateSimulationTime(time)));
            return;
        }
        timeLabel.Text = $"Time: {time}s";
        simulationArea.Invalidate(); // Force redraw
    }



    private void LogMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => LogMessage(message)));
            return;
        }

        messageLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
        messageLog.ScrollToCaret();
    }


    public void ChangeBusLineColor(CanBusesColor casee)
    {
        switch (casee)
        {
            case CanBusesColor.SENDING_1:
                        firstBusLineColor = Color.Green;
                        secondBusLineColor = Color.White;
                    break;
            case CanBusesColor.SENDING_0:
                        firstBusLineColor = Color.Yellow;
                        secondBusLineColor = Color.Yellow;
                    break;
            case CanBusesColor.ARBITRATION:
                        firstBusLineColor = Color.Red;
                        secondBusLineColor = Color.Red;
                    break;
            case CanBusesColor.EMPTY:
                        firstBusLineColor = Color.White;
                        secondBusLineColor = Color.White;
                    break;
            default:
                break;
        }
            simulationArea.Invalidate(); // Force redraw
    }
    public void ChangeEcuConnectionColor(int ecuId, ECUConnectionState state)
    {
        if (!ecuConnectionColors.ContainsKey(ecuId)) return;

        switch (state)
        {
            case ECUConnectionState.RECEIVING:
                ecuConnectionColors[ecuId] = (Color.Blue, Color.Blue);
                break;
            case ECUConnectionState.SENDING_1:
                ecuConnectionColors[ecuId] = (Color.Green, Color.White);
                break;
            case ECUConnectionState.SENDING_0:
                ecuConnectionColors[ecuId] = (Color.Yellow, Color.Yellow);
                break;
            case ECUConnectionState.ARBITRATING:
                ecuConnectionColors[ecuId] = (Color.Red, Color.Red);
                break;
            case ECUConnectionState.EMPTY:
                ecuConnectionColors[ecuId] = (Color.White, Color.White);
                break;
            case ECUConnectionState.PASSIVE:
                ecuConnectionColors[ecuId] = (Color.FromArgb(40, 40, 40), Color.FromArgb(40, 40, 40));
                break;
            default:
                break;
        }

        simulationArea.Invalidate(); // Force redraw to apply the changes
    }


}

