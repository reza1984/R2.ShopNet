# Contributing to R2.ShopNet

Thank you for your interest in contributing to R2.ShopNet! This document provides guidelines and best practices for contributing to this project.

## Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Guidelines](#commit-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)

## Code of Conduct

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive feedback
- Respect differing viewpoints and experiences

## Getting Started

### Prerequisites
1. Install all required tools mentioned in [README.md](README.md#prerequisites)
2. Fork the repository
3. Clone your fork: `git clone https://github.com/YOUR_USERNAME/r2shopnet.git`
4. Add upstream remote: `git remote add upstream <original-repo-url>`

### Setting Up Development Environment
```bash
# Install dependencies
dotnet restore

# Start infrastructure services
docker-compose up -d

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Development Workflow

### Branch Strategy
- `main` - Production-ready code (protected)
- `develop` - Integration branch (protected)
- `feature/*` - New features
- `bugfix/*` - Bug fixes
- `hotfix/*` - Critical production fixes

### Creating a Feature Branch
```bash
# Update your local develop branch
git checkout develop
git pull upstream develop

# Create a new feature branch
git checkout -b feature/your-feature-name
```

### Keeping Your Branch Up to Date
```bash
# Regularly sync with upstream
git checkout develop
git pull upstream develop
git checkout feature/your-feature-name
git rebase develop
```

## Coding Standards

### .NET Backend

#### Project Structure
Follow Clean Architecture principles:
```
ServiceName/
├── ServiceName.API/          # Web API layer
├── ServiceName.Application/  # Application logic (CQRS handlers)
├── ServiceName.Domain/        # Domain entities and business logic
└── ServiceName.Infrastructure/ # Data access and external services
```

#### Naming Conventions
- **Classes**: PascalCase (e.g., `UserService`, `OrderRepository`)
- **Interfaces**: PascalCase with `I` prefix (e.g., `IUserService`, `IOrderRepository`)
- **Methods**: PascalCase (e.g., `GetUserById`, `CreateOrder`)
- **Variables**: camelCase (e.g., `userId`, `orderItems`)
- **Constants**: PascalCase (e.g., `MaxRetryAttempts`)
- **Private fields**: camelCase with `_` prefix (e.g., `_userService`, `_logger`)

#### Code Style
- Use C# 12+ features where appropriate
- Enable nullable reference types
- Use `var` for local variables when type is obvious
- Prefer expression-bodied members for simple methods
- Use LINQ for collection operations
- Follow the `.editorconfig` settings

#### Example
```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return Result.Failure<User>(Error.NotFound("User.NotFound", "User not found"));

        return Result.Success(user);
    }
}
```

### Angular Frontend

#### Project Structure
```
src/
├── app/
│   ├── core/        # Singleton services, guards, interceptors
│   ├── shared/      # Reusable components, directives, pipes
│   ├── features/    # Feature modules
│   └── models/      # TypeScript interfaces and types
```

#### Naming Conventions
- **Components**: `kebab-case` for files, PascalCase for classes (e.g., `user-list.component.ts`, `UserListComponent`)
- **Services**: `kebab-case` for files, PascalCase for classes (e.g., `user.service.ts`, `UserService`)
- **Interfaces**: PascalCase (e.g., `User`, `OrderDetails`)
- **Variables**: camelCase (e.g., `userId`, `orderItems`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `API_BASE_URL`)

#### Code Style
- Use TypeScript strict mode
- Use standalone components (no NgModules)
- Use Signals for state management
- Use zoneless change detection where possible
- Follow the Angular style guide
- Use RxJS operators appropriately

#### Example
```typescript
@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserListComponent {
  private userService = inject(UserService);

  users = signal<User[]>([]);
  loading = signal(false);

  ngOnInit() {
    this.loadUsers();
  }

  private async loadUsers() {
    this.loading.set(true);
    try {
      const users = await this.userService.getUsers();
      this.users.set(users);
    } finally {
      this.loading.set(false);
    }
  }
}
```

## Commit Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/) specification.

### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Code style changes (formatting, missing semi-colons, etc.)
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `build`: Build system or dependencies
- `ci`: CI/CD changes
- `chore`: Other changes that don't modify src or test files

### Examples
```
feat(identity): add user registration endpoint

Implements user registration with email confirmation.
Includes validation for email uniqueness and password strength.

Closes #123
```

```
fix(catalog): resolve product image upload issue

Fixed an issue where images larger than 5MB were being rejected.
Updated MaxFileSize configuration to 10MB.

Fixes #456
```

## Pull Request Process

### Before Submitting
1. Ensure all tests pass: `dotnet test`
2. Build succeeds: `dotnet build`
3. Code follows style guidelines
4. Update documentation if needed
5. Add/update tests for your changes

### Creating a Pull Request
1. Push your branch: `git push origin feature/your-feature-name`
2. Go to GitHub and create a Pull Request
3. Fill out the PR template completely
4. Link related issues
5. Request review from maintainers

### PR Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing completed

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings generated
```

### Review Process
- At least one approval required
- All CI checks must pass
- Resolve all review comments
- Squash commits if requested

## Testing Guidelines

### Unit Tests
- Write tests for all new code
- Aim for 80%+ code coverage
- Use AAA pattern (Arrange, Act, Assert)
- Name tests descriptively: `MethodName_Scenario_ExpectedResult`

#### Example (.NET)
```csharp
[Fact]
public async Task GetUserById_WithValidId_ReturnsUser()
{
    // Arrange
    var userId = Guid.NewGuid();
    var expectedUser = new User { Id = userId, Email = "test@example.com" };
    _mockRepository.Setup(x => x.GetByIdAsync(userId, default))
        .ReturnsAsync(expectedUser);

    // Act
    var result = await _userService.GetUserByIdAsync(userId);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(expectedUser.Email, result.Value.Email);
}
```

### Integration Tests
- Test complete workflows
- Use Testcontainers for database tests
- Clean up test data after each test

### E2E Tests
- Test critical user journeys
- Use Cypress or Playwright
- Run in CI/CD pipeline

## Questions?

If you have questions, please:
1. Check existing documentation
2. Search closed issues
3. Ask in GitHub Discussions
4. Create a new issue

Thank you for contributing! 🎉
