# WhoIsIt - System Architecture

## Overview

WhoIsIt is a cross-platform CLI tool that queries Azure Active Directory (Microsoft Entra ID) to retrieve user information. The application uses Microsoft Graph API to fetch user details and organizational hierarchy information.

## High-Level Architecture

```mermaid
graph TD
    A[User Interface<br/>Command Line Input] --> B[Program.cs Main]
    
    B --> B1[Argument Parser<br/>UId/Email, Flags: -x, -t, -p]
    B --> B2[Input Mode Detection<br/>Direct args vs Piped stdin]
    B --> B3[Batch Processing Controller]
    B --> B4[Tree Traversal Logic<br/>Recursive Manager Chain]
    
    B1 --> C[Authentication Layer]
    B2 --> C
    B3 --> C
    B4 --> C
    
    C --> C1[DefaultAzureCredential]
    C1 --> C2[Azure CLI Credential]
    C1 --> C3[Environment Credential]
    C1 --> C4[Azure PowerShell Credential]
    C1 --> C5[Interactive Browser Credential]
    
    C --> D[Microsoft Graph Client]
    
    D --> D1[GraphServiceClient]
    D1 --> D2[User Query Service]
    D1 --> D3[Manager Lookup Service]
    D1 --> D4[Filtering: employeeId / mail]
    D1 --> D5[Field Selection<br/>Basic + Extended]
    
    D --> E[Microsoft Graph API<br/>Azure AD / Entra ID]
    
    E --> E1[Users Endpoint]
    E --> E2[Manager Relationship Endpoint]
    E --> E3[OAuth 2.0 Authentication]
    E --> E4[Photo Download Endpoint]
    
    E --> F[Output Processing]
    
    F --> F1[Formatted Console Output]
    F --> F2[Display Name Parsing<br/>Extract Org Code]
    F --> F3[Tree Level Indentation]
    F --> F5[Photo Save & Display<br/>iTerm2 Inline Protocol]
    F --> F4[Circular Reference Detection]
    
    F --> G[ClipboardHelper.cs]
    
    G --> G1[Cross-Platform<br/>Clipboard Integration]
    G1 --> G2[macOS: pbcopy]
    G1 --> G3[Windows: clip]
    G1 --> G4[Linux: xclip / xsel]
    
    style A fill:#e1f5ff
    style B fill:#fff4e1
    style C fill:#ffe1f5
    style D fill:#e1ffe1
    style E fill:#f5e1ff
    style F fill:#ffe1e1
    style G fill:#e1ffff
```

## Component Details

### 1. **Program.cs (Core Application)**

**Responsibilities:**
- Parse command-line arguments and flags (`-x`, `-t`, `-p`)
- Detect input mode (direct arguments vs. piped stdin)
- Initialize authentication and Graph API client
- Orchestrate user query workflow
- Implement recursive manager tree traversal
- Download and save user profile photos
- Display inline photos in iTerm2 (macOS)
- Prevent circular reference loops with `HashSet<string>`

**Key Methods:**
- `ProcessUserWithTraversal()`: Main user processing and recursive manager lookup
- `IsITerm2()`: Detect iTerm2 terminal for inline photo display
- `DisplayPhotoInline()`: Display photo using iTerm2 inline image protocol
- Argument parsing logic for batch mode and tree traversal
- Pipe detection: `Console.IsInputRedirected`

### 2. **ClipboardHelper.cs (Cross-Platform Utility)**

**Responsibilities:**
- Provide cross-platform clipboard access
- Auto-copy manager lookup commands to clipboard
- OS-specific process invocation

**Platform Support:**
- **macOS**: Uses `pbcopy` command
- **Windows**: Uses `clip` command
- **Linux**: Uses `xclip` (primary) or `xsel` (fallback)

### 3. **Authentication Layer**

**Azure.Identity.DefaultAzureCredential** provides multi-method authentication:
1. Environment variables
2. Azure CLI (`az login`)
3. Azure PowerShell
4. Interactive browser (fallback)

**Scopes:** `https://graph.microsoft.com/.default`

### 4. **Microsoft Graph API Integration**

**Endpoints Used:**
- `GET /users?$filter={field} eq '{value}'` - User sear
- `GET /users/{id}/photo/$value` - User profile photo downloadch
- `GET /users/{id}/manager` - Manager lookup
- `GET /users/{id}` - User details with specific fields

**Query Fields:**

**Basic Mode:**
- id, displayName, userPrincipalName, mail
- employeeId, jobTitle, department, officeLocation
- mobilePhone, businessPhones

**Extended Mode (-x):**
- All basic fields plus:
- givenName, surname, companyName
- streetAddress, city, state, postalCode, country
- accountEnabled, createdDateTime, employeeHireDate, employeeType

## Data Flow

### Single User Query Flow

