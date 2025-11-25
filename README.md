# BYOND-2.0

## Building the Launcher Installer (Windows)

To create the `setup.exe` installer for the launcher, you will need to have [Inno Setup](https://jrsoftware.org/isinfo.php) installed.

Once Inno Setup is installed, follow these steps:

1.  **Publish the Launcher:**
    Open a command prompt or PowerShell and run the following command from the root of the repository:
    ```sh
    dotnet publish Launcher -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
    ```

2.  **Compile the Installer Script:**
    - Open the Inno Setup Compiler.
    - Go to `File > Open` and select the `launcher_installer.iss` script from the root of the repository.
    - Go to `Build > Compile` to generate the `BYOND_2.0_Launcher_Setup.exe` file in the `installer` directory.