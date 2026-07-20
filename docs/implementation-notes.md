# Implementation Notes

## Repository assessment

- Original application targeted unsupported .NET 7.
- Upgraded all projects to .NET 10.
- Externalised database and JWT secrets. 
- Added Test and Production configuration files.
- Resolved conflicting Entity Framework Core package versions.
- Update a unit test after the controller constructor changed. 
- Correctly applied migrations using the Infrastructure project and API startup project. 
- Identified that protected endpoints require JWT authentication.