```mermaid
sequenceDiagram
    actor User
    participant CLI as Program.cs
    participant Auth as Azure Identity
    participant Graph as Graph Client
    participant API as Microsoft Graph API
    participant Output as Output Processor
    participant Clipboard as ClipboardHelper
    
    User->>CLI: whoisit ABCD9999 [-x] [-t]
    CLI->>CLI: Parse Arguments
    CLI->>CLI: Detect Input Type<br/>(Email contains '@')
    CLI->>Auth: Request Credentials
    Auth->>Auth: Try Azure CLI
    Auth-->>CLI: Access Token
    CLI->>Graph: Initialize Client
    Graph->>API: GET /users?$filter=employeeId eq 'ABCD9999'
    API-->>Graph: User Details
    Graph->>API: GET /users/{id}/manager
    API-->>Graph: Manager Data
    Graph->>API: GET /users/{id}/photo/$value (if -p flag)
    API-->>Graph: Profile Photo Stream
    Graph-->>CLI: Combined User Data
    CLI->>CLI: Save Photo to ./photos/ (if -p flag)
    CLI->>CLI: Display Inline Photo if iTerm2 (macOS)
    CLI->>Output: Process & Format
    Output->>Output: Parse Display Name<br/>(Extract Org Code)
    Output->>Output: Apply Tree Indentation
    Output-->>User: Display User Info
    Output->>Clipboard: Copy Manager Lookup Command
    Clipboard-->>User: ✓ Clipboard Updated
```

### Tree Traversal Flow (-t flag)

```mermaid
flowchart TD
    Start([User: whoisit ABCD9999 -t]) --> Query[Query Initial User]
    Query --> Fetch[Fetch User Details from Graph API]
    Fetch --> Display[Display User Info<br/>Level 0]
    Display --> CheckMgr{Has Manager?}
    
    CheckMgr -->|No| End([End - Top of Hierarchy])
    CheckMgr -->|Yes| CheckVisited{Already Visited?}
    
    CheckVisited -->|Yes| CircularRef[Display Circular<br/>Reference Warning]
    CircularRef --> End
    
    CheckVisited -->|No| AddVisited[Add to Visited Set]
    AddVisited --> IncLevel[Increment Level]
    IncLevel --> Indent[Apply Indentation]
    Indent --> RecurseQuery[Recursive Query<br/>with Manager's UId]
    RecurseQuery --> Fetch
    
    style Start fill:#e1f5ff
    style End fill:#ffe1e1
    style CheckMgr fill:#fff4e1
    style CheckVisited fill:#ffe1f5
    style CircularRef fill:#ffcccc
```

### Batch Processing Flow (Piped Input)

```mermaid
flowchart LR
    Input[cat users.csv] -->|Pipe| Detect{Detect stdin<br/>IsInputRedirected?}
    Detect -->|Yes| ReadLines[Read Lines from stdin]
    ReadLines --> Collect[Collect UIds/Emails<br/>into List]
    Collect --> Loop{For Each Input}
    
    Loop -->|Next| QueryUser[Query User via<br/>Graph API]
    QueryUser --> DisplayResult[Display Formatted<br/>Results]
    DisplayResult --> Separator[Print Separator Line]
    Separator --> Loop
    
    Loop -->|Done| End([Exit])
    
    Detect -->|No| CheckArgs{Has Args?}
    CheckArgs -->|Yes| SingleMode[Single User Mode]
    CheckArgs -->|No| ShowUsage[Show Usage Info]
    
    style Input fill:#e1f5ff
    style Detect fill:#fff4e1
    style Loop fill:#ffe1f5
    style End fill:#e1ffe1
```

## Key Design Patterns

### 1. **Credential Chain Pattern**
Uses `DefaultAzureCredential` to attempt multiple authentication methods in sequence.

### 2. **Recursive Tree Traversal**
Implements depth-first traversal of organizational hierarchy with cycle detection.

### 3. **Cross-Platform Abstraction**
OS-specific implementations unified behind common interface (`ClipboardHelper`).

### 4. **Async/Await Pattern**
All Graph API calls use async operations for non-blocking I/O.

### 5. **Filter-Based Query**
Uses OData filters to perform server-side filtering on employeeId or email.

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 9.0 |
| Language | C# 13 |
| Authentication | Azure.Identity v1.13.1 |
| Graph API | Microsoft.Graph v5.66.0 |
| Platform | Cross-platform (macOS, Windows, Linux) |
| Deployment | Self-contained single-file executable |

## Security Considerations

1. **Authentication**: Leverages Azure credential chain (no hardcoded credentials)
2. **Scope**: Limited to `https://graph.microsoft.com/.default`
3. **Access Control**: Inherits user's Azure AD permissions
4. **No Data Storage**: Queries are ephemeral; no local caching

## Error Handling

- User not found: Graceful error message
- Manager not accessible: Silently skips manager info
- Circular reference: Detects and breaks infinite loops
- Authentication failure: Fallback through credential chain
- Clipboard failure: Non-blocking (silent failure)

## Deployment Models

### Development
```bash
dotnet run <UId|EMAIL>
```

### Production (Single-File Executable)

#### macOS (ARM64)
```bash
dotnet publish -c Release -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

#### macOS (Intel x64)
```bash
dotnet publish -c Release -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

#### Linux (x64)
```bash
dotnet publish -c Release -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

#### Linux (ARM64)
```bash
dotnet publish -c Release -r linux-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

#### Windows (x64)
```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

#### Windows (ARM64)
```bash
dotnet publish -c Release -r win-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

**Artifacts:**
- Platform-specific binary (e.g., `whoisit` for macOS ARM64, `whoisit.exe` for Windows)
- All dependencies bundled
- No .NET runtime required on target machine
- Output location: `bin/Release/net9.0/{runtime-identifier}/publish/`

## Future Enhancement Opportunities

1. **Team Visualization**: Fetch and display direct reports
2. **Configuration File**: Custom field selection via config
3. **Offline Mode**: Cache for limited offline functionality
4. **Group Membership**: Display security and distribution groups
5. ***
