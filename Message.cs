using System.Collections.Generic;
using System.Text;
using System;
using System.Security.Policy;
using System.Collections;
using System.Drawing;

public class Message
{
    public int Time { get; private set; }
    public string SOF { get; private set; }
    public int Priority { get; private set; }
    public string ArbitrationId { get; private set; }
    public string SB { get; private set; }
    public string RTR { get; private set; }
    public string IDE { get; private set; }
    public string r0 { get; private set; }
    public int DLC   { get; private set; }
    public string DlcString { get; private set; }
    public string Data { get; private set; }
    public string DataString { get; private set; }
    public string CRC { get; private set; }
    public string ACK { get; private set; }
    public string EOF { get; private set; }
    public string IFS { get; private set; }
    public string Frame { get; private set; }
    private static readonly uint Polynomial = 0x11021;  // CRC-16-ANSI polynomial
    private static readonly ushort InitialValue = 0xFFFF;  // Initial CRC value

    public Message(int time, string data, int priority)
    {
        Time = time;
        SOF = "0";
        Priority = priority;
        ArbitrationId = Convert.ToString(priority, 2).PadLeft(11, '0');
        if (ArbitrationId.EndsWith("00000"))
        {
            SB = "1";
        }
        else if (ArbitrationId.EndsWith("11111"))
        {
            SB = "0";
        }
        else
        {
            SB = "";
        }
        RTR = "0";
        IDE = "0";
        r0 = "0";
        Data = data;
        ACK = "11";
        EOF = "1111111";
        IFS = "111";

        DLC = Data.Length;
        DlcString = Convert.ToString(DLC, 2).PadLeft(4, '0');

        byte[] byteArray = Encoding.ASCII.GetBytes(Data);
        DataString = string.Join("", Array.ConvertAll(byteArray, b => Convert.ToString(b, 2).PadLeft(8, '0')));

        CRC = CalculateCRC16(byteArray);
        Frame = $"{SOF}{ArbitrationId}{SB}{RTR}{IDE}{r0}{DlcString}{DataString}{CRC}{ACK}{EOF}{IFS}";
    }

    public override string ToString()
    { 
        return Frame;
    }

    private static string CalculateCRC16(byte[] data)
    {
        ushort crc = InitialValue;

        foreach (byte byteData in data)
        {
            crc ^= (ushort)(byteData << 8);

            for (int bit = 8; bit > 0; bit--)
            {
                if ((crc & 0x8000) != 0)
                {
                    crc = (ushort)((crc << 1) ^ Polynomial);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return Convert.ToString(crc, 2).PadLeft(16, '0');
    }

    public void InvalidateMessage()
    {
        byte[] dataBytes = Encoding.ASCII.GetBytes(Data);
        Random random = new Random();

        // Introduce a random bit flip in the data
        int byteIndex = random.Next(dataBytes.Length);
        int bitIndex = random.Next(8);
        dataBytes[byteIndex] ^= (byte)(1 << bitIndex);

        // Update the Data property with the corrupted data
        Data = Encoding.ASCII.GetString(dataBytes);
        string binaryString = string.Join("", Array.ConvertAll(dataBytes, b => Convert.ToString(b, 2).PadLeft(8, '0')));

        Frame = $"{SOF}{ArbitrationId}{SB}{RTR}{IDE}{r0}{binaryString}{CRC}{ACK}{EOF}{IFS}";
    }

    public void ValidateMessage()
    {
        // Recalculate the CRC based on the current Data
        byte[] byteArray = Encoding.ASCII.GetBytes(Data);
        CRC = CalculateCRC16(byteArray);

        // Update the Frame with the corrected CRC
        DataString = string.Join("", Array.ConvertAll(byteArray, b => Convert.ToString(b, 2).PadLeft(8, '0')));
        Frame = $"{SOF}{ArbitrationId}{SB}{RTR}{IDE}{r0}{DlcString}{DataString}{CRC}{ACK}{EOF}{IFS}";
    }


    public static bool IsValidMessage(Message message)
    {

        var data = message.Data;
        var crc = message.CRC;
        byte[] byteArray = Encoding.ASCII.GetBytes(data);
        string calculatedCRC = CalculateCRC16(byteArray);
        Console.WriteLine($"{crc} == {calculatedCRC} == {crc == calculatedCRC}");
        return crc == calculatedCRC;
    }

}