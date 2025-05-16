# California State Developer Talk Demos

This repository contains demos for the California State Developer Talk on Semantic Kernel and Model Context Protocol. These demos showcase the integration of advanced AI capabilities into applications using Azure OpenAI and Semantic Kernel.

## Getting Started

To run these demos, follow the steps below:

1. **Deploy a Model**:
   - Deploy an Azure OpenAI model in your Azure subscription.

2. **Retrieve Settings**:
   - Obtain the following settings from your Azure portal:
     - Azure OpenAI Endpoint
     - Azure OpenAI Key
     - Deployment Name

3. **Configure the Application**:
   - Locate the `appsettings.Development.template.json` file in the respective project folder.
   - Rename the file to `appsettings.Development.json`.
   - Update the file with your Azure settings.

Once configured, you can build and run the demos to explore their functionality.

## Projects

### McpOfficeInfoClient

This project demonstrates the integration of Semantic Kernel with Model Context Protocol to handle citizen inquiries.

#### How to Run
1. Ensure the `McpOfficeInfoServer` project is running. You can find the server executable path in the `bin/Debug/net8.0/` directory of the `McpOfficeInfoServer` project.
2. Navigate to the `McpOfficeInfoClient` directory.
3. Update the `appsettings.Development.json` file:
   - Rename `appsettings.Development.template.json` to `appsettings.Development.json` if not already done.
   - Add your Azure OpenAI settings (`Endpoint`, `Key`, `Deployment`, and `ModelId`).
   - Specify the path to the MCP server executable in the `ExecutablePath` field under `McpOfficeInfoServer`.
4. Build and run the project using your preferred IDE or the .NET CLI.

---

### McpOfficeInfoServer

This project serves as the backend server providing office information for the client application.

#### How to Run
1. Navigate to the `McpOfficeInfoServer` directory.
2. Build and run the project using your preferred IDE or the .NET CLI.

---

### SkDemo

This project demonstrates the use of Semantic Kernel for various AI-driven tasks.

#### How to Run
1. Navigate to the `SkDemo` directory.
2. Update the `appsettings.Development.json` file:
   - Rename `appsettings.Development.template.json` to `appsettings.Development.json` if not already done.
   - Add your Azure OpenAI settings (`Endpoint`, `Key`, `Deployment`, and `ModelId`).
3. Build and run the project using your preferred IDE or the .NET CLI.

---

### SkDemoInquiry

This project extends the Semantic Kernel demo to handle specific inquiry scenarios.

#### How to Run
1. Navigate to the `SkDemoInquiry` directory.
2. Update the `appsettings.Development.json` file:
   - Rename `appsettings.Development.template.json` to `appsettings.Development.json` if not already done.
   - Add your Azure OpenAI settings (`Endpoint`, `Key`, `Deployment`, and `ModelId`).
3. Build and run the project using your preferred IDE or the .NET CLI.