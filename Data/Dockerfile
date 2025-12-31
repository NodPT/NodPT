# Use official MariaDB image
FROM mariadb:latest

# Configure database and credentials (change these before production)
ENV MYSQL_ROOT_PASSWORD=ChangeMeRootPass!
ENV MYSQL_DATABASE=
ENV MYSQL_USER=
ENV MYSQL_PASSWORD=

# Persist database files
VOLUME ["/var/lib/mysql"]

# Expose default MariaDB port
EXPOSE 3306

# Use image's default entrypoint/cmd (mysqld)