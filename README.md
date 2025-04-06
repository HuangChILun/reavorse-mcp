# Unity MCP Package

A Unity package that focuses on enhanced material editing, building upon the original MCP functionality. Currently, only material enhancement is implemented, but the project is under active development with frequent updates. Upcoming features include lighting effects, advanced material properties, physics integration, terrain enhancements, and more. This package enables seamless communication between Unity and Large Language Models (LLMs) via the Model Context Protocol (MCP), acting as a bridge that allows Unity to send commands and receive responses from MCP-compliant tools.

## Overview

The Unity MCP Server provides a bidirectional communication channel between Unity (via C#) and a Python server, enabling:

- **Asset Management**: Create, import, and manipulate Unity assets programmatically.
- **Scene Control**: Manage scenes, objects, and their properties.
- **Enhanced Material Editing**: Apply and modify materials with improved lighting and shader support.
- **Script Integration**: Create, view, and update C# scripts within Unity.
- **Editor Automation**: Control Unity Editor functions like undo, redo, play mode, and build processes.
- **Additional Unity Features**: Leverage new experimental functions such as advanced lighting controls and post-processing effects.

This project is perfect for developers who want to leverage LLMs to enhance their Unity projects or automate repetitive tasks.

## Installation

To use the Unity MCP Package, ensure you have the following installed:

- **Unity 2020.3 LTS or newer** (⚠️ Currently only works in URP projects)
- **Python 3.12 or newer**
- **uv package manager**

### Step 1: Install Python

Download and install Python 3.12 or newer from [python.org](https://www.python.org/downloads/). Make sure to add Python to your system’s PATH during installation.

### Step 2: Install uv

uv is a Python package manager that simplifies dependency management. Install it using the command below based on your operating system:

- **Mac**:

  ```bash
  brew install uv
  ```

- **Windows**:

  ```bash
  powershell -c "irm https://astral.sh/uv/install.ps1 | iex"
  ```

  Then, add uv to your PATH:

  ```bash
  set Path=%USERPROFILE%\.local\bin;%Path%
  ```

- **Linux**:

  ```bash
  curl -LsSf https://astral.sh/uv/install.sh | sh
  ```

For alternative installation methods, see the [uv installation guide](https://docs.astral.sh/uv/getting-started/installation/).

**Important**: Do not proceed without installing uv.

### Step 3: Install the Unity Package

1. Open Unity and go to `Window > Package Manager`.
2. Click the `+` button and select `Add package from git URL`.
3. Enter: `https://github.com/HuangChILun/reavorse-mcp.git`

Once installed, the Unity MCP Package will be available in your Unity project. The server will start automatically when used with an MCP client like Claude Desktop or Cursor.

## Features

- **Bidirectional Communication**: Seamlessly send and receive data between Unity and LLMs.
- **Asset Management**: Import assets, instantiate prefabs, and create new prefabs programmatically.
- **Scene Control**: Open, save, and modify scenes, plus create and manipulate game objects.
- **Enhanced Material Editing & Lighting**:  Improved controls for material properties, advanced lighting, shader integration, and post-processing effects.
- **Script Integration**: Create, view, and update C# scripts within Unity.
- **Editor Automation**: Automate Unity Editor tasks like building projects or entering play mode.
- **Experimental Features**: Additional Unity functionalities are under testing; feedback is appreciated.

## Troubleshooting

Encountering issues? Try these fixes:

- **Unity Bridge Not Running**  
  Ensure the Unity Editor is open and the MCP window is active. Restart Unity if needed.

- **Python Server Not Connected**  
  Verify that Python and uv are correctly installed and that the Unity MCP package is properly set up.

- **Configuration Issues with Claude Desktop or Cursor**  
  Ensure your MCP client is configured to communicate with the Unity MCP server.
  
- **Connection Stuck or No Progress** 
  In some special situations where the process seems stuck with no progress, try closing both Claude and the Unity project, then reconnecting to re-establish the connection.



## Contact

Have questions about the project? Reach out!

- **X**: [@reavorse](https://x.com/q_thomax)

## Acknowledgments

Original Author: A huge thank you to justinpbarnett for creating the original Unity MCP Package. This enhanced version builds upon his work.

Special thanks to Unity Technologies for their excellent Editor API and to the community for continuous feedback.
