# .NET 10 Builder - AI-Driven System Generation

Welcome to the .NET 10 Builder! This tool leverages the power of Google's Gemini AI to transform high-level requirements into a fully-fledged, production-ready .NET application based on Clean Architecture.

You provide the "what" and the "who," and the builder generates the "how" – a complete, modern, and robust system foundation, ready to run in Docker.

## Prerequisites

1. **Docker Desktop**: Required for running the builder and the generated applications. Make sure Docker is running.
2. **Gemini API Key**: Get your API key from [Google AI Studio](https://makersuite.google.com/app/apikey).
3. **(Optional) GitHub CLI**: For automatic repository and issue creation, install and authenticate the `gh` CLI.

## Quick Start with Docker

### 1. Configure Environment Variables

Create a `.env` file in the project root with your Gemini API key:

```bash
# Required: Your Gemini API key from Google AI Studio
GEMINI_KEY=your_gemini_api_key_here

# Optional: GitHub token for automatic repo creation
GH_TOKEN=your_github_token_here
```

**Important**: The `.env` file is already in `.gitignore`, so your secrets won't be committed.

### 2. Define Your Requirements

Create a `requirements.md` file in the project root describing what your system should do:

```markdown
# My Application

## Epic: Task Management
As a user, I want to manage my tasks.

### Stories
- As a user, I want to create a new task.
- As a user, I want to mark a task as complete.
- As a user, I want to see a list of all my tasks.
- As a user, I want to set a priority (1-5) on my tasks.
```

### 3. Define Your Domain Model

Create a `domain.md` file describing your data entities:

```markdown
# Domain Model

## Task
- Title (string): Max 200 characters, Required
- Description (string): Max 1000 characters
- IsCompleted (bool): Default false
- Priority (int): Range 1-5, Default 1
- DueDate (datetime?)

## Tag
- Name (string): Unique, Required
- ColorHex (string): Format #RRGGBB
```

### 4. Run the Builder

Start the builder using Docker Compose:

```bash
docker compose up --build
```

The builder will:
1. Analyze your `requirements.md` and `domain.md` files
2. Generate a structured blueprint using AI
3. Scaffold a complete .NET solution with Clean Architecture
4. Create API endpoints, CQRS handlers, and EF Core setup
5. Generate Docker configuration for the new application
6. (Optional) Create a GitHub repository with issues for your epics and stories

### 5. Run Your Generated Application

After the builder completes, navigate to the generated project and start it:

```bash
cd YourGeneratedProjectName
docker compose up --build
```

Your API will be available at `http://localhost:5000/` with Scalar UI for API documentation.

## How It Works

The builder follows a simple but powerful process:

1. **Input Analysis**: Reads `requirements.md` and `domain.md` from the current directory
2. **AI Structuring**: Sends context to Gemini AI, which returns a structured JSON blueprint
3. **Blueprint Validation**: Validates the blueprint for completeness and correctness
4. **Code Generation**: Scaffolds a complete .NET solution using Scriban templates
5. **GitHub Integration**: (Optional) Creates a private GitHub repository with issues
6. **Containerization**: Generates `Dockerfile` and `docker-compose.yml` for the new application

## Input File Format

### `requirements.md` - Defining Features

Describe what the system should do from a user's perspective using epics and user stories.

**Example:**
```markdown
# E-Commerce System

## Epic: Product Management
As an admin, I want to manage the product catalog.

### Stories
- As an admin, I want to create a new product.
- As an admin, I want to view product details.
- As an admin, I want to update product information.

## Epic: Order Management
As a customer, I want to place orders.

### Stories
- As a customer, I want to add products to my cart.
- As a customer, I want to checkout and place an order.
```

### `domain.md` - Defining Data Entities

Be explicit about entities and their C# data types. Include validation rules.

**Example:**
```markdown
# Domain Model

## Product
- Name (string): Required, Max 200 characters
- Description (string): Max 1000 characters
- Price (decimal): Required, GreaterThan(0)
- StockQuantity (int): Default 0
- CategoryId (Guid): Required

## Order
- OrderDate (DateTime): Required
- CustomerId (Guid): Required
- TotalAmount (decimal): Required
- Status (string): Required
```

**Type Mapping:**
- "Money", "Price", "Amount" → `decimal`
- "Text", "Description", "Email" → `string`
- "Number", "Count", "Quantity" → `int`
- "Flag", "IsActive", "Done" → `bool`
- "Date", "Time", "Created" → `DateTime`

## What You Get

The builder generates a complete, production-ready system:

- **Clean Architecture**: Domain, Application, Infrastructure, and API layers
- **CQRS with MediatR**: Commands and queries separated
- **FastEndpoints**: Modern, minimal API framework
- **Entity Framework Core**: Database access with migrations
- **FluentValidation**: Input validation
- **Docker Support**: Complete Docker setup for API and database
- **Integration Tests**: Test infrastructure included
- **GitHub Issues**: (Optional) Epics and stories as GitHub issues

## File Structure

```
.
├── .env                    # Your environment variables (not committed)
├── requirements.md         # User stories and epics
├── domain.md              # Data model definition
├── system_prompt.txt      # (Optional) Custom AI prompt
├── docker-compose.yaml    # Builder configuration
└── YourGeneratedProject/  # Generated application
    ├── src/
    │   ├── YourProject.Api/
    │   ├── YourProject.Application/
    │   ├── YourProject.Domain/
    │   └── YourProject.Infrastructure/
    ├── tests/
    ├── docker-compose.yml
    └── ...
```

## Troubleshooting

**Builder fails with "GEMINI_KEY saknas"**
- Make sure your `.env` file exists and contains `GEMINI_KEY=your_key_here`
- Check that Docker Compose is reading the `.env` file

**Generated code not visible on host**
- The generated project is created in the current directory (mounted as `/data` in the container)
- Check the directory where you ran `docker compose up`

**GitHub integration fails**
- Make sure `GH_TOKEN` is set in your `.env` file
- The GitHub CLI is already installed in the Docker image, but it needs authentication via `GH_TOKEN`
- If you don't have a GitHub token, the builder will skip GitHub integration and continue with code generation

## Advanced Usage

### Custom System Prompt

You can customize the AI behavior by creating a `system_prompt.txt` file. This allows you to modify how the AI interprets your requirements and generates the blueprint.

### Running Locally (Without Docker)

If you prefer to run the builder locally:

```bash
# Set environment variables
export GEMINI_KEY=your_key_here
export GH_TOKEN=your_token_here  # Optional

# Run the builder
cd MyAgent.Builder
dotnet run
```

## License

This project is open source and available for use.
