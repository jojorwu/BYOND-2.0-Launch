# BYOND-2.0

## Building and Running on Linux

1.  **Publish the Launcher and Client:**
    Open a terminal and run the following commands from the root of the repository:
    ```sh
    dotnet publish Launcher -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
    dotnet publish Client -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
    ```

2.  **Create the Linux Package:**
    Run the packaging script from the root of the repository:
    ```sh
    chmod +x create_linux_package.sh
    ./create_linux_package.sh
    ```
    This will create a `BYOND_2.0_Linux.tar.gz` archive.

3.  **Run the Launcher:**
    - Extract the archive: `tar -xzvf BYOND_2.0_Linux.tar.gz`
    - Navigate to the extracted directory and run the launcher: `./Launcher`