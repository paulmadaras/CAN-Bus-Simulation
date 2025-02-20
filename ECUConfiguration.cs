using System.Collections.Generic;
using System.Linq;
using System;

public class ECUConfiguration
{
    public int ID { get; set; }
    public string Frequency { get; set; } // Frequency as messages per minute, e.g., "60"
    public string ErrorRate { get; set; } // Error rate as a fraction, e.g., "1/25"
    public int MinPriority { get; set; }
    public int MaxPriority { get; set; }
    public List<int> Listeners { get; set; }
    public ECU ECU { get; set; }

    public List<Message> GenerateRandomMessages()
    {

        int messagesPerMinute = int.Parse(Frequency.Replace(" messages/min", ""));
        Console.WriteLine(messagesPerMinute);
        // Parse error rate as a fraction
        int errorRatePercentage = ParseFraction(ErrorRate);
        Console.WriteLine(errorRatePercentage);

        Random random = new Random();
        List<Message> generatedMessages = new List<Message>();

        for (int i = 0; i < 60; i += 60)
        {
            // Generate messages for 1000 stu (simulation time units)
            for (int time = 0; time < messagesPerMinute; time += 1)
            {
                // Determine priority randomly between MinPriority and MaxPriority
                int priority = random.Next(MinPriority, MaxPriority + 1);

                // Generate random data (5-10 ASCII characters)
                string data = new string(Enumerable.Range(0, random.Next(5, 11))
                    .Select(_ => (char)random.Next(65, 91)) // Random uppercase letters
                    .ToArray());

                // Create a correct or incorrect message based on error rate
                Message message = new Message(random.Next(i, i + 60), data, priority);
                if (random.Next(1 + errorRatePercentage) == 1)
                {
                    Console.WriteLine($"will invalidate {errorRatePercentage}");
                    message.InvalidateMessage();
                }

                generatedMessages.Add(message);
            }
        }

        // Shuffle messages before returning
        return generatedMessages.OrderBy(m => m.Time).ToList();
        
    }


    private void ShuffleMessages(List<Message> messages)
    {
        Random random = new Random();
        for (int i = messages.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = messages[i];
            messages[i] = messages[j];
            messages[j] = temp;
        }
    }

    private int ParseFraction(string fraction)
    {
        // Parse a string fraction like "1/25" into an integer value
        var parts = fraction.Split('/');
        if (parts.Length != 2) return 0; // Default to 0 if parsing fails
        if (int.TryParse(parts[0], out int numerator) && int.TryParse(parts[1], out int denominator))
        {
            return denominator > 0 ? denominator : 0; // Return denominator as the value
        }
        return 0;
    }
}
