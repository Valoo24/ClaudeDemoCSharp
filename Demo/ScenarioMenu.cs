namespace ClaudeDemo.Demo;

public record Scenario(string Number, string Title, Func<Task> Run);

// A console menu that runs the chosen scenario. Hidden commands (e.g. "/ref") are not listed
// but can be typed at the prompt to open another menu.
public static class ScenarioMenu
{
    public static async Task RunAsync(
        string title,
        IReadOnlyList<Scenario> scenarios,
        string exitLabel,
        IReadOnlyDictionary<string, Func<Task>>? hiddenCommands = null)
    {
        while (true)
        {
            Console.WriteLine($"=== {title} ===\n");
            foreach (var s in scenarios)
            {
                Console.WriteLine($"  {s.Number,2}. {s.Title}");
            }
            Console.WriteLine($"   0. {exitLabel}");
            Console.Write("\nScenario to run: ");

            var input = Console.ReadLine()?.Trim();
            if (input == "0" || input == null)
            {
                return;
            }

            if (hiddenCommands != null && hiddenCommands.TryGetValue(input, out var command))
            {
                await command();
                Console.Clear();
                continue;
            }

            var selected = scenarios.FirstOrDefault(s => s.Number == input);
            if (selected == null)
            {
                Console.WriteLine("Invalid choice.\n");
                continue;
            }

            Console.WriteLine();
            try
            {
                await selected.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while running the scenario: {ex.Message}");
            }

            Console.WriteLine("\nPress Enter to return to the menu...");
            Console.ReadLine();
            Console.Clear();
        }
    }
}
