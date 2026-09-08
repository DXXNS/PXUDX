using System.Text.Json;
using Renci.SshNet;

public class Config
{
    public List<NodeConfig> Nodes { get; set; } = new();
}

public class NodeConfig
{
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = "";
    public int Port { get; set; } = 22;
}

public class ProxmoxMachine
{
    public string NodeName { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = "";
}

class Program
{
    static Config Config = new();

    static async Task Main()
    {
        Console.Title = "CTVM Updater";
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        //conf load

        if (!File.Exists("nodes.json"))
        {
            PrintError("nodes.json not found.");

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);

            return;
        }

        try
        {
            string json =
                await File.ReadAllTextAsync("nodes.json");

            Config =
                JsonSerializer.Deserialize<Config>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })
                ?? new Config();
        }
        catch (Exception ex)
        {
            PrintError(
                $"Could not read nodes.json: {ex.Message}");

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);

            return;
        }

        if (Config.Nodes.Count == 0)
        {
            PrintError(
                "No nodes configured in nodes.json.");

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);

            return;
        }

        //main

        while (true)
        {
            await MainMenu();
        }
    }

    //Main menu

    static async Task MainMenu()
    {
        Console.Clear();

        PrintHeader();

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Nodelist:");
        Console.ResetColor();

        Console.WriteLine();

        List<ProxmoxMachine> machines =
            await GetAllMachines();

        if (machines.Count == 0)
        {
            Console.ForegroundColor =
                ConsoleColor.Red;

            Console.WriteLine(
                "[!] No containers / VMs found.");

            Console.ResetColor();
        }
        else
        {
            PrintMachineList(machines);
        }

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.WriteLine(
            "----------------------------------------");

        Console.ResetColor();

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.Write("[F1]");

        Console.ResetColor();

        Console.WriteLine(" Update Nodelist");

        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.Write("[F2]");

        Console.ResetColor();

        Console.WriteLine(" Update all");

        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.Write("[F3]");

        Console.ResetColor();

        Console.WriteLine(" Update specific");

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.Write("[ESC]");

        Console.ResetColor();

        Console.WriteLine(" Exit");

        ConsoleKey key =
            Console.ReadKey(true).Key;

        switch (key)
        {


            case ConsoleKey.F1:

                // refresh
                break;



            case ConsoleKey.F2:

                await UpdateAll(machines);

                break;



            case ConsoleKey.F3:

                await UpdateSpecific(machines);

                break;



            case ConsoleKey.Escape:

                Console.Clear();

                Console.ForegroundColor =
                    ConsoleColor.Cyan;

                Console.WriteLine(
                    "Goodbye.");

                Console.ResetColor();

                Environment.Exit(0);

                break;
        }
    }

    //printing shi

    static void PrintHeader()
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            "                 CTVM");

        Console.WriteLine(
            "                Updater");

        Console.WriteLine(
            "========================================");

        Console.ResetColor();
    }

    //ct/vm list

    static void PrintMachineList(
        List<ProxmoxMachine> machines)
    {
        foreach (ProxmoxMachine machine in machines)
        {
            //ordering shi

            if (machine.Type == "CT")
            {
                Console.ForegroundColor =
                    ConsoleColor.Cyan;

                Console.Write("[CT] ");
            }
            else
            {
                Console.ForegroundColor =
                    ConsoleColor.Magenta;

                Console.Write("[VM] ");
            }

            //ids

            Console.ForegroundColor =
                ConsoleColor.White;

            Console.Write($"{machine.Id} ");
            //name


            Console.Write($"{machine.Name} ");

            //node

            Console.ForegroundColor =
                ConsoleColor.DarkGray;

            Console.Write(
                $"({machine.NodeName}) ");

            //stats

            if (machine.Status.Equals(
                    "running",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor =
                    ConsoleColor.Green;

                Console.WriteLine(
                    "● RUNNING");
            }
            else if (machine.Status.Equals(
                         "stopped",
                         StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    "● STOPPED");
            }
            else
            {
                Console.ForegroundColor =
                    ConsoleColor.Yellow;

                string status =
                    string.IsNullOrWhiteSpace(
                        machine.Status)
                        ? "UNKNOWN"
                        : machine.Status.ToUpper();

                Console.WriteLine(
                    $"● {status}");
            }

            Console.ResetColor();
        }
    }

    //get all

    static async Task<List<ProxmoxMachine>>
        GetAllMachines()
    {
        var result =
            new List<ProxmoxMachine>();

        foreach (NodeConfig node in Config.Nodes)
        {
            try
            {
                using SshClient ssh =
                    Connect(node);

                //lcx conts

                string ctOutput =
                    ssh.RunCommand(
                        "pct list"
                    ).Result;

                string[] ctLines =
                    ctOutput.Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (string rawLine in ctLines)
                {
                    string line =
                        rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    //skip header
                    if (line.StartsWith(
                            "VMID",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] parts =
                        line.Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                        continue;

                    string id =
                        parts[0];

                    string status =
                        parts[1];

                    //get real cont name

                    string configOutput =
                        ssh.RunCommand(
                            $"pct config {id}"
                        ).Result;

                    string name =
                        GetHostnameFromConfig(
                            configOutput,
                            id);

                    result.Add(
                        new ProxmoxMachine
                        {
                            NodeName = node.Name,
                            Id = id,
                            Name = name,
                            Type = "CT",
                            Status = status
                        });
                }

                //get vms

                string vmOutput =
                    ssh.RunCommand(
                        "qm list"
                    ).Result;

                string[] vmLines =
                    vmOutput.Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries);

                foreach (string rawLine in vmLines)
                {
                    string line =
                        rawLine.Trim();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    //skip header
                    if (line.StartsWith(
                            "VMID",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] parts =
                        line.Split(
                            ' ',
                            StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 3)
                        continue;

                    string id =
                        parts[0];

                    string name =
                        parts.Length >= 2
                            ? parts[1]
                            : $"VM {id}";

                    string status =
                        parts.Length >= 3
                            ? parts[2]
                            : "unknown";

                    result.Add(
                        new ProxmoxMachine
                        {
                            NodeName = node.Name,
                            Id = id,
                            Name = name,
                            Type = "VM",
                            Status = status
                        });
                }

                ssh.Disconnect();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    $"[ERROR] {node.Name}: {ex.Message}");

                Console.ResetColor();
            }
        }

        await Task.CompletedTask;

        //sortning shi

        return result
            .OrderBy(x => x.NodeName)
            .ThenBy(x =>
            {
                return int.TryParse(
                    x.Id,
                    out int id)
                    ? id
                    : int.MaxValue;
            })
            .ToList();
    }

    //hostname for cts from config

    static string GetHostnameFromConfig(
        string config,
        string id)
    {
        foreach (string rawLine in
                 config.Split('\n'))
        {
            string line =
                rawLine.Trim();

            if (line.StartsWith(
                    "hostname:",
                    StringComparison.OrdinalIgnoreCase))
            {
                string hostname =
                    line[
                        "hostname:".Length..
                    ].Trim();

                if (!string.IsNullOrWhiteSpace(
                        hostname))
                {
                    return hostname;
                }
            }
        }

        return $"CT {id}";
    }

    //update all cts vms

    static async Task UpdateAll(
        List<ProxmoxMachine> machines)
    {
        Console.Clear();

        PrintUpdatingHeader();

        Console.WriteLine();

        // only running cts
        List<ProxmoxMachine> containers =
            machines
                .Where(x =>
                    x.Type == "CT" &&
                    x.Status.Equals(
                        "running",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        //skip
        List<ProxmoxMachine> skipped =
            machines
                .Where(x =>
                    x.Type != "CT" ||
                    !x.Status.Equals(
                        "running",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        Console.ForegroundColor =
            ConsoleColor.White;

        Console.WriteLine(
            $"Running CTs: {containers.Count}");

        Console.ResetColor();

        Console.WriteLine();

        if (containers.Count == 0)
        {
            Console.ForegroundColor =
                ConsoleColor.Yellow;

            Console.WriteLine(
                "No running LXC containers to update.");

            Console.ResetColor();

            await Pause();

            return;
        }

        int updated = 0;
        int errors = 0;

        foreach (ProxmoxMachine container
                 in containers)
        {
            bool success =
                await UpdateContainer(
                    container);

            if (success)
            {
                updated++;
            }
            else
            {
                errors++;
            }
        }


        //summary

        Console.Clear();

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            "            UPDATE COMPLETE");

        Console.WriteLine(
            "========================================");

        Console.ResetColor();

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.WriteLine(
            $"Updated : {updated}");

        Console.ResetColor();

        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            $"Skipped : {skipped.Count}");

        Console.ResetColor();

        Console.ForegroundColor =
            errors > 0
                ? ConsoleColor.Red
                : ConsoleColor.Green;

        Console.WriteLine(
            $"Errors  : {errors}");

        Console.ResetColor();

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.WriteLine(
            "VMs and stopped CTs were skipped.");

        Console.ResetColor();

        Console.WriteLine();

        await Pause();
    }

    //specific updater

    static async Task UpdateSpecific(
        List<ProxmoxMachine> machines)
    {
        Console.Clear();

        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            "            UPDATE SPECIFIC");

        Console.WriteLine(
            "========================================");

        Console.ResetColor();

        Console.WriteLine();

        List<ProxmoxMachine> containers =
            machines
                .Where(x => x.Type == "CT")
                .ToList();

        if (containers.Count == 0)
        {
            Console.ForegroundColor =
                ConsoleColor.Yellow;

            Console.WriteLine(
                "No LXC containers found.");

            Console.ResetColor();

            await Pause();

            return;
        }

        //show cts

        foreach (ProxmoxMachine container
                 in containers)
        {
            if (container.Status.Equals(
                    "running",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor =
                    ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;
            }

            Console.Write(
                $"{container.Id}");

            Console.ResetColor();

            Console.Write(
                $" - {container.Name} ");

            Console.ForegroundColor =
                ConsoleColor.DarkGray;

            Console.Write(
                $"({container.NodeName}) ");

            Console.ResetColor();

            if (container.Status.Equals(
                    "running",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor =
                    ConsoleColor.Green;

                Console.WriteLine(
                    "● RUNNING");
            }
            else
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    "● STOPPED");
            }

            Console.ResetColor();
        }

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.White;

        Console.Write(
            "Enter CT ID: ");

        Console.ResetColor();

        string? input =
            Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            return;

        ProxmoxMachine? selected =
            containers.FirstOrDefault(
                x => x.Id.Equals(
                    input.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (selected == null)
        {
            Console.WriteLine();

            PrintError(
                $"Container {input} not found.");

            await Pause();

            return;
        }

        //stats

        if (!selected.Status.Equals(
                "running",
                StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine();

            PrintError(
                $"CT {selected.Id} is not running.");

            await Pause();

            return;
        }

        //update

        await UpdateContainer(
            selected);

        Console.WriteLine();

        await Pause();
    }
    //update container

    static async Task<bool> UpdateContainer(
        ProxmoxMachine container)
    {
        Console.Clear();

        PrintUpdatingHeader();

        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.Write("Node: ");

        Console.ForegroundColor =
            ConsoleColor.White;

        Console.WriteLine(
            container.NodeName);

        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.Write("CT:   ");

        Console.ForegroundColor =
            ConsoleColor.White;

        Console.WriteLine(
            container.Id);

        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.Write("Name: ");

        Console.ForegroundColor =
            ConsoleColor.White;

        Console.WriteLine(
            container.Name);

        Console.ResetColor();

        Console.WriteLine();

        //find node

        NodeConfig? node =
            Config.Nodes.FirstOrDefault(
                x => x.Name.Equals(
                    container.NodeName,
                    StringComparison.OrdinalIgnoreCase));

        if (node == null)
        {
            PrintError(
                $"Node '{container.NodeName}' not found.");

            return false;
        }

        try
        {
            using SshClient ssh =
                Connect(node);

            //stats

            string status =
                ssh.RunCommand(
                    $"pct status {container.Id}"
                ).Result.Trim();

            if (!status.Contains(
                    "running",
                    StringComparison.OrdinalIgnoreCase))
            {
                PrintError(
                    $"CT {container.Id} is no longer running.");

                return false;
            }

            //update

            Task<SshCommand> updateTask =
                Task.Run(() =>
                    ssh.RunCommand(
                        $"pct exec {container.Id} -- " +
                        "bash -c " +
                        "\"export DEBIAN_FRONTEND=noninteractive; " +
                        "apt-get update -qq && " +
                        "apt-get upgrade -y -qq && " +
                        "apt-get autoremove -y -qq\""
                    )
                );

            //string anim

            string[] animation =
            {
                "Updating.",
                "Updating..",
                "Updating..."
            };

            int animationIndex = 0;

            while (!updateTask.IsCompleted)
            {
                Console.ForegroundColor =
                    ConsoleColor.Cyan;

                Console.Write(
                    "\r" +
                    animation[
                        animationIndex %
                        animation.Length
                    ]);

                Console.ResetColor();

                Console.Write(
                    "          ");

                animationIndex++;

                await Task.Delay(500);
            }

            //summary

            SshCommand command =
                await updateTask;

            Console.WriteLine();

            if (!string.IsNullOrWhiteSpace(
                    command.Error))
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;

                Console.WriteLine(
                    "Update error:");

                Console.WriteLine(
                    command.Error);

                Console.ResetColor();

                return false;
            }

            Console.ForegroundColor =
                ConsoleColor.Green;

            Console.WriteLine(
                $"CT {container.Id} updated successfully.");

            Console.ResetColor();

            await Task.Delay(700);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine();

            PrintError(
                $"CT {container.Id}: {ex.Message}");

            await Task.Delay(1000);

            return false;
        }
    }

    //ssh shi

    static SshClient Connect(
        NodeConfig node)
    {
        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.WriteLine(
            $"Connecting to {node.Name} ({node.Host})...");

        Console.ResetColor();

        var ssh =
            new SshClient(
                node.Host,
                node.Port,
                node.Username,
                node.Password);

        ssh.Connect();

        if (!ssh.IsConnected)
        {
            ssh.Dispose();

            throw new Exception(
                $"Could not connect to {node.Host}");
        }

        return ssh;
    }

    //update header

    static void PrintUpdatingHeader()
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "========================================");

        Console.WriteLine(
            "               Updating...");

        Console.WriteLine(
            "========================================");

        Console.ResetColor();
    }

    //bitchass error printing

    static void PrintError(
        string message)
    {
        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.WriteLine(
            $"[ERROR] {message}");

        Console.ResetColor();
    }

    //pause

    static async Task Pause()
    {
        Console.WriteLine();

        Console.ForegroundColor =
            ConsoleColor.DarkGray;

        Console.WriteLine(
            "Press any key to continue...");

        Console.ResetColor();

        Console.ReadKey(true);

        await Task.CompletedTask;
    }
}