# Product Overview

Excel Database Import Tool is a WPF desktop application for importing Excel files into databases with configurable field mappings and foreign key relationships.

## Core Features

- **Database Configuration Management**: Support for MySQL, SQL Server, and SQLite connections with encrypted credential storage
- **Import Configuration**: Define field mappings between Excel columns and database tables, including foreign key relationships
- **Excel Import Execution**: Batch import Excel data with transaction management and data validation
- **Import History**: View detailed logs of import operations, including errors and statistics
- **Data Validation**: Type checking, constraint validation, and referential integrity enforcement

## Target Users

Database administrators and data operators who need to regularly import Excel data into relational databases with complex relationships and validation requirements.

## Key Technical Characteristics

- Windows-only desktop application (WPF)
- Supports multiple database types (MySQL, SQL Server, SQLite)
- Uses SQLite for local configuration and log storage
- Property-based testing for reliability
- Comprehensive error handling and logging
