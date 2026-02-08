# Sample Data SQL Scripts

This directory contains MySQL scripts to populate the NodPT database with sample data for Templates, Prompts, and AI Models.

## 📋 Overview

The sample data includes:

1. **Templates**: Two sample templates
   - **Coding Project**: For software development workflows
   - **Book Writing**: For content creation and writing projects

2. **Prompts**: Role-specific instructions for AI agents at different hierarchy levels
   - **Director**: Strategic planning and high-level decisions
   - **Manager**: Task coordination and workflow management
   - **Supervisor**: Quality assurance and review
   - **Agent**: Implementation and content creation
   - Each role has both Discussion and Decision prompts

3. **AI Models**: Ollama-based model configurations
   - Default endpoint: `http://ollama:11434/api/generate`
   - Different models for different roles (codellama for coding, llama2 for writing)
   - Optimized parameters for each role and message type

## 📁 Files

- `00_master_sample_data.sql` - Master script that runs all others in order
- `01_sample_data_templates.sql` - Creates the two sample templates
- `02_sample_data_prompts.sql` - Creates prompts for all node levels (8 prompts per template)
- `03_sample_data_aimodels.sql` - Creates AI model configurations (8 models per template)

## 🚀 Usage

### Prerequisites

1. Ensure the database schema has been created by running the WebAPI application at least once in DEBUG mode
2. The following tables must exist: `Template`, `Prompt`, `AIModel`
3. MySQL client installed and configured

### Method 1: Using MySQL Command Line (Recommended)

```bash
# Navigate to the sql-scripts directory
cd /path/to/NodPT/Data/sql-scripts

# Run the master script (executes all scripts in order)
# Note: The master script uses SOURCE command which is MySQL-specific
mysql -h <host> -P <port> -u <username> -p<password> <database> < 00_master_sample_data.sql

# Or run individual scripts in order (works with any SQL client)
mysql -h <host> -P <port> -u <username> -p<password> <database> < 01_sample_data_templates.sql
mysql -h <host> -P <port> -u <username> -p<password> <database> < 02_sample_data_prompts.sql
mysql -h <host> -P <port> -u <username> -p<password> <database> < 03_sample_data_aimodels.sql
```

### Method 2: Using MySQL Workbench or Similar Tool

1. Open MySQL Workbench or your preferred database client
2. Connect to your database
3. Open and execute scripts in order:
   - `01_sample_data_templates.sql`
   - `02_sample_data_prompts.sql`
   - `03_sample_data_aimodels.sql`

### Method 3: Using Docker

If you're running the database in Docker:

```bash
# Copy scripts into the container
docker cp Data/sql-scripts/. <container_name>:/tmp/sql-scripts/

# Execute the master script
docker exec -i <container_name> mysql -u <username> -p<password> <database> < /tmp/sql-scripts/00_master_sample_data.sql
```

### Method 4: Using the Convenience Shell Script

The `load_sample_data.sh` script automates the loading process:

```bash
# Set database connection environment variables
export DB_HOST=localhost
export DB_PORT=3306
export DB_NAME=nodpt
export DB_USER=root
export DB_PASSWORD=yourpassword

# Run the script
cd /path/to/NodPT/Data/sql-scripts
./load_sample_data.sh
```

The script will:
- Verify database connection
- Execute all SQL scripts in order
- Display verification results
- Show colorized success/error messages

## 📊 Data Created

### Templates (2 total)

| Name           | Category    | Description                                    |
| -------------- | ----------- | ---------------------------------------------- |
| Coding Project | Development | Software development with AI-assisted workflow |
| Book Writing   | Writing     | Structured book writing with AI collaboration  |

### Prompts (16 total)

Each template has 8 prompts (4 node types × 2 message types):

**Hierarchical Role Architecture:**
- **Director**: Top-level orchestrator - analyzes project/book concept, defines architecture/structure, creates and instructs Managers
- **Manager**: Module owner - owns a module/section, designs internal structure, creates and instructs Supervisors
- **Supervisor**: Task decomposer - converts modules into concrete tasks, creates and instructs Agents
- **Agent**: Code/content generator - produces actual artifacts following exact instructions

**Communication Flow:** Director → Manager → Supervisor → Agent (strict downward flow)

