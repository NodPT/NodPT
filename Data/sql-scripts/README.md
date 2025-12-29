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
   - **Inspector**: Quality assurance and review
   - **Worker**: Implementation and content creation
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

| Name | Category | Description |
|------|----------|-------------|
| Coding Project | Development | Software development with AI-assisted workflow |
| Book Writing | Writing | Structured book writing with AI collaboration |

### Prompts (16 total)

Each template has 8 prompts (4 node types × 2 message types):

| Template | Node Type | Message Type | Purpose |
|----------|-----------|--------------|---------|
| Coding Project | Director | Discussion | Strategic planning and architecture |
| Coding Project | Director | Decision | Architectural approvals |
| Coding Project | Manager | Discussion | Task coordination |
| Coding Project | Manager | Decision | Work prioritization |
| Coding Project | Inspector | Discussion | Code review feedback |
| Coding Project | Inspector | Decision | Quality gate approvals |
| Coding Project | Worker | Discussion | Implementation guidance |
| Coding Project | Worker | Decision | Technical choices |
| Book Writing | Director | Discussion | Book vision and structure |
| Book Writing | Director | Decision | Content direction |
| Book Writing | Manager | Discussion | Writing coordination |
| Book Writing | Manager | Decision | Section assignments |
| Book Writing | Inspector | Discussion | Editorial review |
| Book Writing | Inspector | Decision | Publication approval |
| Book Writing | Worker | Discussion | Content creation |
| Book Writing | Worker | Decision | Writing choices |

### AI Models (16 total)

Each template has 8 AI model configurations:

| Template | Node Type | Message Type | Model | Endpoint |
|----------|-----------|--------------|-------|----------|
| Coding Project | Director | Discussion | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Director | Decision | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Manager | Discussion | codellama:7b | http://ollama:11434/api/generate |
| Coding Project | Manager | Decision | codellama:7b | http://ollama:11434/api/generate |
| Coding Project | Inspector | Discussion | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Inspector | Decision | codellama:13b | http://ollama:11434/api/generate |
| Coding Project | Worker | Discussion | codellama:7b | http://ollama:11434/api/generate |
| Coding Project | Worker | Decision | codellama:7b | http://ollama:11434/api/generate |
| Book Writing | Director | Discussion | llama2:13b | http://ollama:11434/api/generate |
| Book Writing | Director | Decision | llama2:13b | http://ollama:11434/api/generate |
| Book Writing | Manager | Discussion | llama2:7b | http://ollama:11434/api/generate |
| Book Writing | Manager | Decision | llama2:7b | http://ollama:11434/api/generate |
| Book Writing | Inspector | Discussion | llama2:13b | http://ollama:11434/api/generate |
| Book Writing | Inspector | Decision | llama2:13b | http://ollama:11434/api/generate |
| Book Writing | Worker | Discussion | llama2:7b | http://ollama:11434/api/generate |
| Book Writing | Worker | Decision | llama2:7b | http://ollama:11434/api/generate |

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

### Inspector Models
- Large context for full code/content review (4096 tokens)
- Lower temperature for objective analysis (0.2-0.5)
- Detailed feedback predictions (512-1536 tokens)

### Worker Models
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

1. **XPO Field Naming**: XPO uses PascalCase for all fields (e.g., `Name`, `Description`, `CreatedAt`)
2. **Primary Key**: All tables use `OID` (Object ID) as VARCHAR/GUID primary key
3. **Foreign Keys**: Relationships use the parent table's `OID` (e.g., `Template` field in `Prompt` table)
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
