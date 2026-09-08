

![PXUDX](https://raw.githubusercontent.com/DXXNS/PXUDX/refs/heads/master/pictures/title.PNG)

**PXUDX** is a lightweight C# tool for managing and updating **Proxmox VE** environments over SSH.

It connects to multiple Proxmox nodes, automatically discovers their LXC containers and virtual machines, and displays their current status in a simple console interface.

## Features

* Multi-node Proxmox support
* Automatic LXC and VM discovery
* Live running/stopped status
* Update all running LXC containers
* Update individual containers
* SSH-based management
* Simple keyboard-driven interface
* Color-coded machine status
* Automatic `apt` updates and cleanup

## How it works

PXUDX connects to your configured Proxmox nodes via SSH and uses the native Proxmox commands to retrieve containers and virtual machines.

For updates, PXUDX runs the package manager directly inside the selected LXC container.

Only **running LXC containers** are updated when using the update-all function. VMs and stopped containers are skipped.

## Interface

The main menu gives you an overview of your Proxmox infrastructure:

![PXUDX Interface](https://raw.githubusercontent.com/DXXNS/PXUDX/master/pictures/TitleImage.PNG)

Running machines are displayed in **green**, while stopped machines are displayed in **red**.

## Updating Containers

PXUDX can update all running LXC containers automatically.

![Updating Containers](https://raw.githubusercontent.com/DXXNS/PXUDX/master/pictures/updatingct.png)

You can also select a specific container by its CT ID:

![Updating Specific Container](https://raw.githubusercontent.com/DXXNS/PXUDX/master/pictures/updatingspecific.PNG)

## Configuration

Proxmox nodes are configured through a `nodes.json` file.

```json
{
  "Nodes": [
    {
      "Name": "pve01",
      "Host": "192.168.1.10",
      "Username": "root",
      "Password": "your-password",
      "Port": 22
    }
  ]
}
```



## Controls

| Key   | Action                            |
| ----- | --------------------------------- |
| `F1`  | Refresh node list                 |
| `F2`  | Update all running LXC containers |
| `F3`  | Update specific container         |
| `ESC` | Exit                              |

## Tech Stack

* C#
* .NET
* SSH.NET
* Proxmox VE
* Linux / SSH
* JSON

## Requirements

* Windows
* .NET
* Proxmox VE nodes with SSH access
* Sufficient permissions to manage LXC containers

## License

bro dis is open source sell it or som shi
