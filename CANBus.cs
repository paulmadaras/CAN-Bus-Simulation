using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

public class CANBus
{
    private const int maxTime = 100;

    public List<ECU> ConnectedECUs { get; private set; } = new List<ECU>();
    public Message MessageOnBus { get; set; }

    public RichTextBox TextBox { get; set; }
    private int delay = 1000;
    private int delay500 = 500;
    private int errorLimit = 1;
    public int time { get; set; }


    public void AddECU(ECU ecu)
    {
        ConnectedECUs.Add(ecu);
        AppendTextWithDelay($"ECU{ecu.ID} connected to CAN bus.\n", delay);
    }

    public void TransmitMessage()
    {
        TextBox.Clear();
        AppendTextWithDelay($"time: {time}\n", delay);
        ECU ecuToTransmit = null;
        var minPriority = 3000;
        bool arb = false;

        // Arbitration logic 
        foreach (ECU ecu in ConnectedECUs)
        {
            if ((ecu.NextMessage?.Time ?? maxTime) <= time)
            {
                AppendTextWithDelay(ecu.EnterArbitrationMode(), delay);
                if (!arb)
                {
                    Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.ARBITRATION);
                    arb = true;
                }
            }
        }

        foreach (ECU ecu in ConnectedECUs)
        {
            if (ecu.Status == ECUStatus.Arbitration)
            {
                AppendTextWithDelay($"ecu{ecu.ID} Start of frame: {ecu.NextMessage.SOF}\n", delay);
            }
        }

        int delay1 = delay;
        int bitIndex;
        // Compare bits one by one
        for (bitIndex = 0; bitIndex < 11; bitIndex++)
        {
            ClearWithTime();
            int cont = 0;
            ECU lastInArb = null;
            foreach (ECU ecu in ConnectedECUs)
            {
                if (ecu.Status == ECUStatus.Arbitration)
                {
                    AppendTextWithDelay($"ecu{ecu.ID} Arbitration ID: {ecu.NextMessage.ArbitrationId.Substring(0, bitIndex + 1)}\n", delay1);
                    lastInArb = ecu;
                    cont += 1;
                }
            }

            if (cont == 1)
            {

                AppendTextWithDelay($"ecu{lastInArb.ID} won arbitration at bit {bitIndex + 1}\n", delay1 + 500);
                ecuToTransmit = lastInArb;
                break;
            }

            ecuToTransmit = null;

            foreach (ECU ecu in ConnectedECUs)
            {
                if (ecu.Status == ECUStatus.Arbitration)
                {
                    if (ecu.NextMessage.ArbitrationId[bitIndex] == '0')
                    {
                        ecuToTransmit = ecu;
                    }
                }
            }

            if (ecuToTransmit != null)
            {
                foreach (ECU ecu in ConnectedECUs)
                {
                    if (ecu.Status == ECUStatus.Arbitration && ecu != ecuToTransmit)
                    {
                        if (ecu.NextMessage.ArbitrationId[bitIndex] == '1')
                        {
                            AppendTextWithDelay(ecu.EnterIdleMode(), delay1);
                        }
                    }
                }
            }
            AppendTextWithDelay("", delay);
            delay1 = 0;
        }

