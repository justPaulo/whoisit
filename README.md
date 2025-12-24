# WhoIsIt

A simple and elegant CLI tool to query Azure Active Directory (Microsoft Entra ID) user information using either their UId (Employee ID) or email address.

## Features

- 🔍 Search users by **UId** or **Email Address**
- 📊 Display comprehensive user information
- 🔐 Secure authentication using Azure credentials
- 📱 Shows contact details, job information, and organizational data
- 🎯 Extended information mode with `-x` flag
- 🌲 **Recursive manager tree traversal** with `-t` flag
- 📸 **Profile photo download** with `-p` flag
- 🎨 Clean, formatted output
- 📋 **Auto-copy manager lookup command** to clipboard (cross-platform)
- 📄 **Batch processing** via stdin for multiple users
- 🖼️ **Inline photo display** in iTerm2 on macOS

## Prerequisites

- .NET 9.0 or later (for development)
- Azure CLI installed and authenticated (`az login`)
- Access to your organization's Azure Active Directory

## Installation

### Option 1: Build from Source

```bash
# Clone or download the repository
cd whoisit

# Build the project
dotnet build

# Run directly (use -- before arguments)
dotnet run -- <UId|EMAIL>

# Note: The -- separates dotnet CLI args from app args
# This is required when using flags like -p, -t, -x
```

### Option 2: Install Standalone Binary

#### macOS (ARM64)
```bash
# Build standalone executable
dotnet clean && dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true

# Copy to system path
sudo cp bin/Release/net9.0/osx-arm64/publish/whoisit /usr/local/bin/

# Now you can run from anywhere
whoisit <UId|EMAIL>
```

#### macOS (Intel x64)
```bash
# Build standalone executable
dotnet clean && dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# Copy to system path
sudo cp bin/Release/net9.0/osx-x64/publish/whoisit /usr/local/bin/

# Now you can run from anywhere
whoisit <UId|EMAIL>
```

#### Linux (x64)
```bash
# Build standalone executable
dotnet clean && dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Copy to system path
sudo cp bin/Release/net9.0/linux-x64/publish/whoisit /usr/local/bin/

# Now you can run from anywhere
whoisit <UId|EMAIL>
```

#### Linux (ARM64)
```bash
# Build standalone executable
dotnet clean && dotnet publish -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true

# Copy to system path
sudo cp bin/Release/net9.0/linux-arm64/publish/whoisit /usr/local/bin/

# Now you can run from anywhere
whoisit <UId|EMAIL>
```

#### Windows (x64)
```powershell
# Build standalone executable
dotnet clean; dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Copy to a directory in your PATH (example: C:\Tools)
Copy-Item bin\Release\net9.0\win-x64\publish\whoisit.exe C:\Tools\

# Now you can run from anywhere
whoisit <UId|EMAIL>
```

#### Windows (ARM64)
```powershell
# Build standalone executable
dotnet clean; dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true

# Copy to a directory in your PATH (example: C:\Tools)
Copy-Item bin\Release\net9.0\win-arm64\publish\whoisit.exe C:\Tools\

# Now you can run from anywhere
whoisit <UId|EMAIL>
```

## Usage

> **Note for Development**: When using `dotnet run`, add `--` before arguments to separate dotnet CLI options from app arguments:
> ```bash
> dotnet run -- ABCD9999 -p -x
> ```

### Basic Usage

```bash
# Query by UId
whoisit ABCD9999

# Query by email
whoisit jane.doe@somecompany.com

# Development mode
dotnet run -- ABCD9999
```

### Extended Information

Use the `-x` flag to display additional user details:

```bash
# Extended information with UId
whoisit ABCD9999 -x

# Extended information with email
whoisit jane.doe@somecompany.com -x
```

### Manager Tree Traversal

Use the `-t` flag to recursively traverse the management hierarchy up to the top:

```bash
# Traverse manager tree from UId
whoisit ABCD9999 -t

# Traverse manager tree from email
whoisit jane.doe@somecompany.com -t

# Can be combined with extended info
whoisit ABCD9999 -t -x
```

**Tree traversal features:**

- Recursively follows the manager chain
- Displays each level with indentation
- Continues until reaching a user with no manager (top of hierarchy)
- Detects circular references to prevent infinite loops
- Only works with single user queries (not compatible with batch mode)

### Profile Photo Download

Use the `-p` flag to download and save user profile photos:

```bash
# Download profile photo for a user
whoisit ABCD9999 -p

# Download with extended information
whoisit ABCD9999 -x -p

# Download for email address
whoisit jane.doe@somecompany.com -p

# Batch download photos
cat batch.csv | whoisit -p
```

**Photo download features:**

