# VRM bring to RPG

Templates for games that can bring in VRM files.

- Game engine: Unity 2021.3.25f1
- Unity packages:
  - Starter Assets - Third Person Character Controller 1.1.4
  - UniVRM v0.110.0
  - Mirror 78.3.0
- VRM: ニコニ立体ちゃん version 1
- Language: Japanese (日本語)
- Fonts: 07 AkazukinPOP（07あかずきんポップ）

Project Settings:

- `Player` > `Other Settings` > `Configuration` > `Active Input Handling*`: `Input System Package (New)`

## Ground layer

A Ground layer is assigned to the ground object that the player can jump to.

## Download caches

The character's VRM file is saved in `Application.persistentDataPath/vrm-cache/`.
`Application.persistentDataPath` varies by platform. See [here](https://docs.unity3d.com/ja/2021.3/ScriptReference/Application-persistentDataPath.html) for details.

## Password encryption

Assets/Scripts/NewNetworkAuthenticator.cs encrypts user account passwords with PBKDF2.

It is recommended that the PBKDF2_ITERATION constant be set to a random value of 10,000 or greater.
