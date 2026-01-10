USE demo_db;
CREATE TABLE temp_example (
    id INT PRIMARY KEY AUTO_INCREMENT,
    note VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
-- Sample insert
INSERT INTO temp_example (note)
VALUES ('hello temp');
-- Query the temp table
SELECT *
FROM temp_example;