- Downloads profile photo from Microsoft Graph API
- Saves photos to `./photos/` directory (created automatically)
- Filename format: `<EmployeeId>.jpg` or `<email_domain>.jpg`
- Displays inline in iTerm2 on macOS (automatic detection)
- Silently skips if photo is not available
- Photo path shown in output when successfully downloaded

### Batch Processing

Process multiple users by piping UIds or emails via stdin:

```bash
# From a CSV file (one UId/email per line)
cat batch.csv | whoisit

# From echo with multiple entries
echo -e "ABCD9999\nABCD1234\nABCD1010" | whoisit

# With extended information
cat batch.csv | whoisit -x

# From file redirect
whoisit < batch.csv
```

**Batch mode features:**

- Processes each entry on a new line
- Visual separator between users
- Continues processing if a user is not found
- Clipboard copying disabled for batch mode

## Output Information

### Standard Output

- **Name**: Display name (cleaned, without org code)
- **OrgCode**: Organization code extracted from display name
- **Email**: Primary email address
- **UId**: Employee ID
- **Job Title**: User's job title
- **Department**: Department name
- **Office**: Office location
- **Mobile Phone**: Mobile phone number
- **Business Phone**: Business phone number(s)
- **User Principal**: User principal name
- **Manager**: Manager's name and UId

### Extended Output (`-x` flag)

Additional information includes:

- Given name and surname
- Company name
- Employee type and hire date
- Account status and creation date
- Full address (street, city, state, postal code, country)

### Profile Photo (`-p` flag)

When photo download is enabled:

- **Profile Photo**: ✓ Saved to photos/ABCD9999.jpg (if available)
- **Profile Photo**: ✗ Not available (if unavailable)
- Inline display in iTerm2 terminal on macOS (automatic)
- Photos saved to `./photos/` directory

## Combining Flags

Flags can be combined for more comprehensive output:

```bash
# Extended info with manager tree traversal
whoisit ABCD9999 -x -t

# Photo download with extended info
whoisit ABCD9999 -p -x

# All flags together (tree mode prevents batch)
whoisit ABCD9999 -x -t -p

# Batch with extended info and photos
cat users.csv | whoisit -x -p
```

## Authentication

The tool uses `DefaultAzureCredential` which supports multiple authentication methods:

1. **Azure CLI** (recommended): Run `az login` before using the tool
2. **Managed Identity**: Automatically used when running on Azure resources
3. **Environment Variables**: Can be configured for CI/CD scenarios
4. **Interactive Browser**: Fallback authentication method

### First-Time Setup

```bash
# Login to Azure CLI
az login

# Verify authentication
az account show

# Now you can use whoisit
whoisit ABCD9999
```

## Examples

### Basic User Lookup

```bash
# Basic user lookup by UId
$ whoisit ABCD9999
Searching for user with UId: ABCD9999...

╔═══════════════════════════════════════════════════════════════╗
║                        USER DETAILS                           ║
╚═══════════════════════════════════════════════════════════════╝

  Name:              Max Mustermann
  OrgCode:           ON RST
  Email:             jane.doe@somecompany.com
  UId:               ABCD9999
  Job Title:         Software Engineer
  Department:        IT
  Office:            Lisbon
  Mobile Phone:      +351 123456789
  Business Phone:    +351 987654321
  User Principal:    jane.doe@somecompany.com
  Manager:           John Doe (ABCD9998)

✓ Command copied to clipboard: whoisit ABCD9998

📋 To lookup manager, paste and run the command from clipboard
```

### Manager Tree Traversal

```bash
# Traverse the full manager hierarchy
$ whoisit ABCD9999 -t
Searching for user with UId: ABCD9999...

╔═══════════════════════════════════════════════════════════════╗
║                        USER DETAILS                           ║
╚═══════════════════════════════════════════════════════════════╝

  Name:              Max Mustermann
  OrgCode:           ON RST
  Email:             jane.doe@somecompany.com
  UId:               ABCD9999
  Job Title:         Software Engineer
  Department:        IT
  Office:            Lisbon
  Mobile Phone:      +351 123456789
  Business Phone:    +351 987654321
  User Principal:    jane.doe@somecompany.com
  Manager:           John Doe (ABCD9998)

════════════════════════════════════════════════════════════════

  ↑ Manager at level 1

  Searching for user with UId: ABCD9998...

  ╔═══════════════════════════════════════════════════════════════╗
  ║                        USER DETAILS                           ║
  ╚═══════════════════════════════════════════════════════════════╝

    Name:              John Doe
    OrgCode:           IT DPT
    Email:             john.doe@somecompany.com
    UId:               ABCD9998
    Job Title:         Engineering Manager
    Department:        IT
    [... manager details ...]
    Manager:           Jane Smith (ABCD9997)

  ════════════════════════════════════════════════════════════════

    ↑ Manager at level 2

    Searching for user with UId: ABCD9997...

    ╔═══════════════════════════════════════════════════════════════╗
    ║                        USER DETAILS                           ║
    ╚═══════════════════════════════════════════════════════════════╝

      Name:              Jane Smith
      OrgCode:           SI
      Email:             jane.smith@somecompany.com
      UId:               ABCD9997
      Job Title:         Director of Engineering
      Department:        IT
      [... director details ...]
      Manager:           N/A

    🏁 Reached top of organizational hierarchy
```

