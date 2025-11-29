# Use official MariaDB image
FROM mariadb:latest

# Configure database and credentials (change these before production)
ENV MYSQL_ROOT_PASSWORD=ChangeMeRootPass!
ENV MYSQL_DATABASE=nodpt_db_master
ENV MYSQL_USER=sql_user_admin
ENV MYSQL_PASSWORD=Ca159753*

# Persist database files
VOLUME ["/var/lib/mysql"]

# Expose default MariaDB port
EXPOSE 3306

# Use image's default entrypoint/cmd (mysqld)