# Building Cloud-Ready ASP.NET Core 10 Applications with Aspire

This repo is meant to help with the understanding of how to build
cloud-ready applications with ASP.NET Core 10 and Aspire.  Key concepts include:

* Aspire fundamentals - including the app host and service defaults
* Aspire hosting and client integrations
* Basics of logging, OpenTelemetry, health checks, and resilience
* Configuration and service discovery - including secret and non-secret parameters
* Testing with Aspire
* Agentic development with Aspire
* Contrasting with Docker Compose and other orchestration solutions

## Getting Started

You need the [Aspire prerequisites](https://aspire.dev/get-started/prerequisites/) and the
ability to run an `npx` command ([NodeJS](https://nodejs.org/en/download)).

**Run it!**

> **N O T E:** The first time you run the app, it may take a little longer to start
> if you don't already have the container images used by the solution downloaded.

The Aspire Dashboard will be launched and that will have links for the different
projects.  Start by clicking the link for the `Web App` project.

## Features

This is a simple e-commerce application for learning purposes.

Here are the features:

* **WebApp**
  * The home page and listing pages will show a subset of products
  * There is a page at `/Admin` that will show a list of products that can be edited / deleted and new ones added
  * If you navigate to `/Admin` without the admin role, you should see an `AccessDenied` page
  * Any validation errors from the API should be displayed in the UI
  * Can add items to cart and see a summary of the cart (shows when empty too)
  * Can submit an order or cancel the order and clear the cart
  * A submitted order will send a fake email
  * Couple of simple AI-based interactions are available on the Listing page

* **API**
  
  * `GET` based on category (or "all") and by id allow anonymous requests
  * `POST`, `PUT`, and `DELETE` require authentication and an `admin` role (available with the `bob` login, but not `alice`)
  * Validation will be done with [FluentValidation](https://docs.fluentvalidation.net/en/latest/index.html)
  * A `GET` with a category of something other than "all", "boots", "equip", or "kayak" will throw an error
  * Data is seeded by the `SeedData.json` contents in the `Data` project

* **AI**

  * `GET agent` method (in `CarvedRock.Agent/Agent.cs`) that provides some simple AI functionality (see the [AI section](#ai-setup-notes) below)

* Authentication provided by OIDC via a demo instance of [Duende IdentityServer](https://duendesoftware.com/products/identityserver) (`bob` is an admin, `alice` is not)

## VS Code Setup

You need the following extension:

* [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

Then just hit `F5` to run the app.

The [Aspire CLI](https://aspire.dev/get-started/install-cli/#install-the-aspire-cli) is highly recommended, along with the [Aspire VS Code Extension](https://aspire.dev/get-started/aspire-vscode-extension/).

## Data and EF Core Migrations

The `dotnet ef` tool is used to manage EF Core migrations.  The following command was used to create migrations (from the `CarvedRock.Data` folder).

```bash
dotnet ef migrations add Initial -s ../CarvedRock.Api
```

The application uses PostgreSQL.

## MailKit Client

Added `MailKit.Client` project based on the tutorial here:
<https://learn.microsoft.com/en-us/dotnet/aspire/extensibility/custom-integration>

## Verifying Emails

The very simple email functionality is done using a template
from [this GitHub repo](https://github.com/leemunroe/responsive-html-email-template)
and the [MailPit](https://mailpit.axllent.org/)
service that can easily run in a Docker container.

To see the emails just hit the link for the `Email Inbox` service in the Aspire Dashboard.

## MCP Server

* 4 tools are in the MCP server
* [MCP Inspector](https://github.com/modelcontextprotocol/inspector) is included via an Aspire hosting package to make interactively testing the MCP server easy
* `CarvedRockTools.cs` contains 2 tools that can be called anonymously (if `RequireAuthorization`()
     is not included on the `MapMcp()` call in `Program.cs`
* `AdminTools.cs` contains 2 tools that will require a user with the `admin` role both to see when listing tools
     and to execute them
* OAuth is implemented for security, with the
    [demo Duende IdentityServer](https://demo.duendesoftware.com)

**SPECIAL NOTE:** Enter `interactive.public` in the **Client ID** field
in the MCP Inspector to get authentication working from it.

## AI Setup Notes

***T I P :*** Just run the application, and follow the instructions for
setting the missing parameter!  :)

You also need to provide your own AI service if you want
this app to be fully functional with a chat interface that uses the
MCP server.

To replicate what I have done:

* Go to <https://platform.openai.com> (you'll need a login / account here)
* Create an API key

Add the API key to the user secrets - this can be done by setting the
parameter in the Aspire dashboard the first time you run the project.  
Or manage the user secrets for the API project and set the `AIConnection:OpenAIKey` value.

If you'd rather use Azure AI Foundry directly (also pretty simple), see the commented out
code and notes in `Program.cs` of the `CarvedRock.Agent` project.

## Testing

Notes coming soon on both integration tests (direct calls to API or MCP servers), and
UI tests.  The UI tests will use Playwright.

### Authenticated Users in Integration Tests

For testing authenticated users (with access tokens) against the MCP server or even
the API, I'm taking advantage of the fact that there are only 2 different user types that
I need to test (3 if you count anonymous) - admins and non-admins.  For those I am
using the `m2m` client on the demo IdentityServer for a non-admin, and the `m2m.short`
client for an admin (see the `CarvedRock.Core.ClaimsTransformation.cs` file and
look for the way it adds an admin claim on the token with the `m2m.short` client_id.)

Another option would be to configure your identity provider to support a
`ResourceOwnerPassword` grant and get tokens that way - but I didn't want to add
an identity provider to this solution (to keep things simpler).

## Agentic Development

Notes coming...
