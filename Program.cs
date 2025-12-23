using Azure.Identity;
using Microsoft.Graph;
using WhoIsIt;

// Parse command-line arguments
bool extendedInfo = false;
bool traverseTree = false;
bool photoDownload = false;
List<string> userInputs = new List<string>();

// Check if input is being piped
bool isPiped = !Console.IsInputRedirected ? false : true;

foreach (var arg in args)
{
    if (arg.Equals("-x", StringComparison.OrdinalIgnoreCase))
        extendedInfo = true;
    else if (arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
        traverseTree = true;
    else if (arg.Equals("-p", StringComparison.OrdinalIgnoreCase))
        photoDownload = true;
    else
        userInputs.Add(arg);
}

// Read from stdin if piped
if (isPiped)
{
    string? line;
    while ((line = Console.ReadLine()) != null)
    {
        line = line.Trim();
        if (!string.IsNullOrEmpty(line))
            userInputs.Add(line);
    }
}

if (userInputs.Count == 0)
{
    Console.WriteLine("Usage: whoisit <UId|EMAIL> [-x] [-t] [-p]");
    Console.WriteLine("       cat file.csv | whoisit [-x] [-p]");
    Console.WriteLine("       -x    Show extended information");
    Console.WriteLine("       -t    Traverse manager tree up to the top");
    Console.WriteLine("       -p    Download and save profile photo");
    Console.WriteLine("Example: whoisit Z999ABCD");
    Console.WriteLine("         whoisit john.doe@somecompany.com");
    Console.WriteLine("         whoisit Z999ABCD -x");
    Console.WriteLine("         whoisit Z999ABCD -t");
    Console.WriteLine("         whoisit Z999ABCD -p");
    Console.WriteLine("         cat batch.csv | whoisit");

    return 1;
}

// Batch mode and tree traversal are mutually exclusive
if (traverseTree && (isPiped || userInputs.Count > 1))
{
    Console.WriteLine("❌ Error: Tree traversal (-t) can only be used with a single user, not with batch mode.");
    return 1;
}

// Initialize Graph client with DefaultAzureCredential (supports multiple auth flows)
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ExcludeEnvironmentCredential = false,
    ExcludeAzureCliCredential = false,
    ExcludeAzurePowerShellCredential = false,
    ExcludeInteractiveBrowserCredential = false
});

var scopes = new[] { "https://graph.microsoft.com/.default" };
var graphClient = new GraphServiceClient(credential, scopes);

// Process each user input
bool isFirstUser = true;
HashSet<string> visitedUsers = new HashSet<string>(); // Track visited users to prevent infinite loops
int treeLevel = 0;

foreach (var userInput in userInputs)
{
    if (!isFirstUser)
    {
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }
    isFirstUser = false;

    // Start traversal for the first user
    await ProcessUserWithTraversal(userInput, graphClient, extendedInfo, traverseTree, photoDownload, visitedUsers, treeLevel);
}

return 0;

