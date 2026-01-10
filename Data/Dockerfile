# Use official MariaDB image
FROM mariadb:latest

# Configure database and credentials (change these before production)
ENV MYSQL_ROOT_PASSWORD=ChangeMeRootPass!
ENV MYSQL_DATABASE=demo_db
ENV MYSQL_USER=demo_user
ENV MYSQL_PASSWORD=demo_password123

# Persist database files
VOLUME ["/var/lib/mysql"]

# Expose default MariaDB port
EXPOSE 3306

