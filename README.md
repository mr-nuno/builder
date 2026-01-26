# .NET 10 Builder - AI-Driven System Generation

Welcome to the .NET 10 Builder! This tool leverages the power of Google's Gemini AI to transform high-level requirements into a fully-fledged, production-ready .NET application based on Clean Architecture.

You provide the "what" and the "who," and the builder generates the "how" – a complete, modern, and robust system foundation, ready to run in Docker.

## How It Works

The builder follows a simple but powerful process:

1.  **Input Analysis**: It reads `requirements.md` and `domain.md` to understand your system's needs.
2.  **AI Structuring**: It sends the context to the Gemini AI, which returns a structured JSON "Blueprint" of the system.
3.  **Blueprint Validation**: It rigorously validates the blueprint to ensure it's complete and structurally sound.
4.  **Code Generation**: It scaffolds a complete .NET solution using Scriban templates, including API endpoints, CQRS patterns, and EF Core setup.
5.  **GitHub Integration**: (Optional) It creates a private GitHub repository and populates it with issues for your epics and stories.
6.  **Containerization & Database Setup**: It automatically generates `Dockerfile` and `docker-compose.yml` files, starts a database container, and creates the initial EF Core migration.

## Prerequisites

1.  **.NET 10 SDK**: Ensure you have the .NET 10 SDK installed.
2.  **Docker Desktop**: Required for running the generated application and its database. Make sure the Docker daemon is running.
3.  **Gemini API Key**: You need an API key from Google AI Studio.
4.  **(Optional) GitHub CLI**: For automatic repository and issue creation, install and authenticate the `gh` CLI.

## Quick Start

1.  **Configure API Key**: Set your Gemini API key using the .NET User Secrets manager.
    ```bash
    dotnet user-secrets set "GEMINI_KEY" "YOUR_API_KEY_HERE"
    ```

2.  **Define Your Input**: Create `requirements.md` and `domain.md` files in the project directory. (See examples below).

3.  **Run the Builder**: Execute the program.
    ```bash
    dotnet run
    ```
    The builder will now generate the solution, start a database container, and create the first migration.

4.  **Run the Generated Application**: Navigate into the newly created project directory and start the entire system using Docker Compose.
    ```bash
    cd YourGeneratedProjectName
    docker-compose up --build
    ```

5.  **Access Your API**: Your API is now running! You can access the Scalar UI at `http://localhost:5000/`.

## What You Get: A "Ready-to-Run" System

The builder doesn't just generate code; it creates a fully operational development environment.

-   **`docker-compose.yml`**: Defines the services for your API and a Microsoft SQL Server database.
-   **`Dockerfile`**: A multi-stage Dockerfile that builds a production-optimized, secure container for your API.
-   **Automated EF Core Migration**: The builder automatically starts the database container and runs `dotnet ef migrations add` to create your initial database schema based on the entities in your `domain.md`. This means the system is ready to store data from the very first run.

## How to Write Input Files

The quality of the generated system depends directly on the quality of your input files.

### `domain.md` - Defining the Data

Be explicit about entities and their C# data types.

**Example `domain.md`:**
```markdown
# Domain Model

## Product
- Name: string
- Description: string
- Price: decimal
- StockQuantity: int

## Customer
- FirstName: string
- LastName: string
- Email: string
- RegisteredAt: DateTime
```

### `requirements.md` - Defining the Features

Describe what the system should do from a user's perspective.

**Example `requirements.md`:**
```markdown
# System Requirements

## Epic: Product Management
As an admin, I want to manage the product catalog.

### Stories
- I need to be able to create a new product.
- I need to be able to view the details of a single product.

## Epic: Customer Management
As a support agent, I want to manage customer information.

### Stories
- I need to be able to register a new customer.
- I need to be able to view a customer's profile.
```