        if (ecuToTransmit != null)
        {
            AppendTextWithDelay(ecuToTransmit.EnterSendingMode(), delay1);

            foreach (ECU ecu in ConnectedECUs)
            {
                if (ecu != ecuToTransmit && ecuToTransmit.Listeners != null)
                {
                    if (ecuToTransmit.Listeners.Contains(ecu.ID))
                    {
                        AppendTextWithDelay(ecu.EnterReceivingMode(), delay);
                    }
                }
            }

            DoBitsBeforeACK(ecuToTransmit, bitIndex);

            ecuToTransmit.SendMessage(this);
            bool transmissionReceived = false;

            if (ecuToTransmit.Listeners != null)
            {
                transmissionReceived = false;

                foreach (ECU ecu in ConnectedECUs)
                {
                    if (ecuToTransmit.Listeners.Contains(ecu.ID))
                    {
                        var receive = ecu.ReceiveMessage(this);
                        AppendTextWithDelay(receive, 2 * delay);
                        if (receive.Contains(": 0"))
                        {
                            transmissionReceived = true;
                        }
                    }
                }
            }

            DoBitsAfterACK(ecuToTransmit);

            if (transmissionReceived)
            {
                ecuToTransmit.RemoveMessage();
                AppendTextWithDelay("Transmision successful.\n", 5 * delay);
            }
            else
            {
                ecuToTransmit.TE += 1;
                AppendTextWithDelay("Transmision failed, no one acknowledged the message.\n", 5 * delay);
                ecuToTransmit.ValidateMessage();
            }

            if (ecuToTransmit.TE >= errorLimit)
            {
                AppendTextWithDelay(ecuToTransmit.EnterPassiveMode(), 5*delay);
                ConnectedECUs.Remove(ecuToTransmit);
            }

            // Reset all ECUs to Idle after the message transmission
            foreach (ECU ecu in ConnectedECUs)
            {
                if (ecu.Status != ECUStatus.Idle)
                { 
                    AppendTextWithDelay(ecu.EnterIdleMode(), delay);
                }
            }
            Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.EMPTY); 
        }
    }

    public void AppendTextWithDelay(string text, int delay)
    {
            TextBox.Invoke((MethodInvoker)(() => TextBox.AppendText(text)));
            Application.DoEvents(); // Ensures UI updates immediately
            Thread.Sleep(delay);    // Adds a delay for real-time effect
    }

    public void AppendCharacthersWithDelay(ECU ecu, string text, int delay)
    {
        foreach (char c in text)
        {
            if(c == '0')
            {
                ecu.Sending0();
                Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.SENDING_0);
            }
            else
            {
                ecu.Sending1();
                Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.SENDING_1);
            }
            TextBox.Invoke((MethodInvoker)(() => TextBox.AppendText(c.ToString())));
            Application.DoEvents(); // Ensures UI updates immediately
            Thread.Sleep(delay);    // Adds a delay for real-time effect
        }
    }

    public void ClearWithTime()
    {
        TextBox.Clear();
        AppendTextWithDelay($"time: {time}\n", 0);

    }

    public void DoBitsBeforeACK(ECU ecu, int pos)
    {

        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} Arbitration ID: {ecu.NextMessage.ArbitrationId.Substring(0, pos + 1)}", 0);
        AppendCharacthersWithDelay(ecu, ecu.NextMessage.ArbitrationId.Substring(pos + 1, 10 - pos), delay500);
        ClearWithTime();
        if (ecu.NextMessage.SB != "") {
            AppendTextWithDelay($"ecu{ecu.ID} Stuff Bit: {ecu.NextMessage.SB}", delay);
            ClearWithTime();
        }
        AppendTextWithDelay($"ecu{ecu.ID} Remote Transmission Request: {ecu.NextMessage.RTR}", delay);
        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} Identifier Extension: {ecu.NextMessage.IDE}", delay);
        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} reserved: {ecu.NextMessage.r0}", delay);
        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} Data: ", 0);
        AppendCharacthersWithDelay(ecu, $"{ecu.NextMessage.DataString}", delay500/5);
        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} Cyclic Redundancy Check: ", 0);
        AppendCharacthersWithDelay(ecu, $"{ecu.NextMessage.CRC}", delay500/5);
        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} Acknowledgement: ", 0);
        AppendCharacthersWithDelay(ecu, $"{ecu.NextMessage.ACK}", delay500);
        ClearWithTime();
    }

    public void DoBitsAfterACK(ECU ecu)
    {

        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} End of frame: ", 0);
        AppendCharacthersWithDelay(ecu, $"{ecu.NextMessage.EOF}", delay500 / 5);
        ClearWithTime();
        AppendTextWithDelay($"ecu{ecu.ID} Interframe spacing: ", 0);
        AppendCharacthersWithDelay(ecu, $"{ecu.NextMessage.IFS}\n", delay500 / 5);
    }
}
