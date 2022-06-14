# cv4pve-diag

[![License](https://img.shields.io/github/license/Corsinvest/cv4pve-diag.svg)](LICENSE.md)

```text
    ______                _                      __
   / ____/___  __________(_)___ _   _____  _____/ /_
  / /   / __ \/ ___/ ___/ / __ \ | / / _ \/ ___/ __/
 / /___/ /_/ / /  (__  ) / / / / |/ /  __(__  ) /_
 \____/\____/_/  /____/_/_/ /_/|___/\___/____/\__/

Diagnostic for Proxmox VE         (Made in Italy)

Usage: cv4pve-diag [options]

Options:
  -?|-h|--help           Show help information
  --version              Show version information
  --host                 The host name host[:port],host1[:port],host2[:port]
  --api-token            Api token format 'USER@REALM!TOKENID=UUID'. Require Proxmox VE 6.2 or later
  --username             User name <username>@<realm>
  --password             The password. Specify 'file:path_file' to store password in file.
  --settings-file        File settings (generated from create-settings)
  --ignored-issues-file  File ignored issues (generated from create-ignored-issues)
  --ignored-issues-show  Show second table with ignored issue
  -o|--output            Type output (default: text) Text,Unicode,UnicodeAlt,Markdown,Html,Json,JsonPretty
                         Allowed values are: Text, Unicode, UnicodeAlt, Markdown, Html, Json, JsonPretty

Commands:
  app-check-update       Check update application
  app-upgrade            Upgrade application
  create-ignored-issues  Create File ignored issues (ignored-issues.json)
  create-settings        Create file settings (settings.json)

Run 'cv4pve-diag [command] --help' for more information about a command.

cv4pve-diag is a part of suite cv4pve-tools.
For more information visit https://www.cv4pve-tools.com
```

## Copyright and License

Copyright: Corsinvest Srl
For licensing details please visit [LICENSE.md](LICENSE.md)

## Commercial Support

This software is part of a suite of tools called cv4pve-tools. If you want commercial support, visit the [site](https://www.cv4pve-tools.com)

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

E.g. install on linux 64

Download last package e.g. Debian cv4pve-diag-linux-x64.zip, on your os and install:

```sh
root@debian:~# unzip cv4pve-diag-linux-x64.zip
```

This tool need basically no configuration.

```sh
root@debian:~# cv4pve-diag --host=192.168.0.100 --username=root@pam --password=fagiano
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
--host=192.168.0.100
--username=root@pam
--password=fagiano
```
