using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using CAN_Bus_Simulation;

public enum ECUStatus
{
    Sending,
    Receiving,
    Arbitration,
    Idle
}

public class ECU
{
    public int ID { get; set; }
    public string Name { get; private set; }
    private Queue<Message> MessageQueue = new Queue<Message>();
    public Message NextMessage { get; private set; }
    public List<int> Listeners { get; set; }
    public ECUStatus Status { get; set; }
    public int TE = 0, RE = 0;

    public ECU(int id, string name, List<int> listeners, Queue<Message> messageQueue)
    {
        ID = id;
        Name = name;
        Listeners = listeners;
        MessageQueue = messageQueue;
        if (messageQueue != null && messageQueue.Count > 0)
        {
            NextMessage = MessageQueue.Dequeue();
        }
        Status = ECUStatus.Idle;
    }

    public void SendMessage(CANBus canBus)
    {
        canBus.MessageOnBus = NextMessage;
    }

    public void RemoveMessage()
    {
        NextMessage = MessageQueue.Count() == 0 ? null : MessageQueue.Dequeue();
    }

    public string ReceiveMessage(CANBus canBus)
    {
        var messageData = canBus.MessageOnBus;
        bool isValid = Message.IsValidMessage(messageData);

        return SendACK(isValid);
    }

    private string SendACK(bool isValid)
    {
        if (isValid)
        {
            return $"ecu{ID} ACK: 0\n";
        }
        else
        {
            RE += 1;
            return $"ecu{ID} ACK: \n";
        }
    }

    // Method to put the ECU into Arbitration mode
    public string EnterArbitrationMode()
    {
        Status = ECUStatus.Arbitration;
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.ARBITRATING);
        return $"ecu{ID}: Status changed to Arbitration mode.\n";
    }

    // Method to put the ECU into Idle mode
    public string EnterIdleMode()
    {
        Status = ECUStatus.Idle;
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.EMPTY);
        return $"ecu{ID}: Status changed to Idle mode.\n";
    }

    // Method to put the ECU into Sending mode
    public string EnterSendingMode()
    {
        Status = ECUStatus.Sending;
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.SENDING_0);
        Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.SENDING_0);
        return $"ecu{ID}: Status changed to Sending mode.\n";
    }

    public void Sending1()
    {
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.SENDING_1);
        Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.SENDING_1);
    }

    public void Sending0()
    {
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.SENDING_0);
        Program.can.ChangeBusLineColor(CanBusSimulator.CanBusesColor.SENDING_0);
    }

    // Method to put the ECU into Receiving mode
    public string EnterReceivingMode()
    {
        Status = ECUStatus.Receiving;
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.RECEIVING);
        return $"ecu{ID}: Status changed to Receiving mode.\n";
    }


    public string EnterPassiveMode()
    {
        Program.can.ChangeEcuConnectionColor(ID, CanBusSimulator.ECUConnectionState.PASSIVE);
        return $"ecu{ID} reached 5 transmission errors, entering error passive mode.\n";

    }

    public void ValidateMessage()
    {
        NextMessage.ValidateMessage();
    }

}
