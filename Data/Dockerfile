# MariaDB image with preconfigured demo user
FROM mariadb:latest

ENV MYSQL_ROOT_PASSWORD="saisai1234!" \
    MYSQL_DATABASE="demo_db" \
    MYSQL_USER="demo_user" \
    MYSQL_PASSWORD="demo_password1234!"

# Seed a superuser with full privileges
RUN mkdir -p /docker-entrypoint-initdb.d && \
    printf "CREATE USER IF NOT EXISTS 'demo_user'@'%%' IDENTIFIED BY 'demo_password1234!';\nGRANT ALL PRIVILEGES ON *.* TO 'demo_user'@'%%' WITH GRANT OPTION;\nFLUSH PRIVILEGES;\n" > /docker-entrypoint-initdb.d/01-init.sql