| Template       | Node Type  | Message Type | Purpose                                                                          |
| -------------- | ---------- | ------------ | -------------------------------------------------------------------------------- |
| Coding Project | Director   | Discussion   | Analyze project, define architecture, create Manager nodes with instructions     |
| Coding Project | Director   | Decision     | Approve architecture, set standards, make final structural decisions             |
| Coding Project | Manager    | Discussion   | Design module structure, create Supervisor nodes with task specifications        |
| Coding Project | Manager    | Decision     | Approve module design, assign Supervisors, resolve sub-module conflicts          |
| Coding Project | Supervisor | Discussion   | Decompose sub-module into tasks, create Agent nodes with exact specifications    |
| Coding Project | Supervisor | Decision     | Approve task breakdown, validate Agent instructions, verify code compliance      |
| Coding Project | Agent      | Discussion   | Generate code artifacts following exact Supervisor specifications                |
| Coding Project | Agent      | Decision     | Minor implementation details only (within specification boundaries)              |
| Book Writing   | Director   | Discussion   | Analyze book concept, define structure, create Manager nodes with content plans  |
| Book Writing   | Director   | Decision     | Approve book structure, set style standards, make final content decisions        |
| Book Writing   | Manager    | Discussion   | Design content module, create Supervisor nodes with writing assignments          |
| Book Writing   | Manager    | Decision     | Approve content design, assign Supervisors, resolve section conflicts            |
| Book Writing   | Supervisor | Discussion   | Decompose section into writing tasks, create Agent nodes with exact instructions |
| Book Writing   | Supervisor | Decision     | Approve task breakdown, validate Agent instructions, verify content compliance   |
| Book Writing   | Agent      | Discussion   | Generate written content following exact Supervisor specifications               |
| Book Writing   | Agent      | Decision     | Minor word choice only (within specification boundaries)                         |

### AI Models (16 total)

Each template has 8 AI model configurations:

| Template       | Node Type  | Message Type | Model         | Endpoint                         |
| -------------- | ---------- | ------------ | ------------- | -------------------------------- |
| Coding Project | Director   | Discussion   | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Director   | Decision     | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Manager    | Discussion   | codellama:7b  | http://ollama:11434/api/generate |
| Coding Project | Manager    | Decision     | codellama:7b  | http://ollama:11434/api/generate |
| Coding Project | Supervisor | Discussion   | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Supervisor | Decision     | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Agent      | Discussion   | codellama:7b  | http://ollama:11434/api/generate |
| Coding Project | Agent      | Decision     | codellama:7b  | http://ollama:11434/api/generate |
| Book Writing   | Director   | Discussion   | llama2:13b    | http://ollama:11434/api/generate |
| Book Writing   | Director   | Decision     | llama2:13b    | http://ollama:11434/api/generate |
| Book Writing   | Manager    | Discussion   | llama2:7b     | http://ollama:11434/api/generate |
| Book Writing   | Manager    | Decision     | llama2:7b     | http://ollama:11434/api/generate |
| Book Writing   | Supervisor | Discussion   | llama2:13b    | http://ollama:11434/api/generate |
| Book Writing   | Supervisor | Decision     | llama2:13b    | http://ollama:11434/api/generate |
| Book Writing   | Agent      | Discussion   | llama2:7b     | http://ollama:11434/api/generate |
| Book Writing   | Agent      | Decision     | llama2:7b     | http://ollama:11434/api/generate |

### Execution Rules

The prompts enforce strict hierarchical execution rules:

1. **Responsibility Boundaries**: Each level operates only within its assigned responsibility
2. **No Upward Override**: No role may override or redesign decisions made at a higher level
3. **Deterministic Output**: All outputs must be explicit and implementation-ready
4. **Strict Downward Flow**: Communication flows strictly downward: Director → Manager → Supervisor → Agent
5. **Node Creation**: Each level creates the next level down with complete, unambiguous instructions
6. **Escalation Only**: Lower levels can only ask for clarification, not change higher-level decisions

**Example for Coding Project:**
- **Director** receives: "Create an HRMS web application"
- **Director** creates: Authentication Manager, Employee Manager, Salary Manager, Performance Manager
- **Manager** (Authentication) creates: Login Supervisor, Dashboard Supervisor, User Management Supervisor
- **Supervisor** (Login) creates: Frontend Agent, Backend Agent, Database Agent
- **Agent** generates: Actual code files following exact specifications

