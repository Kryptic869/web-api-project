# Branching Strategy

A simplified GitFlow branching strategy has been selected for this solution.
The strategy separates active development, test-ready changes, and production approved releases while remaining manageable for a relatively small application. 

## Main Branch

The `main` branch represents the current production-ready version of the application. Direct pushes to the `main` branch are not permitted. 

Changes are introduced into `main` only through a pull request from `develop`. The pull request must pass the CI (Continuous Integration) pipeline and receive approval before it can be merged. 

A successful merge into `main` triggers deployment of the approved application artifact to the Production environment. 

## Develop Branch

The	`develop` branch represents the latest integrated version of the application intended for the Test environment. 

Completed feature branches are merged into `develop` through pull requests. Each pull request runs the CI validation process, including dependency restoration, Release compilation, and automated tests. 

A successful merge into the `develop` branch triggers publication and deployment of the application to the Test environment.

## Feature Branches

Developers must create feature branches from `develop` for individual changes or development streams.

Feature branches must follow the naming convention: 
`feature/<short-description>`

Once development is complete, a pull request is opened from the feature branch into `develop`. The feature branch may only be merged after the CI checks succeed and the change has been reviewed. 

## Hotfix Branches

Urgent production fixes are developed using branches created from `main`. 

Hotfix branches must follow the naming convention:
`hotfix/<short-description>`

After validation, the hotfix is merged into `main` for production deployment and into `develop` to ensure the correction remains part of any future releases. 

## Environment Mapping
| Branch      | Purpose						   | Deployment			  |

| `feature/*` | Isolated development work      | No deployment		  |
| `develop`   | Integrated and test-ready code | Test				  |
| `main`      | Approved production code       | Production			  |
| `hotfix/*`  | Urgent production corrections  | No direct deployment |

## Validation Rules
Pull requests into `develop` and `main` must successfully complete the following checks: 

1. Restore NuGet dependencies.
2. Build the solution using the Release configuration.
3. Run the automated test project.
4. Confirm that the API can be published successfully.

Direct pushes to `develop` and `main` should be restricted. Production deployment shoul also require explicit approval through the GitHub Production environment. 