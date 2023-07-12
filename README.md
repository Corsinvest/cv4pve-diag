# cv4pve-diag

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-diag.svg)](LICENSE.md) ![GitHub all releases](https://img.shields.io/github/downloads/corsinvest/cv4pve-diag/total)

```text
     ______                _                      __
    / ____/___  __________(_)___ _   _____  _____/ /_
   / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
  / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
  \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/


  Diagnostic for Proxmox VE                      (Made in Italy)

  cv4pve-diag is a part of suite cv4pve.
  For more information visit https://www.corsinvest.it/cv4pve

Command Syntax:
  cv4pve-diag [options] [command]

Options:
  --api-token <api-token>                            Api token format 'USER@REALM!TOKENID=UUID'. Require Proxmox VE 6.2 or later
  --username <username>                              User name <username>@<realm>
  --password <password>                              The password. Specify 'file:path_file' to store password in file.
  --host <host> (REQUIRED)                           The host name host[:port],host1[:port],host2[:port]
  --settings-file <settings-file>                    File settings (generated from create-settings)
  --ignored-issues-file <ignored-issues-file>        File ignored issues (generated from create-ignored-issues)
  --ignored-issues-show                              Show second table with ignored issue
  -o, --output <Html|Json|JsonPretty|Markdown|Text>  Type output [default: Text]
  --version                                          Show version information
  -?, -h, --help                                     Show help and usage information

Commands:
  create-settings        Create file settings (settings.json)
  create-ignored-issues  Create File ignored issues (ignored-issues.json)
  export-collect         Export collect data collect to data.json
  execute                Execute diagnostic and print result to console
```

## Copyright and License

Copyright: Corsinvest Srl
For licensing details please visit [LICENSE.md](LICENSE.md)

## Commercial Support

This software is part of a suite of tools called cv4pve-tools. If you want commercial support, visit the [site](https://www.corsinvest.it/cv4pve)

## Tutorial

[![Tutorial](http://img.youtube.com/vi/hn1nw9KXlsg/0.jpg)](https://www.youtube.com/watch?v=hn1nw9KXlsg&feature=youtu.be "Tutorial")

## Introduction

Diagnostic for Proxmox VE.

this software collect data from Proxmox VE and output list of Warning/Critical/Info message.

## Main features

* Completely written in C#
* Use native api REST Proxmox VE (library C#)
* Independent os (Windows, Linux, Macosx)
* Installation unzip file extract binary
* Not require installation in Proxmox VE
* Execute out side Proxmox VE
* Custom settings from file --settings-file
* Ignore issue from file --ignored-issues-file
* Use Api token --api-token parameter
* Execution with file parameter e.g. @FileParameter.parm

## Api token

From version 6.2 of Proxmox VE is possible to use [Api token](https://pve.proxmox.com/pve-docs/pveum-plain.html).
This feature permit execute Api without using user and password.
If using **Privilege Separation** when create api token remember specify in permission.

## Configuration

Install on Linux x64

Step 1 - Download the Lastest Zip File cv4pve-diag-linux-x64.zip to a Directory of your Choice:
                                                                                              wget https://github.com/Corsinvest/cv4pve-diag/releases/download/x.x.x/cv4pve-diag-linux-x64.zip
	 
     NOTE: x.x.x is the Version Number
	 Example for v1.4.8: 
	 root@debian:/# wget https://github.com/Corsinvest/cv4pve-diag/releases/download/v1.4.8/cv4pve-diag-linux-x64.zip
	 
```sh
Step 2 - Unzip cv4pve-diag-linux-x64.zip to a Directory of your Choice:
         root@debian:/# unzip cv4pve-diag-linux-x64.zip

Step 3 - Chmod cv4pve-diag to Add Persmissions to Execute cv4pve-diag:
         root@debian:/# chmod +x cv4pve-diag 
	 NOTE: This Allows Owner\Group\Others to Execute cv4pve-diag
```

Step 4 - Run the Diagnostic Tool:  
         NOTE: Use ./ in front of the the Command cv4pve-diag if you are in the same Directory as cv4pve-diag
	       If you are at the Root Directory, use the Directory Path to cv4pve-diag 
```sh
Example in the same Directory:
root@debian:/cv4pvediag# ./cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

Example at the Root Directory:
root@debian:/# /cv4pvediag/cv4pve-diag --output=Text --host=192.168.0.100:8006 --username=root@pam --password=password execute

```

```txt
-------------------------------------------------------------------------------------------------------------------------------------
| Id                             | Description                                                  | Context | SubContext   | Gravity  |
-------------------------------------------------------------------------------------------------------------------------------------
| pve2                           | 1 Replication has errors                                     | Node    | Replication  | Critical |
| pve2                           | Zfs 'rpool' health problem                                   | Node    | Zfs          | Critical |
| 312                            | Unknown resource qemu                                        | Qemu    | Status       | Critical |
| pve3                           | Node not online                                              | Node    | Status       | Warning  |
| local-zfs:vm-117-disk-1        | Image Orphaned                                               | Storage | Image        | Warning  |
| local-zfs:vm-105-disk-3        | Image Orphaned                                               | Storage | Image        | Warning  |
| 121                            | Qemu Agent not enabled                                       | Qemu    | Agent        | Warning  |
| 101                            | OS 'XP/2003' not mantained from vendor!                      | Qemu    | Agent        | Warning  |
| 101                            | Qemu Agent not enabled                                       | Qemu    | Agent        | Warning  |
| 103                            | cv4pve-autosnap not configured                               | Qemu    | AutoSnapshot | Warning  |
| 115                            | vzdump backup not configured                                 | Qemu    | Backup       | Warning  |
| 205                            | vzdump backup not configured                                 | Qemu    | Backup       | Warning  |
| 103                            | vzdump backup not configured                                 | Qemu    | Backup       | Warning  |
| 313                            | vzdump backup not configured                                 | Qemu    | Backup       | Warning  |
| 117                            | Unused disk0                                                 | Qemu    | Hardware     | Warning  |
| 115                            | Cdrom mounted                                                | Qemu    | Hardware     | Warning  |
| 121                            | 10 snapshots older than 1 month                              | Qemu    | Snapshot     | Warning  |
| 313                            | 10 snapshots older than 1 month                              | Qemu    | Snapshot     | Warning  |
| 500                            | Start on boot not enabled                                    | Qemu    | StartOnBoot  | Warning  |
| 117                            | Start on boot not enabled                                    | Qemu    | StartOnBoot  | Warning  |
| 114                            | 10 snapshots older than 1 month                              | Lxc     | Snapshot     | Warning  |
| pve1                           | 3 Update availble                                            | Node    | Update       | Info     |
| pve2                           | 6 Update availble                                            | Node    | Update       | Info     |
| 109                            | For more performance switch 'scsi0' hdd to VirtIO            | Qemu    | VirtIO       | Info     |
-------------------------------------------------------------------------------------------------------------------------------------
```

## Settings

For change default settings can create file using **create-settings** command.
Edit settings.json file and execute new settings using parameter --settings-file.

## Ignore Issue

For ignore issues create file using **create-ignored-issues** command.
Edit ignored-issues.json file and execute using parameter --ignored-issues-file.
The regex rule is used for match in Id,SubContext,Description.

```json
[
  {
    "Id": "105",
    "Context": "Qemu",
    "SubContext": "Protection",
    "Description": null,
    "Gravity": "Critical"
  }
]
```

## Execution with file parameter

Is possible execute with file parameter

```sh
root@debian:~# cv4pve-diag @FileParameter.parm
```

File **FileParameter.parm**

```txt
--host=192.168.0.100:8006
--username=root@pam
--password=password
```