## 🔧 Model Parameters

AI models are configured with different parameters based on their role:

### Director Models
- Higher context window (4096 tokens) for full scope understanding
- Moderate temperature for strategic thinking (0.3-0.8)
- Longer predictions for detailed planning (512-2048 tokens)

### Manager Models
- Balanced settings for practical coordination (3072 context)
- Moderate temperature (0.3-0.6)
- Medium predictions (512-1024 tokens)

### Supervisor Models
- Large context for full code/content review (4096 tokens)
- Lower temperature for objective analysis (0.2-0.5)
- Detailed feedback predictions (512-1536 tokens)

### Agent Models
- Good context for implementation (3072 tokens)
- Variable temperature based on creativity needs (0.4-0.7)
- Longer predictions for content generation (512-2048 tokens)

## ✅ Verification

After running the scripts, verify the data:

```sql
-- Check templates
SELECT Name, Category, Version, IsActive FROM Template WHERE GCRecord IS NULL;

-- Check prompts count by template and type
SELECT 
    t.Name AS TemplateName,
    p.NodeType,
    p.MessageType,
    COUNT(*) AS PromptCount
FROM Prompt p
JOIN Template t ON p.Template = t.OID
WHERE p.GCRecord IS NULL
GROUP BY t.Name, p.NodeType, p.MessageType;

-- Check AI models count by template and type
SELECT 
    t.Name AS TemplateName,
    a.NodeType,
    a.MessageType,
    COUNT(*) AS AIModelCount
FROM AIModel a
JOIN Template t ON a.Template = t.OID
WHERE a.GCRecord IS NULL
GROUP BY t.Name, a.NodeType, a.MessageType;

-- Check total counts
SELECT 
    (SELECT COUNT(*) FROM Template WHERE GCRecord IS NULL) AS TotalTemplates,
    (SELECT COUNT(*) FROM Prompt WHERE GCRecord IS NULL) AS TotalPrompts,
    (SELECT COUNT(*) FROM AIModel WHERE GCRecord IS NULL) AS TotalAIModels;
```

Expected results:
- 2 Templates
- 16 Prompts (8 per template)
- 16 AI Models (8 per template)

## 🔒 Important Notes

1. **XPO Field Naming**: XPO maps persistent properties to PascalCase columns by default (e.g., `Name`, `Description`, `CreatedAt`)
2. **Primary Key**: By default, XPO's `XPObject` base class uses `OID` (Object ID) as an `INT AUTO_INCREMENT` primary key in MySQL/MariaDB
3. **Foreign Keys**: Relationships use the parent table's integer `OID` (e.g., `Template` field in `Prompt` table references `Template.OID`)
4. **Soft Delete**: `GCRecord` field is used for soft deletes (NULL = active, non-NULL = deleted)
5. **Optimistic Locking**: `OptimisticLockField` is used for concurrency control
6. **Enum Values**: NodeType and MessageType are stored as integers (0-based)

## 🐛 Troubleshooting

### Issue: Scripts fail with "table not found"
**Solution**: Run the WebAPI in DEBUG mode first to create the schema

### Issue: Scripts fail with foreign key constraint
**Solution**: Ensure scripts are run in order (templates first, then prompts and models)

### Issue: Duplicate key errors
**Solution**: The scripts can be run multiple times safely. Delete existing data first if needed:

```sql
DELETE FROM AIModel WHERE Template IN (SELECT OID FROM Template WHERE Name IN ('Coding Project', 'Book Writing'));
DELETE FROM Prompt WHERE Template IN (SELECT OID FROM Template WHERE Name IN ('Coding Project', 'Book Writing'));
DELETE FROM Template WHERE Name IN ('Coding Project', 'Book Writing');
```

## 📝 Customization

To customize the sample data:

1. **Prompts**: Edit `02_sample_data_prompts.sql` to modify the AI instructions
2. **AI Models**: Edit `03_sample_data_aimodels.sql` to change model identifiers or parameters
3. **Templates**: Edit `01_sample_data_templates.sql` to add new templates

## 🔗 Related Documentation

- [Data Layer README](/Data/README.md)
- [WebAPI Documentation](/WebAPI/README.md)
- [DevExpress XPO Documentation](https://docs.devexpress.com/XPO/)
- [Ollama API Documentation](https://github.com/ollama/ollama/blob/main/docs/api.md)
