# Declar

Idempotent OS state assistant

- Query the state of the OS
- Instruct the OS to change state

## Provides wrappers and helpers for

- Vendors: `apt`, `dnf`, `pacman`, `paru`, `winget`, `flathub`, `go`, `npm`, `cargo`
- Config file types: `toml`, `json`, `yaml`, `csv`, `ini`, `env`
- File-implementations: systemd unit file, os hosts file

## Construct declaration statements

The main idea is to be able to construct declarations that state to the CLI in which state we want the OS to be. It'll either verify that things are, indeed as they should be, or get to work in making sure they're as declared.

**Vendors**

"I declare that vendor pacman should install dotnet-sdk"

```sh
declar pacman installed dotnet-sdk
```

**Files**

"I declare that the os hosts file should have this line uncommented"

```sh
declar hosts uncommented "127.0.0.1 localhost"
```

### Considerations & nuances

Some interesting side-effects that could make sense would, for example be, the nuances in these declarations.

**Installed by other vendors**

Provided that the statement is that pacman should install dotnet sdk, what do we do if dotnet sdk has already been installed via the AUR using the paru vendor? Uninstall and install via pacman (my preference), do nothing, prompt? Perhaps an over-arching "mode" (inspired by agent tools) could provide a solution: "Just do as you see fit" vs "Ask me about every detail"

**State instructions**

The same could be the case for subtle nuances in `--state` like `commented` `uncommented` - the implied behavior is, if the line is missing entirely and it says `commented`, we should add the line with the correct file type's prefix comment `//` or `#`

**User-isntalled vendors**

There's a consideration on how to deal with user-installed vendors, which are actually more common than pre-installed vendors.

Pre-installed vendors are:
- `winget` for Windows 11
- `apt` for Debian
- `dnf` for Fedora
- `pacman` for Arch
- `paru` for CachyOS

User-installed vendors are:
- `flathub` for flatpak packages
- `go` for Go packages
- `npm` for Node packages
- `cargo` for Rust packages

Current thoughts are that the CLI will check before running any command if the vendor is installed, and if not, prompt the user to install it. This is because it's a bit more of a gray area in terms of how to handle it, and it would be a shame to have the CLI just fail because a user hasn't installed a vendor that they want to use.

Which certainly looks fancy, but theres's a chance we'll run into language design issues later

## Configs

These configs are configurable per-run by providing a CLI flag, or globally by creating a config file in the user's home directory at `~/.config/declar/config.ini` (or `%APPDATA%\declar\config.ini` on Windows)

- `--confirm` / `-c` (default: `false`, using `--confirm` or `-c` set to true) - whether to confirm the state change of the OS before making changes, and whether to ask the user about any discrepancies