async Task ProcessUserWithTraversal(string userInput, GraphServiceClient graphClient, bool extendedInfo, bool traverseTree, bool photoDownload, HashSet<string> visitedUsers, int level)
{
    try
    {
        // Detect if input is email or UId
        bool isEmail = userInput.Contains('@');
        string filterField = isEmail ? "mail" : "employeeId";
        string searchValue = isEmail ? userInput.ToLowerInvariant() : userInput.ToUpperInvariant();
        
        // Check if we've already visited this user (prevent infinite loops)
        if (visitedUsers.Contains(searchValue))
        {
            Console.WriteLine($"⚠ Circular reference detected - already visited: {searchValue}");
            return;
        }
        visitedUsers.Add(searchValue);
        
        // Add indentation for tree levels
        string indent = new string(' ', level * 2);
        
        if (traverseTree && level > 0)
        {
            Console.WriteLine($"{indent}↑ Manager at level {level}");
            Console.WriteLine();
        }
        
        Console.WriteLine($"{indent}Searching for user with {(isEmail ? "email" : "UId")}: {searchValue}...\n");
        
        var selectFields = new List<string>
        {
            "id", "displayName", "userPrincipalName", "mail", "employeeId", "jobTitle", "department", "officeLocation", "mobilePhone", "businessPhones"
        };

        if (extendedInfo)
        {
            selectFields.AddRange(new[]
            {  
                "givenName",  "surname",  "companyName",  "streetAddress",  "city",  "state",  "postalCode",  "country",  "accountEnabled",  "createdDateTime",  "employeeHireDate",  "employeeType"
            });
        }

        var users = await graphClient.Users.GetAsync(config =>
        {
            config.QueryParameters.Filter = $"{filterField} eq '{searchValue}'";
            config.QueryParameters.Select = selectFields.ToArray();
        });

        if (users?.Value == null || !users.Value.Any())
        {
            Console.WriteLine($"{indent}❌ No user found with {(isEmail ? "email" : "UId")}: {searchValue}");
            return;
        }

        var user = users.Value.First();

        // Download profile photo if requested
        string? photoPath = null;
        if (photoDownload)
        {
            try
            {
                var photoStream = await graphClient.Users[user.Id].Photo.Content.GetAsync();
                if (photoStream != null)
                {
                    // Create photos directory if it doesn't exist
                    string photosDir = Path.Combine(Directory.GetCurrentDirectory(), "photos");
                    Directory.CreateDirectory(photosDir);
                    
                    // Save photo with user's employeeId or email as filename
                    string fileName = $"{user.EmployeeId ?? user.Mail?.Replace("@", "_")}.jpg";
                    photoPath = Path.Combine(photosDir, fileName);
                    
                    using (var fileStream = File.Create(photoPath))
                    {
                        await photoStream.CopyToAsync(fileStream);
                    }
                }
            }
            catch
            {
                // Photo not available or accessible - silently continue
            }
        }

        // Fetch manager information
        string? managerName = null;
        string? managerUId = null;
        try
        {
            var managerDirectoryObject = await graphClient.Users[user.Id].Manager.GetAsync();
        if (managerDirectoryObject != null)
        {
            // Get manager ID and fetch full details
            string? managerId = null;
            if (managerDirectoryObject.AdditionalData?.TryGetValue("id", out var idObj) == true)
                managerId = idObj?.ToString();
            else if (managerDirectoryObject is Microsoft.Graph.Models.User managerAsUser)
                managerId = managerAsUser.Id;

            // Fetch manager details with employeeId
            if (!string.IsNullOrEmpty(managerId))
            {
                var managerUser = await graphClient.Users[managerId].GetAsync(config =>
                {
                    config.QueryParameters.Select = new[] { "displayName", "employeeId" };
                });

                    if (managerUser != null)
                    {
                        managerName = managerUser.DisplayName;
                        managerUId = managerUser.EmployeeId;
                        
                        // Clean manager name (remove org code in parentheses)
                        if (!string.IsNullOrEmpty(managerName))
                            managerName = System.Text.RegularExpressions.Regex.Replace(managerName, @"\s*\([^)]+\)\s*", "").Trim();
                    }
                }
            }
        }
        catch
        {
            // Manager not available or accessible
        }

        // Extract OrgCode from displayName (text within parentheses) and clean the name
        string? orgCode = null;
        string? cleanedName = user.DisplayName;
        
        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            var match = System.Text.RegularExpressions.Regex.Match(user.DisplayName, @"\(([^)]+)\)");
            if (match.Success)
            {
                orgCode = match.Groups[1].Value;
                // Remove the parentheses and org code from the display name
                cleanedName = System.Text.RegularExpressions.Regex.Replace(user.DisplayName, @"\s*\([^)]+\)\s*", "").Trim();
            }
        }

        // Display user details in an elegant format
        Console.WriteLine($"{indent}╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"{indent}║                        USER DETAILS                           ║");
        Console.WriteLine($"{indent}╚═══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"{indent}  Name:              {cleanedName ?? "N/A"}");
        Console.WriteLine($"{indent}  OrgCode:           {orgCode ?? "N/A"}");
        Console.WriteLine($"{indent}  Email:             {user.Mail ?? user.UserPrincipalName ?? "N/A"}");
        Console.WriteLine($"{indent}  UId:               {user.EmployeeId ?? "N/A"}");
        Console.WriteLine($"{indent}  Job Title:         {user.JobTitle ?? "N/A"}");
        Console.WriteLine($"{indent}  Department:        {user.Department ?? "N/A"}");
        Console.WriteLine($"{indent}  Office:            {user.OfficeLocation ?? "N/A"}");
        Console.WriteLine($"{indent}  Mobile Phone:      {user.MobilePhone ?? "N/A"}");
        
        if (user.BusinessPhones != null && user.BusinessPhones.Any())
            Console.WriteLine($"{indent}  Business Phone:    {string.Join(", ", user.BusinessPhones)}");
        else
            Console.WriteLine($"{indent}  Business Phone:    N/A");
        
        Console.WriteLine($"{indent}  User Principal:    {user.UserPrincipalName ?? "N/A"}");
        
        if (!string.IsNullOrEmpty(managerName) && !string.IsNullOrEmpty(managerUId))
            Console.WriteLine($"{indent}  Manager:           {managerName} ({managerUId})");
        else if (!string.IsNullOrEmpty(managerName))
            Console.WriteLine($"{indent}  Manager:           {managerName}");
        else
            Console.WriteLine($"{indent}  Manager:           N/A");
        
        // Display photo information
        if (photoDownload)
        {
            if (!string.IsNullOrEmpty(photoPath))
            {
                Console.WriteLine($"{indent}  Profile Photo:     ✓ Saved to {photoPath}");
                
                // Attempt to display photo inline for iTerm2 on macOS
                if (OperatingSystem.IsMacOS() && IsITerm2())
                {
                    DisplayPhotoInline(photoPath, indent);
                }
            }
            else
            {
                Console.WriteLine($"{indent}  Profile Photo:     ✗ Not available");
            }
        }

        if (extendedInfo)
        {
            Console.WriteLine();
            Console.WriteLine($"{indent}  ─── eXtended Information ───");
            Console.WriteLine();
            Console.WriteLine($"{indent}  Given Name:        {user.GivenName ?? "N/A"}");
            Console.WriteLine($"{indent}  Surname:           {user.Surname ?? "N/A"}");
            Console.WriteLine($"{indent}  Company:           {user.CompanyName ?? "N/A"}");
            Console.WriteLine($"{indent}  Employee Type:     {user.EmployeeType ?? "N/A"}");
            Console.WriteLine($"{indent}  Hire Date:         {user.EmployeeHireDate?.ToString("yyyy-MM-dd") ?? "N/A"}");
            Console.WriteLine($"{indent}  Account Enabled:   {(user.AccountEnabled.HasValue ? (user.AccountEnabled.Value ? "Yes" : "No") : "N/A")}");
            Console.WriteLine($"{indent}  Created:           {user.CreatedDateTime?.ToString("yyyy-MM-dd") ?? "N/A"}");
            Console.WriteLine();
            Console.WriteLine($"{indent}  Street Address:    {user.StreetAddress ?? "N/A"}");
            Console.WriteLine($"{indent}  City:              {user.City ?? "N/A"}");
            Console.WriteLine($"{indent}  State:             {user.State ?? "N/A"}");
            Console.WriteLine($"{indent}  Postal Code:       {user.PostalCode ?? "N/A"}");
            Console.WriteLine($"{indent}  Country:           {user.Country ?? "N/A"}");
        }
        
        Console.WriteLine();

        // Copy manager lookup command to clipboard if available (only for single user queries without tree traversal)
        if (!isPiped && !traverseTree && !string.IsNullOrEmpty(managerUId))
        {
            string managerCommand = $"whoisit {managerUId}";
            
            if (ClipboardHelper.CopyToClipboard(managerCommand))
            {
                Console.WriteLine($"{indent}✓ Command copied to clipboard: {managerCommand}");
            }
            else
            {
                Console.WriteLine($"{indent}⚠ Could not copy command to clipboard");
            }
            
            Console.WriteLine();
            Console.WriteLine($"{indent}📋 To lookup manager, paste and run the command from clipboard");
        }
        
        // If tree traversal is enabled and manager exists, recursively process the manager
        if (traverseTree && !string.IsNullOrEmpty(managerUId))
        {
            Console.WriteLine();
            Console.WriteLine($"{indent}════════════════════════════════════════════════════════════════");
            Console.WriteLine();
            await ProcessUserWithTraversal(managerUId, graphClient, extendedInfo, traverseTree, photoDownload, visitedUsers, level + 1);
        }
        else if (traverseTree && string.IsNullOrEmpty(managerUId))
        {
            Console.WriteLine();
            Console.WriteLine($"{indent}🏁 Reached top of organizational hierarchy");
        }
    }
    catch (Azure.Identity.AuthenticationFailedException ex)
    {
        Console.WriteLine("❌ Authentication failed. Please ensure you're logged in:");
        Console.WriteLine("   - Run 'az login' to authenticate with Azure CLI");
        Console.WriteLine($"   - Error: {ex.Message}");
    }
    catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
    {
        Console.WriteLine($"❌ Microsoft Graph API error: {ex.Error?.Message ?? ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ An unexpected error occurred: {ex.Message}");
    }
}

bool IsITerm2()
{
    // Check if running in iTerm2
    var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
    return termProgram != null && termProgram.Equals("iTerm.app", StringComparison.OrdinalIgnoreCase);
}

void DisplayPhotoInline(string photoPath, string indent)
{
    try
    {
        // Read the image file
        byte[] imageBytes = File.ReadAllBytes(photoPath);
        string base64Image = Convert.ToBase64String(imageBytes);
        
        // iTerm2 inline image protocol
        // ESC ] 1337 ; File = [arguments] : base64-encoded-data BEL
        Console.WriteLine($"{indent}");
        Console.Write($"\u001b]1337;File=inline=1;width=20;preserveAspectRatio=1:{base64Image}\u0007");
        Console.WriteLine();
    }
    catch
    {
        // Silently fail if inline display doesn't work
    }
}


// dotnet clean && dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true