### Batch Processing

```bash

# Process multiple users from file
$ cat batch.csv | whoisit
Searching for user with UId: ABCD9999...

╔═══════════════════════════════════════════════════════════════╗
║                        USER DETAILS                           ║
╚═══════════════════════════════════════════════════════════════╝

  Name:              Jane Doe
  OrgCode:           ON RST
  Email:             jane.doe@somecompany.com
  UId:               ABCD9999
  [... additional details ...]

════════════════════════════════════════════════════════════════

Searching for user with UId: ABCD1234...

╔═══════════════════════════════════════════════════════════════╗
║                        USER DETAILS                           ║
╚═══════════════════════════════════════════════════════════════╝

  Name:              Jane Doe
  OrgCode:           IT DPT
  Email:             jane.doe@somecompany.com
  UId:               ABCD1234
  [... additional details ...]
```

### Extended Information

```bash
# Query by email with extended info
$ whoisit jane.doe@somecompany.com -x
Searching for user with email: jane.doe@somecompany.com...

[... standard output ...]

  ─── eXtended Information ───

  Given Name:        Paulo
  Surname:           Nascimento
  Company:           somecompany.com
  Employee Type:     Employee
  Hire Date:         2000-05-15
  Account Enabled:   Yes
  Created:           2000-05-10

  Street Address:    123 Main Street
  City:              Lisbon
  State:             Lisbon
  Postal Code:       1000-001
  Country:           Portugal
```

## Manager Lookup Feature

When a user has a manager, the tool automatically copies the command to look up the manager to your clipboard:

```bash
$ whoisit ABCD9999

[... user details displayed ...]

✓ Command copied to clipboard: whoisit ABCD9998

📋 To lookup manager, paste and run the command from clipboard
```

Simply paste (`Cmd+V` on macOS, `Ctrl+V` on Windows/Linux) in your terminal and press Enter to instantly look up the manager's details.

**Supported platforms:**

- **macOS**: Uses `pbcopy`
- **Windows**: Uses `clip`
- **Linux**: Uses `xclip` (with `xsel` fallback)

If clipboard tools are not available, the command will still be displayed for manual copying.

## Error Handling

The tool provides clear error messages for common scenarios:

- **Authentication failed**: Prompts to run `az login`
- **User not found**: Indicates no user matches the provided UId or email
- **API errors**: Displays Microsoft Graph API error messages
- **Manager not available**: Shows "N/A" if manager information is not accessible
- **Clipboard unavailable**: Shows warning but continues execution

## Development

### Project Structure

```

whoisit/
├── Program.cs           # Main application code
├── ClipboardHelper.cs   # Cross-platform clipboard utility
├── whoisit.csproj       # Project configuration
├── whoisit.sln          # Solution file
├── README.md            # This file
└── .gitignore          # Git ignore rules
```

### Dependencies

- **Microsoft.Graph** (v5.66.0): Microsoft Graph SDK for .NET
- **Azure.Identity** (v1.13.1): Azure authentication library

### Building for Different Platforms

```bash
# macOS ARM64 (M1/M2/M3/M4/M5)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# macOS Intel
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true

# Windows
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## Security Best Practices

- ✅ Uses **managed identity** authentication (no hardcoded credentials)
- ✅ Implements proper **error handling** with user-friendly messages
- ✅ Uses latest **Microsoft Graph SDK** with security updates
- ✅ Follows **least privilege** principle with scoped API permissions
- ✅ Supports **credential rotation** through Azure CLI

## Troubleshooting

### "Authentication failed"

- Ensure you're logged in: `az login`
- Verify your account: `az account show`
- Check you have access to Azure AD

### "No user found"

- Verify the UId or email is correct
- Ensure the user exists in your organization's Azure AD
- Check you have permissions to query users

### "Manager fetch error"

- Manager information may not be available for all users
- The tool will display "N/A" if manager details cannot be retrieved

### "Could not copy to clipboard"

- Clipboard utility may not be installed (Linux: install `xclip` or `xsel`)
- The command is still displayed and can be manually copied
- Does not affect the main functionality of the tool

## License

This project is provided as-is for internal organizational use.

## Contributing

Contributions are welcome! Please ensure code follows the existing style and includes appropriate error handling.

## Support

For issues or questions, please contact your IT department or the tool maintainer.

---

**Version**: 1.0  
**Last Updated**: November 2025  
**Target Framework**: .NET 9.0
