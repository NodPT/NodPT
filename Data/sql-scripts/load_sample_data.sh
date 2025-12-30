#!/bin/bash
# =============================================
# Sample Data Loader Script
# Description: Convenience script to load sample data into MySQL database
# =============================================

# Default values (can be overridden by environment variables)
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-3306}"
DB_NAME="${DB_NAME:-nodpt}"
DB_USER="${DB_USER:-root}"
DB_PASSWORD="${DB_PASSWORD}"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Function to print colored messages
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Function to check if MySQL is accessible
check_mysql_connection() {
    print_info "Checking MySQL connection..."
    
    if [ -z "$DB_PASSWORD" ]; then
        mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -e "SELECT 1;" "$DB_NAME" > /dev/null 2>&1
    else
        mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" -e "SELECT 1;" "$DB_NAME" > /dev/null 2>&1
    fi
    
    if [ $? -eq 0 ]; then
        print_success "MySQL connection successful"
        return 0
    else
        print_error "Cannot connect to MySQL database"
        return 1
    fi
}

# Function to execute SQL file
execute_sql_file() {
    local sql_file="$1"
    local filename=$(basename "$sql_file")
    
    print_info "Executing $filename..."
    
    if [ -z "$DB_PASSWORD" ]; then
        mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" "$DB_NAME" < "$sql_file"
    else
        mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" < "$sql_file"
    fi
    
    if [ $? -eq 0 ]; then
        print_success "$filename executed successfully"
        return 0
    else
        print_error "Failed to execute $filename"
        return 1
    fi
}

# Function to verify data
verify_data() {
    print_info "Verifying sample data..."
    
    local verify_sql="
    SELECT 
        (SELECT COUNT(*) FROM Template WHERE GCRecord IS NULL) AS TotalTemplates,
        (SELECT COUNT(*) FROM Prompt WHERE GCRecord IS NULL) AS TotalPrompts,
        (SELECT COUNT(*) FROM AIModel WHERE GCRecord IS NULL) AS TotalAIModels;
    "
    
    if [ -z "$DB_PASSWORD" ]; then
        result=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" "$DB_NAME" -e "$verify_sql" 2>/dev/null)
    else
        result=$(mysql -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" -e "$verify_sql" 2>/dev/null)
    fi
    
    if [ $? -eq 0 ]; then
        echo "$result"
        print_success "Data verification complete"
        return 0
    else
        print_error "Failed to verify data"
        return 1
    fi
}

# Main script
main() {
    echo "========================================"
    echo "NodPT Sample Data Loader"
    echo "========================================"
    echo ""
    
    # Display connection info
    print_info "Database: $DB_NAME"
    print_info "Host: $DB_HOST:$DB_PORT"
    print_info "User: $DB_USER"
    echo ""
    
    # Check MySQL connection
    if ! check_mysql_connection; then
        echo ""
        echo "Please check your database connection settings:"
        echo "  DB_HOST=$DB_HOST"
        echo "  DB_PORT=$DB_PORT"
        echo "  DB_NAME=$DB_NAME"
        echo "  DB_USER=$DB_USER"
        echo ""
        echo "You can set these via environment variables:"
        echo "  export DB_HOST=localhost"
        echo "  export DB_PORT=3306"
        echo "  export DB_NAME=nodpt"
        echo "  export DB_USER=root"
        echo "  export DB_PASSWORD=yourpassword"
        exit 1
    fi
    
    echo ""
    print_info "Starting sample data load..."
    echo ""
    
    # Execute SQL scripts in order
    if ! execute_sql_file "$SCRIPT_DIR/01_sample_data_templates.sql"; then
        exit 1
    fi
    
    if ! execute_sql_file "$SCRIPT_DIR/02_sample_data_prompts.sql"; then
        exit 1
    fi
    
    if ! execute_sql_file "$SCRIPT_DIR/03_sample_data_aimodels.sql"; then
        exit 1
    fi
    
    echo ""
    verify_data
    
    echo ""
    print_success "Sample data loaded successfully!"
    echo ""
    echo "Sample data includes:"
    echo "  - 2 Templates (Coding Project, Book Writing)"
    echo "  - 16 Prompts (8 per template)"
    echo "  - 16 AI Models (8 per template)"
}

# Show usage if --help is passed
if [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
    echo "Usage: ./load_sample_data.sh"
    echo ""
    echo "Environment variables:"
    echo "  DB_HOST     - Database host (default: localhost)"
    echo "  DB_PORT     - Database port (default: 3306)"
    echo "  DB_NAME     - Database name (default: nodpt)"
    echo "  DB_USER     - Database user (default: root)"
    echo "  DB_PASSWORD - Database password (no default)"
    echo ""
    echo "Example:"
    echo "  export DB_HOST=localhost"
    echo "  export DB_PORT=3306"
    echo "  export DB_NAME=nodpt"
    echo "  export DB_USER=root"
    echo "  export DB_PASSWORD=password"
    echo "  ./load_sample_data.sh"
    exit 0
fi

# Run main function
main
